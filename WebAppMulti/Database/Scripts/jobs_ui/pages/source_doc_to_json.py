import sys
from pathlib import Path

import streamlit as st

from jobs_ui.runner import run_streaming

SCRIPT_DIR = Path(__file__).resolve().parents[2]
AGENT_SCRIPT = SCRIPT_DIR / "session_doc_agent.py"

st.title("Source Doc → Session JSON")
st.caption("Front end for session_doc_agent.py — runs the real CLI, nothing reimplemented here.")

with st.form("run_form"):
    case_id = st.number_input("Case ID", min_value=1, step=1, value=None, placeholder="e.g. 7")
    file_path = st.text_input("File", placeholder=r"C:\path\to\session.pdf (leave blank if using Doc ID)")
    src_doc_id = st.number_input("Doc ID", min_value=1, step=1, value=None,
                                  placeholder="already-uploaded source doc id (leave blank if using File)")
    dest = st.text_input("Dest", placeholder=r"C:\temp\session-context (optional — keeps working files)")
    submitted = st.form_submit_button("Run")

if submitted:
    if not case_id:
        st.error("Case ID is required.")
    elif not file_path and not src_doc_id:
        st.error("Provide either File or Doc ID.")
    elif file_path and src_doc_id:
        st.error("Provide only one of File or Doc ID, not both.")
    else:
        # -u forces the child process's stdout to be unbuffered — without it, Python fully
        # buffers stdout when it's not a real terminal (i.e. when piped like this), so output
        # would arrive in one big burst at the end instead of streaming line by line.
        cmd = [sys.executable, "-u", str(AGENT_SCRIPT), "run", "--case-id", str(int(case_id))]
        cmd += ["--src-doc-id", str(int(src_doc_id))] if src_doc_id else ["--file", file_path]
        if dest:
            cmd += ["--dest", dest]

        run_streaming(cmd, cwd=SCRIPT_DIR, key_prefix="source_doc")
