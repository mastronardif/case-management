"""
Evernorth prior-auth submission job. Sibling to availity_job.py, same shape, simpler flow:

  send     Bundles the PriorAuthForm + TreatmentPlanAuthorization PDFs for each Pending row in
           cases.queueAuthDocsToBeSubmitted and drops them into a local outbox folder — MOCK ONLY.
           The practice currently does this send by hand via Gmail; this just stands in for that
           until real send automation is wanted. Marks the row Sent either way.

  resolve  The case manager's manual step once Evernorth's reply email comes back (also by hand,
           for now — there's no structured machine-parseable response to poll for, since Evernorth
           itself routes ABA prior auth through email/fax, not an EDI 278 channel). Marks the row
           Approved/Denied and, on Approved, writes the real AuthorizationNumber onto the case's
           existing cases.Authorization row (assumed to be exactly one per case — see
           project_claim_peripheral_resolution) plus both source entity rows, replacing the NULLs
           left there since extraction never had an authorization number to give them.

No auto-retry on send failure — same reasoning as availity_job.py: a Failed row sits until someone
reruns `send --queue-submit-id <id>` on purpose.

Requires WebAppMulti running (dotnet run) — PDF bytes come from GET /api/getDocument. Every run
logs through job_logging (console + C:/temp file + dbo.ApplicationLogs), same as every other job.

Usage:
    python evernorth_email_job.py send                         # every Pending row
    python evernorth_email_job.py send --queue-submit-id 3      # just one row
    python evernorth_email_job.py send --dry-run                # list what would be sent, touch nothing

    python evernorth_email_job.py resolve --queue-submit-id 3 --status Approved --authorization-number OP1234567
    python evernorth_email_job.py resolve --queue-submit-id 3 --status Denied --notes "Missing Vineland-3 scores"
"""

from __future__ import annotations

import argparse
import os
from datetime import datetime
from pathlib import Path

import pyodbc
import requests
import urllib3

from job_logging import get_logger

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

SERVER = r"LAPTOP-JIH94VS9\SQLEXPRESS"
DATABASE = "CaseManagement"
API_BASE = "https://localhost:44344/api"
OUTBOX_DIR = Path("C:/temp/evernorth-outbox")

# Reassigned in main() to "evernorth-send" / "evernorth-resolve" once the subcommand is known.
log = get_logger("evernorth")


def get_conn():
    return pyodbc.connect(
        "DRIVER={ODBC Driver 18 for SQL Server};"
        f"SERVER={SERVER};DATABASE={DATABASE};"
        "Trusted_Connection=yes;TrustServerCertificate=yes;",
        autocommit=True,
    )


def get_document_bytes(doc_id: int) -> bytes:
    resp = requests.get(f"{API_BASE}/getDocument", params={"docId": doc_id}, timeout=30, verify=False)
    resp.raise_for_status()
    return resp.content


def get_document_filename(conn, doc_id: int) -> str:
    row = conn.execute("SELECT FileName FROM cases.Document WHERE DocumentId = ?", (doc_id,)).fetchone()
    return row.FileName if row and row.FileName else f"document-{doc_id}.pdf"


BUNDLE_SELECT = """
    SELECT q.QueueSubmitId, q.CaseId, c.CaseNumber, q.Status, q.RecipientEmail,
           q.PriorAuthFormId, paf.SourceDocumentId AS PaSourceDocId,
           q.TreatmentPlanAuthorizationId, tpa.SourceDocumentId AS TpaSourceDocId
    FROM   cases.queueAuthDocsToBeSubmitted q
    JOIN   cases.[Case] c ON c.CaseId = q.CaseId
    JOIN   cases.PriorAuthForm paf ON paf.Id = q.PriorAuthFormId
    JOIN   cases.TreatmentPlanAuthorization tpa ON tpa.Id = q.TreatmentPlanAuthorizationId
"""


def pending_bundles(conn, queue_submit_id: int | None):
    """Default (no id): every Pending row. --queue-submit-id also reaches Failed rows, for a
    deliberate retry; refuses one that's already Sent/Approved/Denied, to avoid resending."""
    if queue_submit_id:
        rows = conn.execute(BUNDLE_SELECT + " WHERE q.QueueSubmitId = ?", (queue_submit_id,)).fetchall()
        if rows and rows[0].Status not in ("Pending", "Failed"):
            log.warning("QueueSubmitId=%s is already '%s' — not resending.", queue_submit_id, rows[0].Status)
            return []
        return rows
    return conn.execute(BUNDLE_SELECT + " WHERE q.Status = 'Pending' ORDER BY q.QueueSubmitId").fetchall()


def mark_sent(conn, queue_submit_id: int, folder_path: str):
    conn.execute(
        "UPDATE cases.queueAuthDocsToBeSubmitted SET Status = 'Sent', SentDate = SYSUTCDATETIME(), "
        "SentFolderPath = ? WHERE QueueSubmitId = ?", (str(folder_path), queue_submit_id))


def mark_failed(conn, queue_submit_id: int, error_message: str):
    # No retry logic yet — a Failed row sits there until someone runs
    # `send --queue-submit-id <id>` again on purpose (see pending_bundles).
    conn.execute(
        "UPDATE cases.queueAuthDocsToBeSubmitted SET Status = 'Failed', ResponseNotes = ? "
        "WHERE QueueSubmitId = ?", (error_message[:4000], queue_submit_id))


def cmd_send(args):
    conn = get_conn()
    rows = pending_bundles(conn, args.queue_submit_id)
    if not rows:
        log.info("Nothing to send.")
        return

    log.info("%d bundle(s) to send.", len(rows))
    if args.dry_run:
        for row in rows:
            log.info("  [dry-run] QueueSubmitId=%s CaseNumber=%s PriorAuthFormId=%s TreatmentPlanAuthorizationId=%s",
                      row.QueueSubmitId, row.CaseNumber, row.PriorAuthFormId, row.TreatmentPlanAuthorizationId)
        return

    for row in rows:
        try:
            folder = OUTBOX_DIR / f"{row.QueueSubmitId}_Case{row.CaseNumber}_{datetime.now():%Y%m%d%H%M%S}"
            folder.mkdir(parents=True, exist_ok=True)

            pa_name = get_document_filename(conn, row.PaSourceDocId)
            tpa_name = get_document_filename(conn, row.TpaSourceDocId)
            (folder / pa_name).write_bytes(get_document_bytes(row.PaSourceDocId))
            (folder / tpa_name).write_bytes(get_document_bytes(row.TpaSourceDocId))

            (folder / "email.txt").write_text(
                f"To: {row.RecipientEmail}\n"
                f"Subject: ABA Prior Authorization — Case {row.CaseNumber}\n\n"
                f"Attached: {pa_name}, {tpa_name}\n\n"
                "[MOCK SEND — practice currently sends this by hand via Gmail; "
                "this folder stands in for that email until real send automation is wanted.]\n",
                encoding="utf-8",
            )

            mark_sent(conn, row.QueueSubmitId, folder)
            log.info("Sent bundle QueueSubmitId=%s (Case %s) -> %s", row.QueueSubmitId, row.CaseNumber, folder,
                      extra={"props": {"QueueSubmitId": row.QueueSubmitId, "CaseId": row.CaseId, "Folder": str(folder)}})
        except Exception as ex:
            log.exception("Failed sending QueueSubmitId=%s (Case %s) — marking Failed (no auto-retry yet).",
                           row.QueueSubmitId, row.CaseNumber)
            mark_failed(conn, row.QueueSubmitId, f"{type(ex).__name__}: {ex}")


def cmd_resolve(args):
    conn = get_conn()
    row = conn.execute(BUNDLE_SELECT + " WHERE q.QueueSubmitId = ?", (args.queue_submit_id,)).fetchone()
    if row is None:
        log.error("QueueSubmitId=%s not found.", args.queue_submit_id)
        return
    if row.Status in ("Approved", "Denied"):
        log.warning("QueueSubmitId=%s is already '%s' — not re-resolving.", args.queue_submit_id, row.Status)
        return

    resolved_by = args.resolved_by or os.getenv("USERNAME") or "unknown"
    conn.execute(
        "UPDATE cases.queueAuthDocsToBeSubmitted SET Status = ?, AuthorizationNumber = ?, ResponseNotes = ?, "
        "ResolvedDate = SYSUTCDATETIME(), ResolvedBy = ? WHERE QueueSubmitId = ?",
        (args.status, args.authorization_number, args.notes, resolved_by, args.queue_submit_id))

    if args.status == "Approved":
        # One Authorization row per case, assumed (see project_claim_peripheral_resolution) — an
        # UPDATE, not an INSERT. A case with no existing row yet (never had an Authorization doc
        # extracted) logs a warning instead of guessing one into existence.
        n = conn.execute(
            "UPDATE cases.[Authorization] SET AuthorizationNumber = ? WHERE CaseId = ?",
            (args.authorization_number, row.CaseId)).rowcount
        if n == 0:
            log.warning("Case %s has no cases.Authorization row yet — AuthorizationNumber only saved "
                        "on the queue row, not propagated.", row.CaseNumber)
        conn.execute("UPDATE cases.PriorAuthForm SET AuthorizationNumber = ? WHERE Id = ?",
                      (args.authorization_number, row.PriorAuthFormId))
        conn.execute("UPDATE cases.TreatmentPlanAuthorization SET AuthorizationNumber = ? WHERE Id = ?",
                      (args.authorization_number, row.TreatmentPlanAuthorizationId))

    log.info("QueueSubmitId=%s (Case %s) -> %s", args.queue_submit_id, row.CaseNumber, args.status,
              extra={"props": {"QueueSubmitId": args.queue_submit_id, "CaseId": row.CaseId, "Status": args.status}})


def main():
    global log
    parser = argparse.ArgumentParser(description="Evernorth prior-auth submission job")
    sub = parser.add_subparsers(dest="command", required=True)

    send_p = sub.add_parser("send", help="Bundle Pending rows into the mock outbox folder")
    send_p.add_argument("--queue-submit-id", type=int, help="Only this QueueSubmitId (default: all Pending)")
    send_p.add_argument("--dry-run", action="store_true", help="List what would be sent; touch nothing")
    send_p.set_defaults(func=cmd_send)

    resolve_p = sub.add_parser("resolve", help="Case manager's manual Approved/Denied update")
    resolve_p.add_argument("--queue-submit-id", type=int, required=True)
    resolve_p.add_argument("--status", choices=["Approved", "Denied"], required=True)
    resolve_p.add_argument("--authorization-number", help="Required for Approved")
    resolve_p.add_argument("--notes", help="Evernorth's response, or the denial reason")
    resolve_p.add_argument("--resolved-by", help="Default: current Windows username")
    resolve_p.set_defaults(func=cmd_resolve)

    args = parser.parse_args()
    if args.command == "resolve" and args.status == "Approved" and not args.authorization_number:
        parser.error("--authorization-number is required when --status Approved")

    log = get_logger(f"evernorth-{args.command}")
    args.func(args)


if __name__ == "__main__":
    main()
