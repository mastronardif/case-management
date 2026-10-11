import sys
from pathlib import Path

import pyodbc
import streamlit as st

from job_logging import DATABASE, SERVER
from jobs_ui.runner import run_streaming

SCRIPT_DIR = Path(__file__).resolve().parents[2]
EVERNORTH_JOB_SCRIPT = SCRIPT_DIR / "evernorth_email_job.py"
OUTBOX_DIR = Path("C:/temp/evernorth-outbox")

st.title("Evernorth — Prior Auth Submission")
st.caption("MOCK send — drops both PDFs into a local outbox folder instead of actually emailing "
           "ABA@Evernorth.com (the practice currently does this by hand via Gmail). Resolve is "
           "always manual — Evernorth's own reply comes back by email/fax, not a structured feed.")


def get_queue_rows():
    conn = pyodbc.connect(
        "DRIVER={ODBC Driver 18 for SQL Server};"
        f"SERVER={SERVER};DATABASE={DATABASE};"
        "Trusted_Connection=yes;TrustServerCertificate=yes;"
    )
    with conn:
        cols = [d[0] for d in conn.execute("SELECT TOP 0 * FROM cases.queueAuthDocsToBeSubmitted").description]
        rows = conn.execute(
            "SELECT * FROM cases.queueAuthDocsToBeSubmitted ORDER BY QueueSubmitId DESC").fetchall()
    return cols, [list(r) for r in rows]


cols, rows = get_queue_rows()
st.subheader(f"Queue ({len(rows)})")
if rows:
    st.dataframe([dict(zip(cols, r)) for r in rows])
else:
    st.info("Nothing queued yet — submit a bundle from a case's Claim page first.")
st.button("Refresh")

folders = sorted((p for p in OUTBOX_DIR.iterdir() if p.is_dir()), reverse=True) if OUTBOX_DIR.exists() else []
st.subheader(f"Outbox ({len(folders)})")
if OUTBOX_DIR.exists():
    if folders:
        for folder in folders:
            with st.expander(folder.name):
                for f in sorted(folder.iterdir()):
                    st.write(f.name)
    else:
        st.info("Outbox folder exists but is empty — nothing sent yet.")
else:
    st.info(f"Outbox folder doesn't exist yet ({OUTBOX_DIR}) — nothing sent yet.")

st.subheader("Jobs")

send_tab, resolve_tab = st.tabs(["Send", "Resolve"])

with send_tab:
    st.caption("Bundles Pending rows' PDFs into the outbox folder and marks them Sent.")
    with st.form("send_form"):
        send_id = st.number_input("QueueSubmitId (optional)", min_value=1, step=1, value=None,
                                   placeholder="leave blank for every Pending row")
        send_dry_run = st.checkbox("Dry run (list only, touch nothing)", value=False, key="send_dry_run")
        if st.form_submit_button("Run send"):
            cmd = [sys.executable, "-u", str(EVERNORTH_JOB_SCRIPT), "send"]
            if send_id:
                cmd += ["--queue-submit-id", str(int(send_id))]
            if send_dry_run:
                cmd += ["--dry-run"]
            run_streaming(cmd, cwd=SCRIPT_DIR, key_prefix="evernorth_send")

with resolve_tab:
    st.caption("The case manager's manual update once Evernorth's reply (email/fax) comes back.")
    with st.form("resolve_form"):
        resolve_id = st.number_input("QueueSubmitId", min_value=1, step=1, value=None)
        status = st.radio("Status", ["Approved", "Denied"], horizontal=True)
        auth_number = st.text_input("Authorization number", placeholder="required if Approved") \
            if status == "Approved" else None
        notes = st.text_input("Notes", placeholder="Evernorth's response, or the denial reason")
        resolved_by = st.text_input("Resolved by (optional)", placeholder="defaults to your Windows username")
        if st.form_submit_button("Run resolve"):
            if not resolve_id:
                st.error("QueueSubmitId is required.")
            elif status == "Approved" and not auth_number:
                st.error("Authorization number is required for Approved.")
            else:
                cmd = [sys.executable, "-u", str(EVERNORTH_JOB_SCRIPT), "resolve",
                       "--queue-submit-id", str(int(resolve_id)), "--status", status]
                if auth_number:
                    cmd += ["--authorization-number", auth_number]
                if notes:
                    cmd += ["--notes", notes]
                if resolved_by:
                    cmd += ["--resolved-by", resolved_by]
                run_streaming(cmd, cwd=SCRIPT_DIR, key_prefix="evernorth_resolve")
