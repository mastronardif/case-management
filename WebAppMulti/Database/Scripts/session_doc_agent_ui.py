"""
Dummy front end for session_doc_agent.py — a Streamlit form wrapping the existing CLI, not a
reimplementation of it. Just builds the same command you'd type by hand and streams its output.
Scratch/dev tool: lives next to the script until the real flow gets folded into the main app.

Usage:
    pip install streamlit
    streamlit run session_doc_agent_ui.py
"""

import re
import subprocess
import sys
from pathlib import Path

import streamlit as st

SCRIPT_DIR = Path(__file__).resolve().parent
AGENT_SCRIPT = SCRIPT_DIR / "session_doc_agent.py"

# A plain <textarea> can't render clickable links, so doc URLs printed by session_doc_agent.py
# (e.g. "http://localhost:5173/api/getDocument?docId=4618") are pulled out of the streamed
# output separately and rendered as real target="_blank" anchors alongside it.
DOC_LINK_PATTERN = re.compile(r"https?://\S+?getDocument\?docId=\d+")

# Matches "[SHADOW\ask_claude] rest of message", capturing the source name (ask_claude) and
# the message separately so the source stays visible in the panel instead of being stripped.
SHADOW_PATTERN = re.compile(r"\[SHADOW\\(\w+)\]\s*(.*)")

st.set_page_config(page_title="Session Doc -> JSON", layout="wide")
st.title("Source Doc -> Session JSON")
st.caption("Dummy front end for session_doc_agent.py — runs the real CLI, nothing reimplemented here.")

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

        st.caption("Command:")
        st.code(" ".join(cmd), language="powershell")

        main_col, shadow_col = st.columns([3, 1])
        with main_col:
            output_box = st.empty()
            links_box = st.empty()
        with shadow_col:
            st.markdown("**Shadow**")
            shadow_box = st.empty()

        lines = []
        seen_links = []
        shadow_lines = []

        def render_shadow(done=False):
            body = "\n\n".join(shadow_lines) or "_(nothing yet)_"
            if done:
                body += "\n\n**Done**"
            shadow_box.markdown(body)

        with st.spinner("Running..."):
            process = subprocess.Popen(
                cmd, cwd=SCRIPT_DIR, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                text=True, bufsize=1,
            )
            for line in process.stdout:
                stripped = line.rstrip()

                # ask_claude.py's nested "claude -p" subprocess — kept out of the main log and
                # shown in its own column instead, since it's a different process's activity
                # bracketing session_doc_agent.py's own narrative, not part of it.
                shadow_match = SHADOW_PATTERN.search(stripped)
                if shadow_match:
                    source, message = shadow_match.groups()
                    shadow_lines.append(f"**{source}** — {message}")
                    render_shadow()
                    continue

                lines.append(stripped)
                output_box.text_area("Output", "\n".join(lines), height=400, key=f"output_{len(lines)}")

                for url in DOC_LINK_PATTERN.findall(line):
                    if url not in seen_links:
                        seen_links.append(url)
                if seen_links:
                    links_html = "<br>".join(
                        f'<a href="{url}" target="_blank" rel="noopener">{url}</a>' for url in seen_links
                    )
                    links_box.markdown(links_html, unsafe_allow_html=True)
            process.wait()

        render_shadow(done=True)

        if process.returncode == 0:
            st.success("Done.")
        else:
            st.error(f"Exited with code {process.returncode}.")
