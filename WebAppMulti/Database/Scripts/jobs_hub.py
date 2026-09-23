"""
Back-office jobs hub — the root page listing the background jobs, each opening its own page.
Scratch/dev tool that lives next to the scripts until the flows get folded into the main app.

Usage:
    pip install -r requirements.txt
    streamlit run jobs_hub.py
"""

import streamlit as st

from jobs_ui.registry import JOBS, STATUS_COLORS

st.set_page_config(page_title="Back Office Jobs", layout="wide")

job_pages = {
    job["id"]: st.Page(job["page"], title=job["title"], url_path=job["id"])
    for job in JOBS if job.get("page")
}


def home():
    st.title("Back Office Jobs")
    st.caption("Launcher for the background jobs. Each one runs the real CLI or service — nothing is reimplemented here.")

    for job in JOBS:
        with st.container(border=True):
            left, right = st.columns([5, 1])
            with left:
                st.subheader(job["title"])
                st.write(job["description"])
                if job.get("command"):
                    st.code(job["command"], language="powershell")
            with right:
                st.markdown(f":{STATUS_COLORS[job['status']]}-badge[{job['status']}]")
                if job["id"] in job_pages:
                    st.page_link(job_pages[job["id"]], label="Open")


st.navigation([st.Page(home, title="Jobs", default=True), *job_pages.values()]).run()
