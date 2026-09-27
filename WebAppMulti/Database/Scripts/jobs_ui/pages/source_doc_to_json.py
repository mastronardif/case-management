import sys
from pathlib import Path

import pyodbc
import streamlit as st

from job_logging import DATABASE, SERVER
from jobs_ui.runner import run_streaming

SCRIPT_DIR = Path(__file__).resolve().parents[2]
AGENT_SCRIPT = SCRIPT_DIR / "doc_extraction_agent.py"


def get_table_names():
    """ProjectorRule names that are also a real resolvable [cases] table — the same
    CaseId/SourceDocumentId/JsonDocumentId whitelist usp_CaseTable_Resolve itself checks.
    Without this filter the list would also include internal 837P pipeline projections
    (Claim837P, Payer837P, ...), which --table-name can't actually resolve into."""
    conn = pyodbc.connect(
        "DRIVER={ODBC Driver 18 for SQL Server};"
        f"SERVER={SERVER};DATABASE={DATABASE};"
        "Trusted_Connection=yes;TrustServerCertificate=yes;"
    )
    with conn:
        names = [r[0] for r in conn.execute("""
            SELECT DISTINCT r.Name
            FROM   cases.ProjectorRule r
            WHERE  r.IsActive = 1
              AND  EXISTS (
                       SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS c
                       WHERE  c.TABLE_SCHEMA = 'cases' AND c.TABLE_NAME = r.Name
                         AND  c.COLUMN_NAME IN ('CaseId', 'SourceDocumentId', 'JsonDocumentId')
                       GROUP BY c.TABLE_NAME
                       HAVING COUNT(DISTINCT c.COLUMN_NAME) = 3)
            ORDER  BY r.Name
        """)]
    # Session first (the default/most-used), then everything else alphabetically.
    return ["Session"] + sorted(n for n in names if n != "Session")


# Outside the form so choosing a table updates the title immediately — widgets inside a
# st.form only take effect on submit, which would leave a stale table name in the title.
table_name = st.selectbox("Table", get_table_names(),
                           help="Which [cases] table's active projection/rule to extract against")

st.title(f"Source Doc → {table_name} JSON")
st.caption("Front end for doc_extraction_agent.py — runs the real CLI, nothing reimplemented here.")

with st.form("run_form"):
    case_id = st.number_input("Case ID", min_value=1, step=1, value=None, placeholder="e.g. 7")
    file_path = st.text_input("File", placeholder=r"C:\path\to\source.pdf (leave blank if using Doc ID)")
    src_doc_id = st.number_input("Doc ID", min_value=1, step=1, value=None,
                                  placeholder="already-uploaded source doc id (leave blank if using File)")
    dest = st.text_input("Dest", placeholder=r"C:\temp\session-context (optional — keeps working files)")
    submitted = st.form_submit_button("Run")

if submitted:
    if not case_id:
        st.error("Case ID is required.")
    elif not file_path and not src_doc_id:
        st.error("Provide either File or Doc ID.")
    elif file_path and src_doc_id:
        st.error("Provide only one of File or Doc ID, not both.")
    else:
        # -u forces the child process's stdout to be unbuffered — without it, Python fully
        # buffers stdout when it's not a real terminal (i.e. when piped like this), so output
        # would arrive in one big burst at the end instead of streaming line by line.
        cmd = [sys.executable, "-u", str(AGENT_SCRIPT), "run", "--table-name", table_name,
               "--case-id", str(int(case_id))]
        if table_name != "Session":
            # doc_extraction_agent.py's own default (SessionSource) only fits a Session run;
            # for any other table, the DocumentType should just be the table's own name.
            cmd += ["--doc-type", table_name]
        cmd += ["--src-doc-id", str(int(src_doc_id))] if src_doc_id else ["--file", file_path]
        if dest:
            cmd += ["--dest", dest]

        run_streaming(cmd, cwd=SCRIPT_DIR, key_prefix="source_doc")
