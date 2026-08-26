"""
Automates the "Session Doc -> Session JSON" process (Tests/SessionDocToJson-Process.md).

Three commands:

  pack    Uploads the session source document (or reuses one already uploaded), pulls the
          currently active "Session" projection + rule docs, and bundles all three into a
          local zip — the same [doc, projection, rule] context described in the process doc's
          docContextPack step. Prints the zip path and stops, for when you want to hand the
          extraction to an interactive Claude Code chat yourself.

  run     Does the whole thing end-to-end: pack, then a headless Claude Code call
          ("claude -p ...") to extract the JSON, then finish. Uses your normal Claude Code
          subscription auth (regular -p, not --bare) — no separate API key. Working files
          (source.*, projection.json, rule.json, session.json) go to a temp folder that's
          deleted afterward, unless you pass --dest to keep them in a folder you choose.

  finish  Takes JSON someone (or `run`/`local`) already extracted, saves it, runs the (V)
          projectorComparer step to validate it and produce a review page, and prints the
          review link.

  local   Same extraction as `run`, but against an already-unzipped folder (e.g. one `pack`
          produced earlier) instead of re-fetching from the DB. Writes session.json into that
          folder. Add --case-id/--src-doc-id to also chain into finish.

`run` and `local` both build their prompt from ask.claude.txt — the same template file you can
also use directly for a manual/interactive extraction (see that file's header comment). One
prompt source, not two: session_extraction_notes.md corrections get merged in automatically.

Never commits to cases.Session itself — Save & Resolve on the review page is always a manual
click, regardless of which command produced the JSON.

Usage:
    python session_doc_agent.py run --case-id 5 --file "C:\\path\\to\\session.pdf"
    python session_doc_agent.py run --case-id 5 --src-doc-id 1626
    python session_doc_agent.py run --case-id 5 --src-doc-id 3532 --dest "C:\\temp\\session-5-3532"
    python session_doc_agent.py run --case-id 5 --src-doc-id 3533 --dest "C:\\temp\\aug26\\session-5-3533"
    

    python session_doc_agent.py pack --case-id 5 --file "C:\\path\\to\\session.pdf"
    python session_doc_agent.py local --dir "C:\\temp\\session-context-5-1976"
    python session_doc_agent.py finish --case-id 5 --src-doc-id 3532 --json-file extracted.json
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
NOTES_FILE  = os.path.join(WORK_DIR, "session_extraction_notes.md")
PROMPT_TEMPLATE_FILE = os.path.join(WORK_DIR, "ask.claude.txt")

CONTENT_TYPE_EXT = {
    "application/pdf": ".pdf",
    "image/jpeg": ".jpg",
    "image/png": ".png",
    "image/tiff": ".tiff",
}
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


def upload_document(case_id, file_path):
    with open(file_path, "rb") as f:
        files = {"file": (os.path.basename(file_path), f)}
        data = {"caseId": case_id, "documentType": "SessionSource"}
        resp = requests.post(f"{API_BASE}/uploadDocument", data=data, files=files, timeout=60, verify=False)
    resp.raise_for_status()
    return resp.json()["docId"]


def save_json_document(json_str, name):
    resp = requests.post(f"{API_BASE}/saveWorkflow", json={"json": json_str, "name": name}, timeout=30, verify=False)
    resp.raise_for_status()
    return resp.json()["docId"]


def run_pipeline_step(extra_args, case_id):
    cmd = ["dotnet", "run", "--"] + extra_args + ["--case-id", str(case_id)]
    result = subprocess.run(cmd, cwd=PROJECT_DIR, capture_output=True, text=True)
    output = result.stdout + result.stderr
    print(output)
    doc_ids = [int(m) for m in re.findall(r"docId\s+(\d+)\s", output)]
    if not doc_ids:
        raise RuntimeError("No output docId found — see log above.")
    return doc_ids


def resolve_source_and_context(case_id, file, src_doc_id):
    """Resolve the source doc (upload if needed) plus the active Session projection/rule.
    Returns (src_doc_id, source_bytes, source_ext, projection_doc_id, rule_doc_id,
    projection_bytes, rule_bytes)."""
    if src_doc_id:
        print(f"Using existing source doc {src_doc_id}")
    else:
        print(f"Uploading {file}...")
        src_doc_id = upload_document(case_id, file)
        print(f"  source doc: {src_doc_id}")

    source_bytes, source_content_type = get_document(src_doc_id)
    source_ext = CONTENT_TYPE_EXT.get(source_content_type, ".bin")

    projection_doc_id, rule_doc_id = get_active_projector_rule("Session")
    print(f"Session projection/rule: {projection_doc_id} / {rule_doc_id}")
    projection_bytes, _ = get_document(projection_doc_id)
    rule_bytes, _ = get_document(rule_doc_id)

    return src_doc_id, source_bytes, source_ext, projection_doc_id, rule_doc_id, projection_bytes, rule_bytes


# ── pack ──────────────────────────────────────────────────────────────────────

def cmd_pack(args):
    src_doc_id, source_bytes, source_ext, projection_doc_id, rule_doc_id, projection_bytes, rule_bytes = \
        resolve_source_and_context(args.case_id, args.file, args.src_doc_id)

    zip_path = os.path.join(WORK_DIR, f"session-context-{args.case_id}-{src_doc_id}.zip")
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.writestr(f"source{source_ext}", source_bytes)
        zf.writestr("projection.json", projection_bytes)
        zf.writestr("rule.json", rule_bytes)

    print(f"\nPacked: {zip_path}")
    print("Hand this zip to Claude Code and ask it to extract the session JSON - it should")
    print("read projection.json's \"source\" paths (and rule.json's requiredFields) to know")
    print("exactly what to produce from source" + source_ext + ".")
    print(f"\nWhen you have the extracted JSON saved to a file, run:")
    print(f"  python session_doc_agent.py finish --case-id {args.case_id} --src-doc-id {src_doc_id} --json-file <path>")


# ── run (pack + headless extract + finish) ──────────────────────────────────────

def load_extraction_notes():
    if not os.path.exists(NOTES_FILE):
        return ""
    with open(NOTES_FILE, "r", encoding="utf-8") as f:
        return f.read().strip()


def build_extraction_prompt(folder):
    """Loads the shared ask.claude.txt template (same one used for manual/interactive runs)
    and substitutes {folder}/{notes}/{returnInstruction} for a headless, Read-only, stdout-
    captured run — Claude reads projection.json/rule.json/source.* itself from `folder`."""
    with open(PROMPT_TEMPLATE_FILE, "r", encoding="utf-8") as f:
        template = strip_comments(f.read())

    notes = load_extraction_notes()
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

    prompt = build_extraction_prompt(dir_path)

    print("Calling `claude -p` for extraction (uses your Claude Code subscription auth)...")
    result_text = ask_claude(prompt, cwd=dir_path)
    try:
        extracted = extract_json_object(result_text)
    except json.JSONDecodeError as ex:
        sys.exit(f"claude -p did not return valid JSON: {ex}\n---\n{result_text}")

    out_path = os.path.join(dir_path, "session.json")
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(extracted, f, indent=2)
    print(f"Wrote {out_path}")
    print(json.dumps(extracted, indent=2))

    if args.case_id and args.src_doc_id:
        projection_doc_id, _ = get_active_projector_rule("Session")
        finish(args.case_id, args.src_doc_id, extracted, projection_doc_id)
    else:
        print("\nNo --case-id/--src-doc-id given - stopped after writing session.json locally.")
        print("Run `finish` with those options (and --json-file) to save/validate it.")


def cmd_run(args):
    src_doc_id, source_bytes, source_ext, projection_doc_id, rule_doc_id, projection_bytes, rule_bytes = \
        resolve_source_and_context(args.case_id, args.file, args.src_doc_id)

    # --dest keeps the working files around for inspection (e.g. re-running extraction by
    # hand); without it, this is scratch space that gets cleaned up after the run like before.
    keep_dir = bool(args.dest)
    work_dir = args.dest or tempfile.mkdtemp(prefix="session_doc_agent_")
    if keep_dir:
        os.makedirs(work_dir, exist_ok=True)
    try:
        with open(os.path.join(work_dir, f"source{source_ext}"), "wb") as f:
            f.write(source_bytes)
        with open(os.path.join(work_dir, "projection.json"), "wb") as f:
            f.write(projection_bytes)
        with open(os.path.join(work_dir, "rule.json"), "wb") as f:
            f.write(rule_bytes)
        if keep_dir:
            print(f"Working files: {work_dir}")

        prompt = build_extraction_prompt(work_dir)

        print("Calling `claude -p` for extraction (uses your Claude Code subscription auth)...")
        result_text = ask_claude(prompt, cwd=work_dir)
        try:
            extracted = extract_json_object(result_text)
        except json.JSONDecodeError as ex:
            sys.exit(f"claude -p did not return valid JSON: {ex}\n---\n{result_text}")
        print(json.dumps(extracted, indent=2))

        if keep_dir:
            with open(os.path.join(work_dir, "session.json"), "w", encoding="utf-8") as f:
                json.dump(extracted, f, indent=2)
    finally:
        if not keep_dir:
            shutil.rmtree(work_dir, ignore_errors=True)

    finish(args.case_id, src_doc_id, extracted, projection_doc_id)


# ── finish ────────────────────────────────────────────────────────────────────

def finish(case_id, src_doc_id, extracted, projection_doc_id):
    # Provenance travels with the JSON itself, not just the external claim-sources doc — so it
    # survives even if this doc is later looked at in isolation.
    extracted["sourceDocs"] = [src_doc_id]

    json_doc_id = save_json_document(json.dumps(extracted, indent=2), "session-note-ai")
    print(f"Saved extracted JSON: {json_doc_id}")

    expr = f"{json_doc_id} (V) {projection_doc_id}"
    print(f"Validating: {expr}")
    doc_ids = run_pipeline_step(
        ["--expression", expr, "--table-name", "Session", "--src-doc-id", str(src_doc_id)],
        case_id,
    )

    print("\n=== Review before committing ===")
    for doc_id in doc_ids:
        print(f"  http://localhost:5173/api/getDocument?docId={doc_id}")
    print("Open the review HTML link above and click 'Save & Resolve' when it looks right.")
    print("This agent does not commit to cases.Session on its own.")


def cmd_finish(args):
    with open(args.json_file, "r", encoding="utf-8") as f:
        extracted = json.load(f)  # fail fast here if the JSON file wasn't valid

    projection_doc_id, _ = get_active_projector_rule("Session")
    finish(args.case_id, args.src_doc_id, extracted, projection_doc_id)


# ── Main ──────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="Session Doc -> Session JSON agent")
    sub = parser.add_subparsers(dest="command", required=True)

    def add_source_args(p):
        p.add_argument("--case-id", type=int, required=True)
        p.add_argument("--file", help="Path to the raw session document to upload")
        p.add_argument("--src-doc-id", type=int, help="Already-uploaded source document id")

    pack_parser = sub.add_parser("pack", help="Upload + bundle [doc, projection, rule] into a zip")
    add_source_args(pack_parser)
    pack_parser.set_defaults(func=cmd_pack)

    run_parser = sub.add_parser("run", help="Full pipeline: pack, headless extract via claude -p, finish")
    add_source_args(run_parser)
    run_parser.add_argument("--dest", help="Folder to write source/projection/rule/session.json into "
                                            "(default: temp folder, deleted after the run)")
    run_parser.set_defaults(func=cmd_run)

    finish_parser = sub.add_parser("finish", help="Save extracted JSON, validate, print review link")
    finish_parser.add_argument("--case-id", type=int, required=True)
    finish_parser.add_argument("--src-doc-id", type=int, required=True)
    finish_parser.add_argument("--json-file", required=True, help="Path to already-extracted JSON")
    finish_parser.set_defaults(func=cmd_finish)

    local_parser = sub.add_parser("local", help="Extract from an already-unzipped [source, projection.json, rule.json] folder")
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
