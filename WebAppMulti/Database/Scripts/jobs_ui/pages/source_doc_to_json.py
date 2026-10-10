import re
import sys
from pathlib import Path

import pyodbc
import streamlit as st

from job_logging import DATABASE, SERVER
from jobs_ui.runner import run_streaming

SCRIPT_DIR = Path(__file__).resolve().parents[2]
AGENT_SCRIPT = SCRIPT_DIR / "doc_extraction_agent.py"

# Tables that are always submitted together (see project_priorauth_treatmentplan_evernorth) —
# not independently selectable, so the dropdown shows them as one combined (tuple) entry instead
# of two individual ones. Add future bundles here the same way; nothing else needs to change.
BUNDLES = [("PriorAuthForm", "TreatmentPlanAuthorization")]


def get_table_names():
    """ProjectorRule names that are also a real resolvable [cases] table — the same
    CaseId/SourceDocumentId/JsonDocumentId whitelist usp_CaseTable_Resolve itself checks.
    Without this filter the list would also include internal 837P pipeline projections
    (Claim837P, Payer837P, ...), which --table-name can't actually resolve into.

    Tables listed in BUNDLES are collapsed into one combined entry each (a tuple, instead of
    their own individual strings) — they're always submitted together, never independently."""
    conn = pyodbc.connect(
        "DRIVER={ODBC Driver 18 for SQL Server};"
        f"SERVER={SERVER};DATABASE={DATABASE};"
        "Trusted_Connection=yes;TrustServerCertificate=yes;"
    )
    with conn:
        names = {r[0] for r in conn.execute("""
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
        """)}

    bundle_entries = [b for b in BUNDLES if set(b) <= names]
    bundled_away = {t for b in bundle_entries for t in b}

    individual = sorted(n for n in names - bundled_away if n != "Session")
    # Session first (the default/most-used), then everything else alphabetically, bundles last.
    return (["Session"] if "Session" in names else []) + individual + bundle_entries


def table_label(table_sel):
    return ", ".join(table_sel) if isinstance(table_sel, tuple) else table_sel


# Outside the form so choosing a table updates the title immediately — widgets inside a
# st.form only take effect on submit, which would leave a stale table name in the title.
table_sel = st.selectbox("Table", get_table_names(), format_func=table_label,
                          help="Which [cases] table's active projection/rule to extract against")
is_bundle = isinstance(table_sel, tuple)

st.title(f"Source Doc → {table_label(table_sel)} JSON")
st.caption("Front end for doc_extraction_agent.py — runs the real CLI, nothing reimplemented here.")

with st.form("run_form"):
    case_id = st.number_input("Case ID", min_value=1, step=1, value=None, placeholder="e.g. 7")

    if is_bundle:
        # One File/Doc ID pair per table in the bundle — a list of inputs, not one, matching the
        # list the Table selectbox itself now holds.
        per_table = {}
        for t in table_sel:
            st.markdown(f"**{t}**")
            per_table[t] = (
                st.text_input(f"{t} File", key=f"{t}_file",
                               placeholder=r"C:\path\to\source.pdf (leave blank if using Doc ID)"),
                st.number_input(f"{t} Doc ID", min_value=1, step=1, value=None, key=f"{t}_docid",
                                 placeholder="already-uploaded source doc id (leave blank if using File)"),
            )
    else:
        file_path = st.text_input("File", placeholder=r"C:\path\to\source.pdf (leave blank if using Doc ID)")
        src_doc_id = st.number_input("Doc ID", min_value=1, step=1, value=None,
                                      placeholder="already-uploaded source doc id (leave blank if using File)")

    dest = st.text_input("Dest", placeholder=r"C:\temp\session-context (optional — keeps working files)")
    submitted = st.form_submit_button("Run")

if submitted:
    if not case_id:
        st.error("Case ID is required.")

    elif is_bundle:
        bad = [t for t, (f, d) in per_table.items() if (not f and not d) or (f and d)]
        if bad:
            st.error(f"Each table needs exactly one of File or Doc ID — check: {', '.join(bad)}.")
        else:
            # Each table still extracts completely independently via the same `run` every
            # single-table page uses, unchanged — only the final step (bundle-review) is new.
            extracted = {}
            ok = True
            for t, (f, d) in per_table.items():
                cmd = [sys.executable, "-u", str(AGENT_SCRIPT), "run", "--table-name", t,
                       "--doc-type", t, "--case-id", str(int(case_id))]
                cmd += ["--src-doc-id", str(int(d))] if d else ["--file", f]
                if dest:
                    cmd += ["--dest", dest]
                code, lines = run_streaming(cmd, cwd=SCRIPT_DIR, key_prefix=f"source_doc_{t}")
                if code != 0:
                    ok = False
                    break

                output = "\n".join(lines)
                # "Saved extracted JSON: 1234" is plain text, not a URL, so run_streaming's own
                # link-scraping doesn't catch it — it's the one thing bundle-review actually needs.
                json_match = re.search(r"Saved extracted JSON: (\d+)", output)
                if not json_match:
                    st.error(f"Couldn't find the extracted JSON doc id in {t}'s output.")
                    ok = False
                    break
                json_doc_id = int(json_match.group(1))

                if d:
                    src_doc_id_used = int(d)
                else:
                    src_match = re.search(r"source doc: (\d+)", output)
                    if not src_match:
                        st.error(f"Couldn't find the uploaded source doc id in {t}'s output.")
                        ok = False
                        break
                    src_doc_id_used = int(src_match.group(1))

                extracted[t] = (json_doc_id, src_doc_id_used)

            if ok:
                st.markdown("---")
                st.markdown("**Combining into one review page...**")
                pa_doc, pa_src = extracted["PriorAuthForm"]
                tpa_doc, tpa_src = extracted["TreatmentPlanAuthorization"]
                cmd = [sys.executable, "-u", str(AGENT_SCRIPT), "bundle-review",
                       "--case-id", str(int(case_id)),
                       "--prior-auth-doc-id", str(pa_doc), "--prior-auth-src-doc-id", str(pa_src),
                       "--treatment-plan-doc-id", str(tpa_doc), "--treatment-plan-src-doc-id", str(tpa_src)]
                run_streaming(cmd, cwd=SCRIPT_DIR, key_prefix="source_doc_bundle")

    elif not file_path and not src_doc_id:
        st.error("Provide either File or Doc ID.")
    elif file_path and src_doc_id:
        st.error("Provide only one of File or Doc ID, not both.")
    else:
        # -u forces the child process's stdout to be unbuffered — without it, Python fully
        # buffers stdout when it's not a real terminal (i.e. when piped like this), so output
        # would arrive in one big burst at the end instead of streaming line by line.
        cmd = [sys.executable, "-u", str(AGENT_SCRIPT), "run", "--table-name", table_sel,
               "--case-id", str(int(case_id))]
        if table_sel != "Session":
            # doc_extraction_agent.py's own default (SessionSource) only fits a Session run;
            # for any other table, the DocumentType should just be the table's own name.
            cmd += ["--doc-type", table_sel]
        cmd += ["--src-doc-id", str(int(src_doc_id))] if src_doc_id else ["--file", file_path]
        if dest:
            cmd += ["--dest", dest]

        run_streaming(cmd, cwd=SCRIPT_DIR, key_prefix="source_doc")
