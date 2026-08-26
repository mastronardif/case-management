#!/usr/bin/env python3
"""
Build an ImportDocs CSV (CaseNumber,DocumentType,Title,ContentType,FilePath) from every
file in a folder, ready for: dotnet run -- import.csv "<connection string>"

Usage:
    python BuildImportCsv.py --path "C:\\Users\\mastronardif\\Downloads\\Case5\\August" --case CASE-7341693071 --title SessionNote --month Aug
"""

import argparse
import csv
import mimetypes
from pathlib import Path

DEFAULT_OUT = Path(__file__).resolve().parent / "import.csv"


def build_rows(folder: Path, case_number: str, title: str, month: str):
    files = sorted(p for p in folder.iterdir() if p.is_file())
    if not files:
        raise SystemExit(f"No files found in {folder}")

    multiple = len(files) > 1
    rows = []
    for i, f in enumerate(files, start=1):
        row_title = f"{title}.{month}.{i:02d}" if multiple else f"{title}.{month}"
        content_type, _ = mimetypes.guess_type(f.name)
        rows.append({
            "CaseNumber": case_number,
            "DocumentType": title,
            "Title": row_title,
            "ContentType": content_type or "application/octet-stream",
            "FilePath": str(f.resolve()),
        })
    return rows


def main():
    parser = argparse.ArgumentParser(description="Build an ImportDocs CSV from a folder of files.")
    parser.add_argument("--path", required=True, help="Folder containing the files to import")
    parser.add_argument("--case", required=True, help="CaseNumber, e.g. CASE-7341693071")
    parser.add_argument("--title", required=True, help="Base title / DocumentType, e.g. SessionNote")
    parser.add_argument("--month", required=True, help="Month label appended to Title, e.g. Aug")
    parser.add_argument("--out", default=str(DEFAULT_OUT), help=f"Output CSV path (default: {DEFAULT_OUT})")
    args = parser.parse_args()

    folder = Path(args.path)
    if not folder.is_dir():
        raise SystemExit(f"Not a folder: {folder}")

    rows = build_rows(folder, args.case, args.title, args.month)

    out_path = Path(args.out)
    with open(out_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=["CaseNumber", "DocumentType", "Title", "ContentType", "FilePath"])
        writer.writeheader()
        writer.writerows(rows)

    print(f"Wrote {len(rows)} row(s) to {out_path}")
    for row in rows:
        print(f"  {row['Title']:<30} {row['ContentType']:<25} {row['FilePath']}")


if __name__ == "__main__":
    main()
