"""
Shared runner for the jobs hub pages: runs a command as a subprocess and streams its output live
into the page — a resizable output box, clickable doc links, and a Shadow panel for the nested
subprocesses (ask_claude, dotnet) that session_doc_agent.py brackets with [SHADOW\\<source>] lines.
"""

import re
import subprocess

import streamlit as st

# A plain <textarea> can't render clickable links, so doc URLs printed by the job
# (e.g. "http://localhost:5173/api/getDocument?docId=4618") are pulled out of the streamed
# output separately and rendered as real target="_blank" anchors alongside it.
DOC_LINK_PATTERN = re.compile(r"https?://\S+?getDocument\?docId=\d+")

# Matches "[SHADOW\ask_claude] rest of message", capturing the source name (ask_claude) and
# the message separately so the source stays visible in the panel instead of being stripped.
SHADOW_PATTERN = re.compile(r"\[SHADOW\\(\w+)\]\s*(.*)")


def run_streaming(cmd, cwd, key_prefix):
    """Run cmd, streaming output into the page. Returns the process exit code."""
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
            cmd, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
            text=True, bufsize=1,
        )
        for line in process.stdout:
            stripped = line.rstrip()

            shadow_match = SHADOW_PATTERN.search(stripped)
            if shadow_match:
                source, message = shadow_match.groups()
                shadow_lines.append(f"**{source}** — {message}")
                render_shadow()
                continue

            lines.append(stripped)
            output_box.text_area("Output", "\n".join(lines), height=400, key=f"{key_prefix}_output_{len(lines)}")

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
    return process.returncode
