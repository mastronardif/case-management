"""
session_get_zip.py — fetches [source document, projection.json, rule.json] for a session and
writes them into a folder as loose files (the same shape session_doc_agent.py's `local`
command consumes), plus a zip of the same three files for archival/hand-off. Skips the manual
"run pack, then Expand-Archive it yourself" step.

Usage:
    python session_get_zip.py --case-id 5 --src-doc-id 1978 --dir "C:\\temp\\session-context-5-1978"
"""

import argparse
import os
import zipfile

from session_doc_agent import CONTENT_TYPE_EXT, get_active_projector_rule, get_document


def main():
    parser = argparse.ArgumentParser(description="Fetch [source, projection.json, rule.json] into a folder")
    parser.add_argument("--case-id", type=int, required=True, help="For your own reference/logging")
    parser.add_argument("--src-doc-id", type=int, required=True)
    parser.add_argument("--dir", required=True)
    args = parser.parse_args()

    os.makedirs(args.dir, exist_ok=True)

    print(f"Case {args.case_id}, source doc {args.src_doc_id} -> {args.dir}")

    source_bytes, source_content_type = get_document(args.src_doc_id)
    source_ext = CONTENT_TYPE_EXT.get(source_content_type, ".bin")
    source_name = f"source{source_ext}"
    with open(os.path.join(args.dir, source_name), "wb") as f:
        f.write(source_bytes)
    print(f"  wrote {source_name}")

    projection_doc_id, rule_doc_id = get_active_projector_rule("Session")
    print(f"  Session projection/rule: {projection_doc_id} / {rule_doc_id}")

    projection_bytes, _ = get_document(projection_doc_id)
    with open(os.path.join(args.dir, "projection.json"), "wb") as f:
        f.write(projection_bytes)
    print("  wrote projection.json")

    rule_bytes, _ = get_document(rule_doc_id)
    with open(os.path.join(args.dir, "rule.json"), "wb") as f:
        f.write(rule_bytes)
    print("  wrote rule.json")

    zip_path = os.path.join(args.dir, f"session-context-{args.case_id}-{args.src_doc_id}.zip")
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.writestr(source_name, source_bytes)
        zf.writestr("projection.json", projection_bytes)
        zf.writestr("rule.json", rule_bytes)
    print(f"  wrote {os.path.basename(zip_path)}")

    print(f"\nDone: {args.dir}")


if __name__ == "__main__":
    main()
