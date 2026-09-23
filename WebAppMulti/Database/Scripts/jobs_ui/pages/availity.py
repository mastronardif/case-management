import sys
from pathlib import Path

import requests
import streamlit as st

from jobs_ui.runner import run_streaming

MOCK_AVAILITY_URL = "http://localhost:8000"
SCRIPT_DIR = Path(__file__).resolve().parents[2]
AVAILITY_JOB_SCRIPT = SCRIPT_DIR / "availity_job.py"

st.title("Availity — Send Claims & Check Status")
st.caption("Built against a local mock (real SFTP server + responder) until real Availity credentials exist.")

st.subheader("Mock Availity")
try:
    health = requests.get(f"{MOCK_AVAILITY_URL}/health", timeout=2).json()
    sftp = health["sftp"]
    st.success(f"Mock Availity is up — SFTP {sftp['host']}:{sftp['port']}, user {sftp['user']}")

    col1, col2 = st.columns(2)
    with col1:
        st.write("**Mailbox**")
        mailbox = requests.get(f"{MOCK_AVAILITY_URL}/mailbox", timeout=5).json()
        for folder, files in mailbox.items():
            st.write(f"{folder} ({len(files)})")
            if files:
                st.code("\n".join(files), language=None)
    with col2:
        st.write("**Submissions**")
        submissions = requests.get(f"{MOCK_AVAILITY_URL}/submissions", timeout=5).json()
        if submissions:
            st.dataframe(submissions)
        else:
            st.info("Nothing sent to the mock yet.")

    with st.expander("Script an outcome for a claim number"):
        claim_number = st.text_input("Claim number", placeholder="CM000000009")
        outcome = st.radio("Outcome", ["accepted", "rejected"], horizontal=True)
        reason = st.text_input("Reason (if rejected)", placeholder="bad member id") if outcome == "rejected" else None
        if st.button("Set outcome") and claim_number:
            requests.put(f"{MOCK_AVAILITY_URL}/mock/claims/{claim_number}",
                         json={"outcome": outcome, "reason": reason}, timeout=5)
            st.rerun()

    if st.button("Reset mock (clear mailbox + scripted outcomes)"):
        requests.post(f"{MOCK_AVAILITY_URL}/mock/reset", timeout=5)
        st.rerun()
except requests.RequestException:
    st.error(f"Mock Availity isn't reachable at {MOCK_AVAILITY_URL}. Start it from the Scripts folder:")
    st.code("py -m uvicorn mock_availity.main:app --port 8000", language="powershell")

st.button("Refresh")

st.subheader("Jobs")


def run_job(command, queue_submit_id, dry_run, key_prefix):
    cmd = [sys.executable, "-u", str(AVAILITY_JOB_SCRIPT), command]
    if queue_submit_id:
        cmd += ["--queue-submit-id", str(int(queue_submit_id))]
    if dry_run:
        cmd += ["--dry-run"]
    run_streaming(cmd, cwd=SCRIPT_DIR, key_prefix=key_prefix)


send_tab, status_tab = st.tabs(["Send", "Status"])

with send_tab:
    st.caption("Uploads Pending Queue2 claims to Availity over SFTP.")
    with st.form("send_form"):
        send_id = st.number_input("QueueSubmitId (optional)", min_value=1, step=1, value=None,
                                   placeholder="leave blank for every Pending row")
        send_dry_run = st.checkbox("Dry run (list only, touch nothing)", value=False, key="send_dry_run")
        if st.form_submit_button("Run send"):
            run_job("send", send_id, send_dry_run, "availity_send")

with status_tab:
    st.caption("Checks Availity's response (notification + .ACK/.TA1/.999) for Sent Queue2 claims.")
    with st.form("status_form"):
        status_id = st.number_input("QueueSubmitId (optional)", min_value=1, step=1, value=None,
                                     placeholder="leave blank for every Sent row")
        status_dry_run = st.checkbox("Dry run (list only, touch nothing)", value=False, key="status_dry_run")
        if st.form_submit_button("Run status"):
            run_job("status", status_id, status_dry_run, "availity_status")
