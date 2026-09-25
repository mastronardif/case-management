"""
Archives the pipeline's config documents from the DB into a timestamped zip under
Archive/ArchiveJson — the zips are committed to git, so they're the source copy. The
manifest (which docs, which folder) comes from cases.usp_GetManifestJson:

  ProjectorRules/   projection + rule docs for every cases.ProjectorRule row
  FieldMaps/        cases.TableFieldMap mapping docs (usp_TableFieldMap_Apply reads these) —
                    latest version per table, or every version with --all-versions
  Constants/        every document cases.MyConstants points at (Type = 'int'), e.g. Operators

Needs WebAppMulti running: docs come through /api/getDocument.

Usage:
    python archive_jsons.py [--include-inactive] [--all-versions]
"""

import os
import shutil
import sys
import tempfile
import zipfile
from collections import Counter
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
ARCHIVE_DIR = os.path.join(ROOT, "Archive", "ArchiveJson")
# ===============================

CONTENT_TYPE_EXT = {"application/json": "json", "text/html": "html"}


def get_manifest(include_inactive=False, all_versions=False):
    conn_str = (
        "DRIVER={ODBC Driver 18 for SQL Server};"
        f"SERVER={SERVER};DATABASE={DATABASE};"
        "Trusted_Connection=yes;TrustServerCertificate=yes;"
    )
    with pyodbc.connect(conn_str) as conn:
        cursor = conn.cursor()
        cursor.execute(
            "EXEC [cases].[usp_GetManifestJson] @IncludeInactive = ?, @AllVersions = ?",
            1 if include_inactive else 0,
            1 if all_versions else 0,
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
    written = Counter()
    for row in manifest:
        folder, name, role, doc_id = row["Folder"], row["Name"], row["Role"], row["DocumentId"]
        try:
            content = fetch_doc(doc_id)
        except requests.RequestException as ex:
            print(f"ERROR: docId {doc_id} ({folder}/{name}.{role}) fetch failed: {ex}")
            continue
        ext = CONTENT_TYPE_EXT.get(row["ContentType"], "json")
        filename = f"{doc_id}.{safe_name(name)}.{role}.{ext}"
        os.makedirs(os.path.join(work_dir, folder), exist_ok=True)
        with open(os.path.join(work_dir, folder, filename), "w", encoding="utf-8") as f:
            f.write(content)
        written[folder] += 1
        print(f"  wrote {folder}/{filename}")
    return written


def zip_and_archive(work_dir):
    os.makedirs(ARCHIVE_DIR, exist_ok=True)
    stamp = datetime.now().strftime("%Y-%m-%d_%H%M%S")
    zip_path = os.path.join(ARCHIVE_DIR, f"ArchiveJson_{stamp}.zip")
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
        for folder, _, files in os.walk(work_dir):
            for filename in sorted(files):
                full = os.path.join(folder, filename)
                zf.write(full, os.path.relpath(full, work_dir).replace(os.sep, "/"))
    return zip_path


def main():
    include_inactive = "--include-inactive" in sys.argv
    all_versions = "--all-versions" in sys.argv

    print(f"Fetching manifest (include_inactive={include_inactive}, all_versions={all_versions})...")
    manifest = get_manifest(include_inactive, all_versions)
    print(f"  {len(manifest)} document(s): {dict(Counter(row['Folder'] for row in manifest))}")

    work_dir = tempfile.mkdtemp(prefix="archive_json_")
    try:
        written = write_docs(manifest, work_dir)
        if not written:
            print("Nothing written — aborting, no zip created.")
            return
        zip_path = zip_and_archive(work_dir)
        print(f"\nArchived {sum(written.values())} file(s) {dict(written)} -> {zip_path}")
    finally:
        shutil.rmtree(work_dir, ignore_errors=True)


if __name__ == "__main__":
    main()
