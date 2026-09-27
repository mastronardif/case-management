"""
Automates "source doc -> AI-extracted JSON -> validate -> review link" for any [cases] table
that has a CaseId/SourceDocumentId/JsonDocumentId resolve target (Tests/SessionDocToJson-Process.md
documents the original Session-only version of this flow). --table-name selects which table's
active projection + rule to extract against; it defaults to "Session", so every command below
that omits it behaves exactly as before the script was generalized.

Three commands:

  pack    Uploads the source document (or reuses one already uploaded), pulls the target table's
          currently active projection + rule docs, and bundles all three into a local zip — the
          same [doc, projection, rule] context described in the process doc's docContextPack
          step. Prints the zip path and stops, for when you want to hand the extraction to an
          interactive Claude Code chat yourself.

  run     Does the whole thing end-to-end: pack, then a headless Claude Code call
          ("claude -p ...") to extract the JSON, then finish. Uses your normal Claude Code
          subscription auth (regular -p, not --bare) — no separate API key. Working files
          (source.*, projection.json, rule.json, extracted.json) go to a temp folder that's
          deleted afterward, unless you pass --dest to keep them in a folder you choose.

  finish  Takes JSON someone (or `run`/`local`) already extracted, saves it, runs the (V)
          projectorComparer step to validate it and produce a review page, and prints the
          review link.

  local   Same extraction as `run`, but against an already-unzipped folder (e.g. one `pack`
          produced earlier) instead of re-fetching from the DB. Writes extracted.json into that
          folder. Add --case-id/--src-doc-id to also chain into finish.

`run` and `local` both build their prompt from ask.claude.txt — the same template file you can
also use directly for a manual/interactive extraction (see that file's header comment). One
prompt source, not two: a per-table "known corrections" file (e.g. session_extraction_notes.md,
authorization_extraction_notes.md — see --table-name) gets merged in automatically. A table with
no notes file yet just runs without one; there's nothing to create until you have a correction
to record.

Never commits to the target table itself — Save & Resolve on the review page is always a manual
click, regardless of which command produced the JSON.

Usage:
    python doc_extraction_agent.py run --case-id 5 --file "C:\\path\\to\\session.pdf"
    python doc_extraction_agent.py run --case-id 5 --src-doc-id 1626
    python doc_extraction_agent.py run --case-id 5 --src-doc-id 3532 --dest "C:\\temp\\session-5-3532"
    python doc_extraction_agent.py run --case-id 5 --src-doc-id 3533 --dest "C:\\temp\\aug26\\session-5-3533"
    python doc_extraction_agent.py run --table-name Authorization --doc-type Authorization \\
        --case-id 5 --src-doc-id 5023

    python doc_extraction_agent.py pack --case-id 5 --file "C:\\path\\to\\session.pdf"
    python doc_extraction_agent.py local --dir "C:\\temp\\session-context-5-1976"
    python doc_extraction_agent.py finish --case-id 5 --src-doc-id 3532 --json-file extracted.json
"""

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile

import pyodbc
import requests
import urllib3

from ask_claude import ask_claude, strip_comments, substitute_vars

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# === CONFIG ===
SERVER      = r"LAPTOP-JIH94VS9\SQLEXPRESS"
DATABASE    = "CaseManagement"
# Hits the backend directly, not through the Vite dev server's proxy — this script only needs
# WebAppMulti running, not the frontend. verify=False since the dev cert is self-signed.
API_BASE    = "https://localhost:44344/api"
PROJECT_DIR = r"C:\Users\mastronardif\source\repos\CaseMangement\CaseManagement.Jobs\src\CaseManagement.SessionBillResolvers.V2"
WORK_DIR    = os.path.dirname(os.path.abspath(__file__))
PROMPT_TEMPLATE_FILE = os.path.join(WORK_DIR, "ask.claude.txt")

CONTENT_TYPE_EXT = {
    "application/pdf": ".pdf",
    "image/jpeg": ".jpg",
    "image/png": ".png",
    "image/tiff": ".tiff",
}

# Real file type from the first bytes. Documents uploaded through the UI are stored as
# application/octet-stream, so the stored content type can't be trusted — and Claude's Read tool
# opens a file as PDF/image or "binary" purely by its extension, so a PDF saved as source.bin
# can't be read at all (headless claude then flails and answers in prose instead of JSON).
FILE_SIGNATURES = [
    (b"\x89PNG\r\n\x1a\n", ".png"),
    (b"\xff\xd8\xff", ".jpg"),
    (b"II*\x00", ".tiff"),
    (b"MM\x00*", ".tiff"),
    (b"GIF87a", ".gif"),
    (b"GIF89a", ".gif"),
]
# ===============================


def get_conn():
    conn_str = (
        "DRIVER={ODBC Driver 18 for SQL Server};"
        f"SERVER={SERVER};DATABASE={DATABASE};"
        "Trusted_Connection=yes;TrustServerCertificate=yes;"
    )
    return pyodbc.connect(conn_str)


def get_active_projector_rule(name):
    with get_conn() as conn:
        cursor = conn.cursor()
        cursor.execute(
            "SELECT ProjectionDocumentId, RuleDocumentId FROM [cases].[ProjectorRule] "
            "WHERE Name = ? AND IsActive = 1",
            name,
        )
        row = cursor.fetchone()
        if row is None:
            raise RuntimeError(f"No active ProjectorRule row named '{name}'")
        return row.ProjectionDocumentId, row.RuleDocumentId


def get_document(doc_id):
    resp = requests.get(f"{API_BASE}/getDocument", params={"docId": doc_id}, timeout=30, verify=False)
    resp.raise_for_status()
    content_type = resp.headers.get("Content-Type", "").split(";")[0].strip()
    return resp.content, content_type


def detect_source_ext(data, content_type):
    """File extension for the source doc: what the bytes say first, the stored content type second,
    ".bin" only as a last resort."""
    # The PDF spec allows a little junk before the header, so look in the first 1 KB, not just byte 0.
    if b"%PDF-" in data[:1024]:
        return ".pdf"
    for signature, ext in FILE_SIGNATURES:
        if data.startswith(signature):
            return ext
    if data[:4] == b"RIFF" and data[8:12] == b"WEBP":
        return ".webp"
    return CONTENT_TYPE_EXT.get(content_type, ".bin")


def write_source_file(work_dir, source_ext, source_bytes):
    """Writes source<ext>, first removing any other source.* left in a reused --dest folder —
    two source files would leave Claude guessing which one to read."""
    for name in os.listdir(work_dir):
        if name.startswith("source."):
            os.remove(os.path.join(work_dir, name))
    with open(os.path.join(work_dir, f"source{source_ext}"), "wb") as f:
        f.write(source_bytes)


def upload_document(case_id, file_path, doc_type):
    with open(file_path, "rb") as f:
        files = {"file": (os.path.basename(file_path), f)}
        data = {"caseId": case_id, "documentType": doc_type}
        resp = requests.post(f"{API_BASE}/uploadDocument", data=data, files=files, timeout=60, verify=False)
    resp.raise_for_status()
    return resp.json()["docId"]


def save_json_document(json_str, name):
    resp = requests.post(f"{API_BASE}/saveWorkflow", json={"json": json_str, "name": name}, timeout=30, verify=False)
    resp.raise_for_status()
    return resp.json()["docId"]


def run_pipeline_step(extra_args, case_id):
    cmd = ["dotnet", "run", "--"] + extra_args + ["--case-id", str(case_id)]
    # [SHADOW\dotnet] brackets this the same way ask_claude.py brackets its own nested
    # subprocess — subprocess.run(capture_output=True) blocks until dotnet fully exits, so
    # there's no live output during the call itself, just a start/end marker either side of it.
    print(f"[SHADOW\\dotnet] cmd: {' '.join(cmd)}", file=sys.stderr, flush=True)
    result = subprocess.run(cmd, cwd=PROJECT_DIR, capture_output=True, text=True)
    print(f"[SHADOW\\dotnet] done: exit={result.returncode}", file=sys.stderr, flush=True)
    output = result.stdout + result.stderr
    print(output)
    doc_ids = [int(m) for m in re.findall(r"docId\s+(\d+)\s", output)]
    if not doc_ids:
        raise RuntimeError("No output docId found — see log above.")
    return doc_ids


def resolve_source_and_context(case_id, file, src_doc_id, table_name, doc_type):
    """Resolve the source doc (upload if needed) plus the target table's active projection/rule.
    Returns (src_doc_id, source_bytes, source_ext, projection_doc_id, rule_doc_id,
    projection_bytes, rule_bytes)."""
    if src_doc_id:
        print(f"Using existing source doc {src_doc_id}")
    else:
        print(f"Uploading {file}...")
        src_doc_id = upload_document(case_id, file, doc_type)
        print(f"  source doc: {src_doc_id}")

    source_bytes, source_content_type = get_document(src_doc_id)
    source_ext = detect_source_ext(source_bytes, source_content_type)
    if source_ext == ".bin":
        print(f"WARNING: couldn't identify the source file type (stored as {source_content_type!r}); "
              "Claude can't read a .bin, so extraction will likely fail.")

    projection_doc_id, rule_doc_id = get_active_projector_rule(table_name)
    print(f"{table_name} projection/rule: {projection_doc_id} / {rule_doc_id}")
    projection_bytes, _ = get_document(projection_doc_id)
    rule_bytes, _ = get_document(rule_doc_id)

    return src_doc_id, source_bytes, source_ext, projection_doc_id, rule_doc_id, projection_bytes, rule_bytes


# ── pack ──────────────────────────────────────────────────────────────────────

def cmd_pack(args):
    src_doc_id, source_bytes, source_ext, projection_doc_id, rule_doc_id, projection_bytes, rule_bytes = \
        resolve_source_and_context(args.case_id, args.file, args.src_doc_id, args.table_name, args.doc_type)

    zip_path = os.path.join(WORK_DIR, f"{args.table_name.lower()}-context-{args.case_id}-{src_doc_id}.zip")
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.writestr(f"source{source_ext}", source_bytes)
        zf.writestr("projection.json", projection_bytes)
        zf.writestr("rule.json", rule_bytes)

    print(f"\nPacked: {zip_path}")
    print("Hand this zip to Claude Code and ask it to extract the JSON - it should")
    print("read projection.json's \"source\" paths (and rule.json's requiredFields) to know")
    print("exactly what to produce from source" + source_ext + ".")
    print(f"\nWhen you have the extracted JSON saved to a file, run:")
    print(f"  python doc_extraction_agent.py finish --table-name {args.table_name} "
          f"--case-id {args.case_id} --src-doc-id {src_doc_id} --json-file <path>")


# ── run (pack + headless extract + finish) ──────────────────────────────────────

def notes_file_for(table_name):
    return os.path.join(WORK_DIR, f"{table_name.lower()}_extraction_notes.md")


def load_extraction_notes(table_name):
    notes_file = notes_file_for(table_name)
    if not os.path.exists(notes_file):
        return ""
    with open(notes_file, "r", encoding="utf-8") as f:
        return f.read().strip()


def build_extraction_prompt(folder, table_name):
    """Loads the shared ask.claude.txt template (same one used for manual/interactive runs)
    and substitutes {folder}/{notes}/{returnInstruction} for a headless, Read-only, stdout-
    captured run — Claude reads projection.json/rule.json/source.* itself from `folder`."""
    with open(PROMPT_TEMPLATE_FILE, "r", encoding="utf-8") as f:
        template = strip_comments(f.read())

    notes = load_extraction_notes(table_name)
    notes_section = f"Known corrections from past extraction runs — follow these:\n{notes}" if notes else ""
    return_instruction = (
        "Your final answer must be ONLY the JSON object as plain text — no markdown code "
        "fences, no commentary. Do not write, edit, or create any files, even if one with a "
        "similar name already exists in this directory — the caller will save it."
    )

    return substitute_vars(template, [
        f"folder={folder}",
        f"notes={notes_section}",
        f"returnInstruction={return_instruction}",
    ])


def extract_json_object(text):
    # The prompt asks for pure JSON with no commentary, but Claude sometimes prepends a short
    # note anyway (e.g. flagging a prompt-injection attempt found in the source files — good
    # behavior, just not "ONLY the JSON object"). Don't rely on prompt compliance: look for a
    # fenced block anywhere in the text first, then fall back to the outermost {...} span.
    fence_match = re.search(r"```(?:json)?\s*(\{.*?\})\s*```", text, re.DOTALL)
    if fence_match:
        return json.loads(fence_match.group(1))

    start, end = text.find("{"), text.rfind("}")
    if start != -1 and end > start:
        return json.loads(text[start:end + 1])

    return json.loads(text.strip())


# ── local (already-unzipped folder, no DB fetch) ────────────────────────────────

def find_source_file(dir_path):
    for name in sorted(os.listdir(dir_path)):
        if name.startswith("source."):
            return os.path.join(dir_path, name)
    raise RuntimeError(f"No 'source.*' file found in {dir_path}")


def cmd_local(args):
    dir_path = os.path.abspath(args.dir)
    find_source_file(dir_path)  # just validates a source.* file is present
    projection_path = os.path.join(dir_path, "projection.json")
    rule_path = os.path.join(dir_path, "rule.json")
    if not os.path.exists(projection_path) or not os.path.exists(rule_path):
        sys.exit(f"Expected projection.json and rule.json in {dir_path}")

    prompt = build_extraction_prompt(dir_path, args.table_name)

    print("Calling `claude -p` for extraction (uses your Claude Code subscription auth)...")
    result_text = ask_claude(prompt, cwd=dir_path)
    try:
        extracted = extract_json_object(result_text)
    except json.JSONDecodeError as ex:
        sys.exit(f"claude -p did not return valid JSON: {ex}\n---\n{result_text}")

    out_path = os.path.join(dir_path, "extracted.json")
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(extracted, f, indent=2)
    print(f"Wrote {out_path}")
    print(json.dumps(extracted, indent=2))

    if args.case_id and args.src_doc_id:
        projection_doc_id, _ = get_active_projector_rule(args.table_name)
        finish(args.case_id, args.src_doc_id, extracted, projection_doc_id, args.table_name)
    else:
        print("\nNo --case-id/--src-doc-id given - stopped after writing extracted.json locally.")
        print("Run `finish` with those options (and --json-file) to save/validate it.")


def cmd_run(args):
    src_doc_id, source_bytes, source_ext, projection_doc_id, rule_doc_id, projection_bytes, rule_bytes = \
        resolve_source_and_context(args.case_id, args.file, args.src_doc_id, args.table_name, args.doc_type)

    # --dest keeps the working files around for inspection (e.g. re-running extraction by
    # hand); without it, this is scratch space that gets cleaned up after the run like before.
    keep_dir = bool(args.dest)
    work_dir = args.dest or tempfile.mkdtemp(prefix="doc_extraction_agent_")
    if keep_dir:
        os.makedirs(work_dir, exist_ok=True)
    try:
        write_source_file(work_dir, source_ext, source_bytes)
        with open(os.path.join(work_dir, "projection.json"), "wb") as f:
            f.write(projection_bytes)
        with open(os.path.join(work_dir, "rule.json"), "wb") as f:
            f.write(rule_bytes)
        if keep_dir:
            print(f"Working files: {work_dir}")

        prompt = build_extraction_prompt(work_dir, args.table_name)

        print("Calling `claude -p` for extraction (uses your Claude Code subscription auth)...")
        result_text = ask_claude(prompt, cwd=work_dir)
        try:
            extracted = extract_json_object(result_text)
        except json.JSONDecodeError as ex:
            sys.exit(f"claude -p did not return valid JSON: {ex}\n---\n{result_text}")
        print(json.dumps(extracted, indent=2))

        if keep_dir:
            with open(os.path.join(work_dir, "extracted.json"), "w", encoding="utf-8") as f:
                json.dump(extracted, f, indent=2)
    finally:
        if not keep_dir:
            shutil.rmtree(work_dir, ignore_errors=True)

    finish(args.case_id, src_doc_id, extracted, projection_doc_id, args.table_name)


# ── finish ────────────────────────────────────────────────────────────────────

def finish(case_id, src_doc_id, extracted, projection_doc_id, table_name):
    # Provenance travels with the JSON itself, not just the external claim-sources doc — so it
    # survives even if this doc is later looked at in isolation.
    extracted["sourceDocs"] = [src_doc_id]

    json_doc_id = save_json_document(json.dumps(extracted, indent=2), f"{table_name.lower()}-note-ai")
    print(f"Saved extracted JSON: {json_doc_id}")

    expr = f"{json_doc_id} (V) {projection_doc_id}"
    print(f"Validating: {expr}")
    doc_ids = run_pipeline_step(
        ["--expression", expr, "--table-name", table_name, "--src-doc-id", str(src_doc_id)],
        case_id,
    )

    print("\n=== Review before committing ===")
    for doc_id in doc_ids:
        print(f"  http://localhost:5173/api/getDocument?docId={doc_id}")
    print("Open the review HTML link above and click 'Save & Resolve' when it looks right.")
    print(f"This agent does not commit to cases.{table_name} on its own.")


def cmd_finish(args):
    with open(args.json_file, "r", encoding="utf-8") as f:
        extracted = json.load(f)  # fail fast here if the JSON file wasn't valid

    projection_doc_id, _ = get_active_projector_rule(args.table_name)
    finish(args.case_id, args.src_doc_id, extracted, projection_doc_id, args.table_name)


# ── Main ──────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="Doc -> JSON extraction agent (any [cases] table)")
    sub = parser.add_subparsers(dest="command", required=True)

    def add_table_arg(p):
        p.add_argument("--table-name", default="Session",
                        help="Target [cases] table — selects which active projection/rule to extract "
                             "against, and the correction notes file (default: Session)")

    def add_source_args(p):
        p.add_argument("--case-id", type=int, required=True)
        p.add_argument("--file", help="Path to the raw source document to upload")
        p.add_argument("--src-doc-id", type=int, help="Already-uploaded source document id")
        p.add_argument("--doc-type", default="SessionSource",
                        help="DocumentType to upload as, when --file is given (default: SessionSource)")

    pack_parser = sub.add_parser("pack", help="Upload + bundle [doc, projection, rule] into a zip")
    add_table_arg(pack_parser)
    add_source_args(pack_parser)
    pack_parser.set_defaults(func=cmd_pack)

    run_parser = sub.add_parser("run", help="Full pipeline: pack, headless extract via claude -p, finish")
    add_table_arg(run_parser)
    add_source_args(run_parser)
    run_parser.add_argument("--dest", help="Folder to write source/projection/rule/extracted.json into "
                                            "(default: temp folder, deleted after the run)")
    run_parser.set_defaults(func=cmd_run)

    finish_parser = sub.add_parser("finish", help="Save extracted JSON, validate, print review link")
    add_table_arg(finish_parser)
    finish_parser.add_argument("--case-id", type=int, required=True)
    finish_parser.add_argument("--src-doc-id", type=int, required=True)
    finish_parser.add_argument("--json-file", required=True, help="Path to already-extracted JSON")
    finish_parser.set_defaults(func=cmd_finish)

    local_parser = sub.add_parser("local", help="Extract from an already-unzipped [source, projection.json, rule.json] folder")
    add_table_arg(local_parser)
    local_parser.add_argument("--dir", required=True, help="Folder containing source.*, projection.json, rule.json")
    local_parser.add_argument("--case-id", type=int, help="If given with --src-doc-id, also runs finish")
    local_parser.add_argument("--src-doc-id", type=int)
    local_parser.set_defaults(func=cmd_local)

    args = parser.parse_args()
    if args.command in ("pack", "run") and not args.file and not args.src_doc_id:
        parser.error("Provide either --file or --src-doc-id")

    args.func(args)


if __name__ == "__main__":
    main()
