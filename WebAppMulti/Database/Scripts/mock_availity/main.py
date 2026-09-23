"""
Local stand-in for Availity's batch EDI mailbox, until real credentials exist. One process runs:
  - an SFTP server (localhost:2222) with SendFiles / ReceiveFiles folders,
  - a responder that turns uploaded 837s into Availity-style notification + response files,
  - this FastAPI app, used only to script outcomes and inspect what the mock did.

Availity's own status API is not mocked (deferred). State is in memory except the seen-ISA13
list, which lives in data/state.json; POST /mock/reset clears everything.

Run from the Scripts folder:
    py -m uvicorn mock_availity.main:app --port 8000
Swagger docs: http://localhost:8000/docs
"""

import asyncio
import logging
from contextlib import asynccontextmanager
from typing import Literal, Optional

from fastapi import FastAPI
from pydantic import BaseModel

from . import config, responder, sftp_server

log = logging.getLogger("uvicorn.error")

_rejects: dict[str, str] = {}   # claim number -> reason; anything not listed is accepted
_submissions: list[dict] = []   # what the responder did, newest last


async def _poll_send_files() -> None:
    while True:
        for path in responder.pending_uploads():
            try:
                record = responder.process_file(path, _rejects)
            except PermissionError:
                continue  # still being written / locked; try again next tick
            _submissions.append(record)
            log.info("Mock Availity: %s -> %s %s", record["file"], record["notification"], record["responses"])
        await asyncio.sleep(config.POLL_SECONDS)


@asynccontextmanager
async def lifespan(_: FastAPI):
    config.ensure_dirs()
    server = await sftp_server.start()
    poller = asyncio.create_task(_poll_send_files())
    log.info("Mock Availity SFTP listening on port %s (user %s)", config.SFTP_PORT, config.SFTP_USER)
    yield
    poller.cancel()
    server.close()
    await server.wait_closed()


app = FastAPI(title="Mock Availity", description="Local stand-in for Availity's batch EDI mailbox.",
              lifespan=lifespan)


class ClaimOutcome(BaseModel):
    outcome: Literal["accepted", "rejected"] = "accepted"
    reason: Optional[str] = None


@app.get("/health")
def health():
    return {"status": "ok", "sftp": {"host": "localhost", "port": config.SFTP_PORT, "user": config.SFTP_USER,
                                     "folders": ["SendFiles", "ReceiveFiles"]}}


# Test control: script what the 999 will say for a claim number (matched against CLM01 and BHT03).
@app.put("/mock/claims/{claim_number}", response_model=ClaimOutcome)
def set_claim_outcome(claim_number: str, body: ClaimOutcome):
    if body.outcome == "rejected":
        _rejects[claim_number] = body.reason or "rejected by mock"
    else:
        _rejects.pop(claim_number, None)
    return body


@app.get("/mock/claims")
def scripted_outcomes():
    return [{"claimNumber": c, "outcome": "rejected", "reason": r} for c, r in _rejects.items()]


@app.get("/submissions")
def submissions():
    return _submissions


@app.get("/mailbox")
def mailbox():
    return {folder: sorted(p.name for p in (config.MAILBOX / folder).glob("*") if p.is_file())
            for folder in ("SendFiles", "ReceiveFiles")}


@app.post("/mock/reset")
def reset():
    responder.reset()
    _rejects.clear()
    _submissions.clear()
    return {"status": "cleared"}
