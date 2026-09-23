"""
What Availity does to a file dropped in SendFiles, per the Batch EDI Companion Guide ch. 9:

  1. Recognize it (has content, .txt, starts with ISA) and drop a notification file in SendFiles:
     <original name>-<BatchID>-success   or   <original name>-<BatchID>-FAILED  (9.3)
  2. If the ISA is malformed          -> <BatchID>.ACK / .ACT with a "1E" line             (9.4)
  3. If ISA13 was already used        -> <BatchID>.TA1  TA1*<isa13>*..*R*025               (9.5)
  4. Otherwise a 999 in ReceiveFiles: IK5*R / AK9 for scripted rejects; positive 999 only
     when config.POSITIVE_999 is on                                                        (9.6)

Claim-level responses (.ibr / .277ibr) are not produced yet.
"""

from __future__ import annotations

import json
import shutil
import time
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path

from . import config

ISA_LENGTH = 106  # the only fixed-length record in X12 (guide 9.4.1.1)


@dataclass
class Transaction:
    st02: str
    claims: list[str] = field(default_factory=list)


@dataclass
class Interchange:
    isa: list[str]
    gs: list[str]
    transactions: list[Transaction]


def parse_interchange(text: str) -> Interchange | None:
    """None when the ISA isn't a valid fixed-width envelope."""
    if len(text) < ISA_LENGTH or not text.startswith("ISA"):
        return None
    elem, terminator = text[3], text[ISA_LENGTH - 1]
    isa = text[:ISA_LENGTH - 1].split(elem)
    if len(isa) != 17 or not isa[13].isdigit() or len(isa[13]) != 9:
        return None
    gs: list[str] = []
    transactions: list[Transaction] = []
    for raw in text[ISA_LENGTH:].split(terminator):
        seg = raw.strip().split(elem)
        if seg[0] == "GS" and not gs:
            gs = seg
        elif seg[0] == "ST" and len(seg) > 2:
            transactions.append(Transaction(st02=seg[2]))
        elif seg[0] in ("BHT", "CLM") and transactions:
            claim = seg[3] if seg[0] == "BHT" and len(seg) > 3 else seg[1] if seg[0] == "CLM" and len(seg) > 1 else ""
            if claim and claim not in transactions[-1].claims:
                transactions[-1].claims.append(claim)
    if len(gs) < 7:
        return None
    return Interchange(isa, gs, transactions)


def _load_state() -> dict:
    if config.STATE_FILE.exists():
        return json.loads(config.STATE_FILE.read_text())
    return {"isa13": [], "response_control": 0}


def _save_state(state: dict) -> None:
    config.STATE_FILE.write_text(json.dumps(state))


def _batch_id(now: datetime) -> str:
    # 16 digits like the guide's examples: CCYYMMDDHHMMSS + hundredths
    return now.strftime("%Y%m%d%H%M%S") + f"{now.microsecond // 10000:02d}"


def _segments(*segments: list[str]) -> str:
    return "".join("*".join(s) + "~\n" for s in segments)


def _envelope_isa(ic: Interchange, control: str, now: datetime) -> list[str]:
    """ISA for a response: Availity is the sender, the original sender is the receiver."""
    return ["ISA", "00", " " * 10, "00", " " * 10, "ZZ", "AVAILITY".ljust(15),
            ic.isa[5], ic.isa[6].strip().ljust(15), now.strftime("%y%m%d"), now.strftime("%H%M"),
            "^", "00501", control, "0", ic.isa[15], ":"]


def build_ta1(ic: Interchange, control: str, now: datetime) -> str:
    isa13 = ic.isa[13]
    return _segments(
        _envelope_isa(ic, control, now),
        ["TA1", isa13, ic.isa[9], ic.isa[10], "R", "025"],   # 025 = duplicate interchange control number
        ["IEA", "0", control],
    )


def build_999(ic: Interchange, rejects: dict[str, str], control: str, now: datetime) -> str:
    group = str(int(control))
    body = [["ST", "999", "0001", "005010X231A1"], ["AK1", "HC", ic.gs[6], "005010X222A1"]]
    accepted = 0
    for tx in ic.transactions:
        rejected_claim = next((c for c in tx.claims if c in rejects), None)
        body.append(["AK2", "837", tx.st02, "005010X222A1"])
        if rejected_claim:
            body += [["IK3", "CLM", "1", "2300", "8"], ["CTX", f"CLM01:{rejected_claim}"],
                     ["IK4", "2", "1314", "5", "AA"], ["IK5", "R", "5"]]
        else:
            body.append(["IK5", "A"])
            accepted += 1
    total = len(ic.transactions)
    status = "A" if accepted == total else "R" if accepted == 0 else "P"
    body.append(["AK9", status, str(total), str(total), str(accepted)])
    body.append(["SE", str(len(body) + 1), "0001"])
    return _segments(
        _envelope_isa(ic, control, now),
        ["GS", "FA", "AVAILITY", ic.gs[2], now.strftime("%Y%m%d"), now.strftime("%H%M"), group, "X", "005010X231A1"],
        *body,
        ["GE", "1", group],
        ["IEA", "1", control],
    )


def build_ack(batch_id: str, now: datetime) -> tuple[str, str]:
    error = "Availity does not recognize the interchange data starting at position 0 as valid."
    ack = (f"1|{now:%Y-%m-%d}|{now:%H.%M.%S}.{now.microsecond // 1000:03d}|{config.CUSTOMER_ID}|{batch_id}|000000000\n"
           f"1E|{error}\n")
    act = ("-" * 78 + "\n" + "AVAILITY PROPRIETARY ACKNOWLEDGEMENT".center(78) + "\n" + "-" * 78 + "\n"
           f"Customer ID: {config.CUSTOMER_ID}    Date Received: {now:%Y-%m-%d}    File Status: REJECTED\n"
           f"1E - {error}\n" + "-" * 78 + "\nEND OF REPORT".center(78) + "\n")
    return ack, act


def process_file(path: Path, rejects: dict[str, str]) -> dict:
    """Handle one uploaded file. Returns a record describing what the mock did (for the API)."""
    now = datetime.now()
    batch_id = _batch_id(now)
    name = path.name
    data = path.read_bytes()

    failure = None
    if not data:
        failure = "Empty file received - please review and resubmit"
    elif not name.lower().endswith(".txt"):
        failure = "Invalid file type received - please review and resubmit"
    elif not data.startswith(b"ISA"):
        failure = "Invalid file format received - please correct and resubmit"

    record = {"file": name, "batchId": batch_id, "receivedAt": now.isoformat(timespec="seconds"),
              "notification": "FAILED" if failure else "success", "isa13": None, "claims": [],
              "responses": [], "note": failure}
    notification = config.SEND_FILES / f"{name}-{batch_id}-{record['notification']}"
    notification.write_text(failure or f"{name} received by Availity, batch {batch_id}.\n")
    shutil.move(str(path), config.ARCHIVE / f"{batch_id}_{name}")
    if failure:
        return record

    def respond(extension: str, content: str) -> None:
        (config.RECEIVE_FILES / f"{batch_id}.{extension}").write_text(content)
        record["responses"].append(f"{batch_id}.{extension}")

    ic = parse_interchange(data.decode("latin-1"))
    if ic is None:
        ack, act = build_ack(batch_id, now)
        respond("ACK", ack)
        respond("ACT", act)
        record["note"] = "ISA not a valid fixed-width envelope"
        return record

    state = _load_state()
    record["isa13"] = ic.isa[13]
    record["claims"] = sorted({c for tx in ic.transactions for c in tx.claims})
    state["response_control"] += 1
    control = f"{state['response_control']:09d}"

    if ic.isa[13] in state["isa13"]:
        respond("TA1", build_ta1(ic, control, now))
        record["note"] = f"Duplicate ISA13 {ic.isa[13]}"
    else:
        state["isa13"].append(ic.isa[13])
        rejected = [c for c in record["claims"] if c in rejects]
        if rejected or config.POSITIVE_999:
            respond("999", build_999(ic, rejects, control, now))
        record["note"] = "; ".join(f"{c}: {rejects[c]}" for c in rejected) or None
    _save_state(state)
    return record


def pending_uploads() -> list[Path]:
    """Uploads that are finished (not touched for a moment) and aren't notification files."""
    cutoff = time.time() - 1.0
    return [p for p in sorted(config.SEND_FILES.iterdir())
            if p.is_file() and not p.name.endswith(("-success", "-FAILED")) and p.stat().st_mtime < cutoff]


def reset() -> None:
    """Forget seen ISA13s and empty the mailbox — a clean slate for a test run."""
    for folder in (config.SEND_FILES, config.RECEIVE_FILES, config.ARCHIVE):
        for f in folder.glob("*"):
            if f.is_file():
                f.unlink()
    if config.STATE_FILE.exists():
        config.STATE_FILE.unlink()
