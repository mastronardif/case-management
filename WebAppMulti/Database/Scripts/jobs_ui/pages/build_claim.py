from pathlib import Path

import pyodbc
import streamlit as st

from job_logging import DATABASE, SERVER
from jobs_ui.runner import run_streaming

SCRIPT_DIR = Path(__file__).resolve().parents[2]
# Scripts -> Database -> WebAppMulti -> repo root
PROJECT_DIR = SCRIPT_DIR.parents[2] / "CaseManagement.Jobs" / "src" / "CaseManagement.SessionBillResolvers.V2"


def query(sql, *params):
    conn = pyodbc.connect(
        "DRIVER={ODBC Driver 18 for SQL Server};"
        f"SERVER={SERVER};DATABASE={DATABASE};"
        "Trusted_Connection=yes;TrustServerCertificate=yes;"
    )
    with conn:
        cursor = conn.cursor()
        cursor.execute(sql, *params)
        columns = [c[0] for c in cursor.description]
        return [dict(zip(columns, row)) for row in cursor.fetchall()]


st.title("Build Claim — Queue → 837P")
st.caption("Runs the C# clearing-house job for one queued claim: builds the claim, validates it, writes the "
           "837P EDI and hands it to the send queue. It talks to SQL directly, so the web server doesn't "
           "need to be running.")

pending = query("SELECT QueueClaimId, CaseId, Status, CreatedDate FROM cases.queueClaimsToBeCreated "
                "WHERE Status = 'Pending' ORDER BY QueueClaimId")
st.subheader(f"Pending queue rows ({len(pending)})")
if pending:
    st.dataframe(pending)
else:
    st.info("Nothing is waiting in the queue.")
st.button("Refresh")

st.subheader("Build")
with st.form("build_form"):
    queue_claim_id = st.number_input("QueueClaimId", min_value=1, step=1, value=None, placeholder="e.g. 8")
    rebuild = st.checkbox("Rebuild even if this row was already built")
    submitted = st.form_submit_button("Build claim")

if submitted:
    if not queue_claim_id:
        st.error("QueueClaimId is required.")
    else:
        queue_claim_id = int(queue_claim_id)
        rows = query("SELECT QueueClaimId, CaseId, Status FROM cases.queueClaimsToBeCreated "
                     "WHERE QueueClaimId = ?", queue_claim_id)
        if not rows:
            st.error(f"No queue row with QueueClaimId {queue_claim_id}.")
        elif rows[0]["Status"] != "Pending" and not rebuild:
            # The job doesn't check the row's status, so a second run makes a second claim and uses
            # up another ISA control number.
            st.warning(f"QueueClaimId {queue_claim_id} (case {rows[0]['CaseId']}) is '{rows[0]['Status']}' — "
                       "already built. Running again creates a second claim and consumes another ISA "
                       "control number. Tick \"Rebuild even if this row was already built\" if that's what you want.")
        else:
            run_streaming(["dotnet", "run", "--", "--queue-claim-id", str(queue_claim_id)],
                          cwd=PROJECT_DIR, key_prefix="build_claim")
