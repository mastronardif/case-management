import os
import shutil
import sys
import tempfile
import zipfile
from datetime import datetime

import pyodbc
import requests
import urllib3

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# === CONFIG ===
SERVER      = r"LAPTOP-JIH94VS9\SQLEXPRESS"
DATABASE    = "CaseManagement"
# Hits the backend directly, not through the Vite dev server's proxy — this script only needs
# WebAppMulti running, not the frontend. verify=False since the dev cert is self-signed.
API_BASE    = "https://localhost:44344/api/getDocument"
ROOT        = os.path.dirname(os.path.abspath(__file__))
ARCHIVE_DIR = os.path.join(ROOT, "Archive", "ProjectorRules")
# ===============================


def get_manifest(include_inactive=False):
    conn_str = (
        "DRIVER={ODBC Driver 18 for SQL Server};"
        f"SERVER={SERVER};DATABASE={DATABASE};"
        "Trusted_Connection=yes;TrustServerCertificate=yes;"
    )
    with pyodbc.connect(conn_str) as conn:
        cursor = conn.cursor()
        cursor.execute(
            "EXEC [cases].[usp_GetManifestProjectorRule] @IncludeInactive = ?",
            1 if include_inactive else 0,
        )
        columns = [c[0] for c in cursor.description]
        return [dict(zip(columns, row)) for row in cursor.fetchall()]


def fetch_doc(doc_id):
    resp = requests.get(API_BASE, params={"docId": doc_id}, timeout=30, verify=False)
    resp.raise_for_status()
    return resp.text


def safe_name(name):
    return "".join(c if c.isalnum() or c in "-_." else "_" for c in name)


def write_docs(manifest, work_dir):
    written = 0
    for row in manifest:
        name = safe_name(row["Name"])
        for role, doc_id in (
            ("projection", row["ProjectionDocumentId"]),
            ("rule", row["RuleDocumentId"]),
        ):
            if not doc_id:
                continue
            try:
                content = fetch_doc(doc_id)
            except requests.RequestException as ex:
                print(f"ERROR: docId {doc_id} ({name}.{role}) fetch failed: {ex}")
                continue
            filename = f"{doc_id}.{name}.{role}.json"
            with open(os.path.join(work_dir, filename), "w", encoding="utf-8") as f:
                f.write(content)
            written += 1
            print(f"  wrote {filename}")
    return written


def zip_and_archive(work_dir):
    os.makedirs(ARCHIVE_DIR, exist_ok=True)
    stamp = datetime.now().strftime("%Y-%m-%d_%H%M%S")
    zip_path = os.path.join(ARCHIVE_DIR, f"ProjectionRules_{stamp}.zip")
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
        for filename in sorted(os.listdir(work_dir)):
            zf.write(os.path.join(work_dir, filename), filename)
    return zip_path


def main():
    include_inactive = "--include-inactive" in sys.argv

    print(f"Fetching manifest (include_inactive={include_inactive})...")
    manifest = get_manifest(include_inactive)
    print(f"  {len(manifest)} ProjectorRule row(s)")

    work_dir = tempfile.mkdtemp(prefix="projector_rule_archive_")
    try:
        written = write_docs(manifest, work_dir)
        if written == 0:
            print("Nothing written — aborting, no zip created.")
            return
        zip_path = zip_and_archive(work_dir)
        print(f"\nArchived {written} file(s) -> {zip_path}")
    finally:
        shutil.rmtree(work_dir, ignore_errors=True)


if __name__ == "__main__":
    main()
