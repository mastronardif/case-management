"""
Shared logging for the Python jobs — the same three sinks the C# workers get from Serilog:
console, a rolling file under C:/temp, and the dbo.ApplicationLogs SQL table.

    from job_logging import get_logger
    log = get_logger("availity-send")
    log.info("Sent claim %s", claim_number, extra={"props": {"ClaimId": 5}})

Every row written to ApplicationLogs carries a Job property, so C# and Python rows can be told
apart in one query. The SQL sink never raises: a database outage must not crash a job, and the
console and file sinks still have the message.
"""

import logging
import logging.handlers
import sys
from datetime import datetime
from pathlib import Path
from xml.sax.saxutils import escape, quoteattr

import pyodbc

SERVER = r"LAPTOP-JIH94VS9\SQLEXPRESS"
DATABASE = "CaseManagement"
LOG_DIR = Path("C:/temp")

# Serilog level names, so the Level column matches what the C# jobs write.
SERILOG_LEVELS = {
    "DEBUG": "Debug", "INFO": "Information", "WARNING": "Warning", "ERROR": "Error", "CRITICAL": "Fatal",
}


class SqlServerHandler(logging.Handler):
    """Inserts each record into dbo.ApplicationLogs, Properties in Serilog's XML shape."""

    def __init__(self, job: str):
        super().__init__()
        self.job = job
        self._conn = None

    def _connect(self):
        return pyodbc.connect(
            "DRIVER={ODBC Driver 18 for SQL Server};"
            f"SERVER={SERVER};DATABASE={DATABASE};"
            "Trusted_Connection=yes;TrustServerCertificate=yes;",
            autocommit=True, timeout=3,
        )

    def emit(self, record: logging.LogRecord):
        try:
            props = {"Job": self.job, **getattr(record, "props", {})}
            xml = "<properties>" + "".join(
                f"<property key={quoteattr(str(k))}>{escape(str(v))}</property>" for k, v in props.items()
            ) + "</properties>"
            exception = self.formatter.formatException(record.exc_info) if record.exc_info else None
            row = (
                record.getMessage(),
                str(record.msg),
                SERILOG_LEVELS.get(record.levelname, record.levelname),
                datetime.fromtimestamp(record.created),
                exception,
                xml,
            )
            sql = ("INSERT INTO dbo.ApplicationLogs (Message, MessageTemplate, Level, TimeStamp, Exception, Properties) "
                   "VALUES (?, ?, ?, ?, ?, ?)")
            try:
                if self._conn is None:
                    self._conn = self._connect()
                self._conn.execute(sql, row)
            except pyodbc.Error:
                self._conn = None  # reconnect once, then give up on this record
                self._conn = self._connect()
                self._conn.execute(sql, row)
        except Exception:
            self._conn = None  # swallow — logging must never take the job down


def get_logger(job: str, level: int = logging.INFO) -> logging.Logger:
    log = logging.getLogger(job)
    if log.handlers:  # already configured (Streamlit reruns re-import modules)
        return log
    log.setLevel(level)
    log.propagate = False

    console = logging.StreamHandler(sys.stdout)
    console.setFormatter(logging.Formatter("%(asctime)s [%(levelname).3s] %(message)s", "%H:%M:%S"))
    log.addHandler(console)

    try:
        LOG_DIR.mkdir(parents=True, exist_ok=True)
        file_handler = logging.handlers.TimedRotatingFileHandler(
            LOG_DIR / f"casemanagement-{job}.log", when="midnight", backupCount=30, encoding="utf-8")
        file_handler.suffix = "%Y%m%d"
        file_handler.setFormatter(logging.Formatter("%(asctime)s [%(levelname)s] %(message)s"))
        log.addHandler(file_handler)
    except OSError:
        pass  # no file sink (e.g. read-only box) — console and SQL still work

    sql = SqlServerHandler(job)
    sql.setFormatter(logging.Formatter())
    log.addHandler(sql)
    return log
