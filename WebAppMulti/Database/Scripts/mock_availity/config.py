"""Settings for the mock Availity mailbox. Connection shape follows the Availity Batch EDI
Companion Guide 4.2: SFTP on port 22 (2222 here), user + password, SendFiles / ReceiveFiles folders."""

from pathlib import Path

DATA = Path(__file__).resolve().parent / "data"       # git-ignored
MAILBOX = DATA / "mailbox"                            # the SFTP root (login lands here, "Home")
SEND_FILES = MAILBOX / "SendFiles"                    # 837 uploads + -success / -FAILED notifications
RECEIVE_FILES = MAILBOX / "ReceiveFiles"              # .999 / .TA1 / .ACK response files
ARCHIVE = DATA / "archive"                            # consumed 837s, outside the SFTP root
STATE_FILE = DATA / "state.json"                      # seen ISA13s + response control counter
HOST_KEY = DATA / "host_key"

# Same folder set the guide's web client shows under Home.
FOLDERS = ["Alerts", "ClinicalFiles", "ReceiveFiles", "SendFiles", "SuccessFiles"]

SFTP_PORT = 2222
SFTP_USER = "mockuser"
SFTP_PASSWORD = "Mock-Availity-Pass1!"                # guide: 14+ chars, upper, lower, digit, special
POLL_SECONDS = 2

# Guide 9.6: negative 999s are always sent; positive 999s only if the org's EDI reporting
# preferences ask for them. True lets the status job see accepted results; set False to
# rehearse the "silence after -success" case.
POSITIVE_999 = True

CUSTOMER_ID = "0012345"                               # only appears inside .ACK files


def ensure_dirs() -> None:
    for folder in FOLDERS:
        (MAILBOX / folder).mkdir(parents=True, exist_ok=True)
    ARCHIVE.mkdir(parents=True, exist_ok=True)
