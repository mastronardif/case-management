"""
Availity I/O job. Two halves:

  send    Uploads ReadyToSubmit claims from cases.queueClaimsToBeSubmitted (Queue2) to Availity
          over SFTP.

  status  Reads back what Availity said about claims already Sent — the notification file in
          SendFiles (received/rejected) and, once available, the response file in ReceiveFiles
          (.ACK file-level reject, .TA1 envelope-level reject i.e. duplicate ISA13, or .999
          transaction-level accept/reject) — and updates Queue2/Claim accordingly. A row with no
          notification or response yet is simply left 'Sent' and picked up again next run; there's
          no polling/scheduling here yet, each run is one pass.

          Correlation is by filename: send() names each upload "837P_<QueueSubmitId>_..." with no
          hyphens of its own, so Availity's notification filename ("<our name>-<BatchID>-success")
          can be split on "-" to recover both the QueueSubmitId (from the prefix) and the BatchID
          (which names the matching response file in ReceiveFiles).

Built and tested against the local mock (mock_availity/) until real Availity SFTP credentials
exist; when they do, only the three CONFIG constants below change.

Requires WebAppMulti running (dotnet run) — EDI content comes from GET /api/getDocument, same as
session_doc_agent.py. Every run logs through job_logging (console + C:/temp file +
dbo.ApplicationLogs), same as the C# jobs.

Usage:
    python availity_job.py send                         # every Pending Queue2 row
    python availity_job.py send --queue-submit-id 7      # just one row
    python availity_job.py send --dry-run                # list what would be sent, touch nothing

    python availity_job.py status                        # every Sent Queue2 row
    python availity_job.py status --queue-submit-id 7     # just one row
    python availity_job.py status --dry-run               # list what would be checked, touch nothing
"""

from __future__ import annotations

import argparse
import io
from datetime import datetime

import paramiko
import pyodbc
import requests
import urllib3

from job_logging import get_logger

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# === CONFIG — swap these three for the real Availity SFTP account when it's ready ===
SFTP_HOST = "localhost"
SFTP_PORT = 2222
SFTP_USER = "mockuser"
SFTP_PASSWORD = "Mock-Availity-Pass1!"
# ======================================================================================

SERVER = r"LAPTOP-JIH94VS9\SQLEXPRESS"
DATABASE = "CaseManagement"
API_BASE = "https://localhost:44344/api"

# Reassigned in main() to "availity-send" / "availity-status" once the subcommand is known.
log = get_logger("availity")


def get_conn():
    return pyodbc.connect(
        "DRIVER={ODBC Driver 18 for SQL Server};"
        f"SERVER={SERVER};DATABASE={DATABASE};"
        "Trusted_Connection=yes;TrustServerCertificate=yes;",
        autocommit=True,
    )


def get_document(doc_id: int) -> bytes:
    resp = requests.get(f"{API_BASE}/getDocument", params={"docId": doc_id}, timeout=30, verify=False)
    resp.raise_for_status()
    return resp.content


def sftp_connect():
    transport = paramiko.Transport((SFTP_HOST, SFTP_PORT))
    transport.connect(username=SFTP_USER, password=SFTP_PASSWORD)
    return transport, paramiko.SFTPClient.from_transport(transport)


def pending_submissions(conn, queue_submit_id: int | None):
    """Default (no id): every Pending row — Failed rows are never picked up automatically,
    there's no retry logic yet (by design, for now). --queue-submit-id targets one row
    directly and also reaches Failed rows, so a deliberate retry after fixing the cause works;
    it still refuses a row that already succeeded (Sent), to avoid a double submission."""
    if queue_submit_id:
        sql = """
            SELECT q.QueueSubmitId, q.ClaimId, q.EdiDocumentId, q.Status, c.CaseId, c.ClaimNumber
            FROM   cases.queueClaimsToBeSubmitted q
            JOIN   cases.Claim c ON c.ClaimId = q.ClaimId
            WHERE  q.QueueSubmitId = ?
        """
        rows = conn.execute(sql, (queue_submit_id,)).fetchall()
        if rows and rows[0].Status not in ("Pending", "Failed"):
            log.warning("QueueSubmitId=%s is already '%s' — not resending.", queue_submit_id, rows[0].Status)
            return []
        return rows

    sql = """
        SELECT q.QueueSubmitId, q.ClaimId, q.EdiDocumentId, q.Status, c.CaseId, c.ClaimNumber
        FROM   cases.queueClaimsToBeSubmitted q
        JOIN   cases.Claim c ON c.ClaimId = q.ClaimId
        WHERE  q.Status = 'Pending'
        ORDER  BY q.QueueSubmitId
    """
    return conn.execute(sql).fetchall()


def mark_sent(conn, queue_submit_id: int, claim_id: int, remote_name: str):
    conn.execute(
        "UPDATE cases.queueClaimsToBeSubmitted SET Status = 'Sent', SentDate = SYSUTCDATETIME(), "
        "ClearinghouseResponse = ? WHERE QueueSubmitId = ?",
        (f"Uploaded {remote_name}", queue_submit_id))
    conn.execute(
        "UPDATE cases.Claim SET Status = 'Submitted', SubmittedDate = SYSUTCDATETIME() WHERE ClaimId = ?",
        (claim_id,))


def mark_failed(conn, queue_submit_id: int, claim_id: int, error_message: str):
    # No retry logic yet — a Failed row sits there until someone runs
    # `send --queue-submit-id <id>` again on purpose (see pending_submissions).
    conn.execute(
        "UPDATE cases.queueClaimsToBeSubmitted SET Status = 'Failed', ClearinghouseResponse = ? "
        "WHERE QueueSubmitId = ?", (error_message[:4000], queue_submit_id))
    conn.execute("UPDATE cases.Claim SET Status = 'Failed' WHERE ClaimId = ?", (claim_id,))


def add_pipeline_event(conn, case_id: int, claim_id: int, event_type: str, details: str):
    conn.execute(
        "INSERT INTO cases.ClaimPipelineEvent (CaseId, ClaimId, EventType, Details) VALUES (?, ?, ?, ?)",
        (case_id, claim_id, event_type, details))


def sent_submissions(conn, queue_submit_id: int | None):
    """Default (no id): every Sent row. --queue-submit-id checks one row regardless of status,
    useful for re-checking a row that's already terminal."""
    if queue_submit_id:
        sql = """
            SELECT q.QueueSubmitId, q.ClaimId, q.Status, c.CaseId, c.ClaimNumber
            FROM   cases.queueClaimsToBeSubmitted q
            JOIN   cases.Claim c ON c.ClaimId = q.ClaimId
            WHERE  q.QueueSubmitId = ?
        """
        return conn.execute(sql, (queue_submit_id,)).fetchall()

    sql = """
        SELECT q.QueueSubmitId, q.ClaimId, q.Status, c.CaseId, c.ClaimNumber
        FROM   cases.queueClaimsToBeSubmitted q
        JOIN   cases.Claim c ON c.ClaimId = q.ClaimId
        WHERE  q.Status = 'Sent'
        ORDER  BY q.QueueSubmitId
    """
    return conn.execute(sql).fetchall()


def mark_result(conn, row, status: str, response_text: str, event_type: str):
    response_text = response_text.strip()[:4000]
    conn.execute(
        "UPDATE cases.queueClaimsToBeSubmitted SET Status = ?, AcknowledgedDate = SYSUTCDATETIME(), "
        "ClearinghouseResponse = ? WHERE QueueSubmitId = ?", (status, response_text, row.QueueSubmitId))
    conn.execute(
        "UPDATE cases.Claim SET Status = ?, AcknowledgedDate = SYSUTCDATETIME() WHERE ClaimId = ?",
        (status, row.ClaimId))
    add_pipeline_event(conn, row.CaseId, row.ClaimId, event_type, response_text[:400])
    log.info("QueueSubmitId=%s ClaimNumber=%s -> %s", row.QueueSubmitId, row.ClaimNumber, status,
              extra={"props": {"ClaimId": row.ClaimId, "QueueSubmitId": row.QueueSubmitId, "Result": status}})


def parse_ta1(body: str) -> tuple[str, str]:
    """Returns (code, note) from TA1*<isa13>*<date>*<time>*<code>*<note>~."""
    seg = next((s.split("*") for s in body.split("~") if s.strip().startswith("TA1")), None)
    return (seg[4], seg[5]) if seg and len(seg) > 5 else ("R", "unparseable TA1")


def parse_999(body: str) -> tuple[bool, str]:
    """Our files are always exactly one ST (one claim) per interchange, so a single IK5 tells the
    whole story. Returns (accepted, detail-line-for-the-log)."""
    lines = [s.strip() for s in body.split("~") if s.strip()]
    accepted = any(line.startswith("IK5*A") for line in lines)
    if accepted:
        return True, "999: accepted (IK5*A)"
    detail = next((line for line in lines if line.startswith(("IK3", "IK4", "CTX"))), None)
    return False, f"999: rejected — {detail}" if detail else "999: rejected"


def cmd_send(args):
    conn = get_conn()
    rows = pending_submissions(conn, args.queue_submit_id)
    if not rows:
        log.info("Nothing to send.")
        return

    log.info("%d row(s) to send.", len(rows))
    if args.dry_run:
        for row in rows:
            log.info("  [dry-run] QueueSubmitId=%s ClaimId=%s ClaimNumber=%s EdiDocumentId=%s Status=%s",
                      row.QueueSubmitId, row.ClaimId, row.ClaimNumber, row.EdiDocumentId, row.Status)
        return

    transport, sftp = sftp_connect()
    log.info("Connected to Availity SFTP %s:%s as %s", SFTP_HOST, SFTP_PORT, SFTP_USER)
    try:
        for row in rows:
            try:
                edi_bytes = get_document(row.EdiDocumentId)
                remote_name = f"837P_{row.QueueSubmitId}_{row.ClaimNumber}_{datetime.now():%Y%m%d%H%M%S}.txt"
                sftp.putfo(io.BytesIO(edi_bytes), f"SendFiles/{remote_name}")
                mark_sent(conn, row.QueueSubmitId, row.ClaimId, remote_name)
                add_pipeline_event(conn, row.CaseId, row.ClaimId, "SubmittedToAvaility",
                                    f"QueueSubmitId={row.QueueSubmitId}; file={remote_name}")
                log.info("Sent claim %s (QueueSubmitId=%s) as %s", row.ClaimNumber, row.QueueSubmitId, remote_name,
                          extra={"props": {"ClaimId": row.ClaimId, "QueueSubmitId": row.QueueSubmitId,
                                            "RemoteFile": remote_name}})
            except Exception as ex:
                log.exception("Failed sending QueueSubmitId=%s ClaimNumber=%s — marking Failed (no auto-retry yet).",
                               row.QueueSubmitId, row.ClaimNumber)
                mark_failed(conn, row.QueueSubmitId, row.ClaimId, f"{type(ex).__name__}: {ex}")
                add_pipeline_event(conn, row.CaseId, row.ClaimId, "SubmitFailed",
                                    f"QueueSubmitId={row.QueueSubmitId}; {type(ex).__name__}: {ex}")
    finally:
        sftp.close()
        transport.close()


def cmd_status(args):
    conn = get_conn()
    rows = sent_submissions(conn, args.queue_submit_id)
    if not rows:
        log.info("Nothing to check.")
        return

    log.info("%d row(s) to check.", len(rows))
    if args.dry_run:
        for row in rows:
            log.info("  [dry-run] QueueSubmitId=%s ClaimId=%s ClaimNumber=%s Status=%s",
                      row.QueueSubmitId, row.ClaimId, row.ClaimNumber, row.Status)
        return

    transport, sftp = sftp_connect()
    log.info("Connected to Availity SFTP %s:%s as %s", SFTP_HOST, SFTP_PORT, SFTP_USER)
    try:
        send_files = sftp.listdir("SendFiles")
        receive_files = set(sftp.listdir("ReceiveFiles"))

        for row in rows:
            try:
                prefix = f"837P_{row.QueueSubmitId}_"
                notif = next((f for f in send_files
                              if f.startswith(prefix) and f.endswith(("-success", "-FAILED"))), None)
                if notif is None:
                    log.info("QueueSubmitId=%s: no notification yet — still Sent.", row.QueueSubmitId)
                    continue

                # Our own upload names never contain "-" (see module docstring), so this split is safe.
                _original, batch_id, outcome = notif.rsplit("-", 2)

                if outcome == "FAILED":
                    body = sftp.open(f"SendFiles/{notif}").read().decode()
                    mark_result(conn, row, "Failed", body, "AvailityFileRejected")
                    continue

                # "success" only means the file was recognized — .ACK/.TA1/.999 still gate the
                # real outcome, checked most-severe-first per the guide's staged validation.
                ext = next((e for e in ("ACK", "TA1", "999") if f"{batch_id}.{e}" in receive_files), None)
                if ext is None:
                    log.info("QueueSubmitId=%s: received (batch %s), no response yet — still Sent.",
                              row.QueueSubmitId, batch_id)
                    continue

                body = sftp.open(f"ReceiveFiles/{batch_id}.{ext}").read().decode()
                if ext == "ACK":
                    reason = next((line[3:] for line in body.splitlines() if line.startswith("1E|")), body)
                    mark_result(conn, row, "Failed", f"ACK: {reason}", "AvailityFileRejected")
                elif ext == "TA1":
                    code, note = parse_ta1(body)
                    status = "Failed" if code == "R" else "Accepted"
                    mark_result(conn, row, status, f"TA1: code={code} note={note}", "AvailityInterchangeRejected")
                else:  # 999
                    accepted, detail = parse_999(body)
                    status = "Accepted" if accepted else "Rejected"
                    event = "ClaimAcceptedByAvaility" if accepted else "ClaimRejectedByAvaility"
                    mark_result(conn, row, status, detail, event)
            except Exception:
                log.exception("Failed checking status for QueueSubmitId=%s ClaimNumber=%s — left as-is.",
                               row.QueueSubmitId, row.ClaimNumber)
    finally:
        sftp.close()
        transport.close()


def main():
    global log
    parser = argparse.ArgumentParser(description="Availity I/O job")
    sub = parser.add_subparsers(dest="command", required=True)

    send_p = sub.add_parser("send", help="Upload Pending Queue2 claims to Availity over SFTP")
    send_p.add_argument("--queue-submit-id", type=int, help="Only this QueueSubmitId (default: all Pending)")
    send_p.add_argument("--dry-run", action="store_true", help="List what would be sent; touch nothing")
    send_p.set_defaults(func=cmd_send)

    status_p = sub.add_parser("status", help="Check Availity's response for Sent Queue2 claims")
    status_p.add_argument("--queue-submit-id", type=int, help="Only this QueueSubmitId (default: all Sent)")
    status_p.add_argument("--dry-run", action="store_true", help="List what would be checked; touch nothing")
    status_p.set_defaults(func=cmd_status)

    args = parser.parse_args()
    log = get_logger(f"availity-{args.command}")
    args.func(args)


if __name__ == "__main__":
    main()
