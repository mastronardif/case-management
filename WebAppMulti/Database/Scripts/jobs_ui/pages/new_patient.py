"""
First pass, deliberately light: a checklist showing the case manager what documents a new
patient needs, one card per doc. Not functional yet — no upload, no extraction trigger here.
The case manager still gets the actual docs to Frank, who runs them through the existing tools
(Source Doc -> JSON, Evernorth) by hand. Cards for tables that already have a working extraction
pipeline link straight to that tool; cards for tables that don't (Intake, Insurance) say so plainly.
"""

import streamlit as st

st.title("New Patient")
st.caption("Checklist of the documents a new patient needs — not a form. Get these docs to Frank; "
           "he runs them through the existing tools.")

CARDS = [
    {
        "title": "Intake (Patient)",
        "doc": "New client intake form — demographics, guardian/contact info.",
        "table": "Case + Patient",
        "ready": False,
    },
    {
        "title": "Insurance",
        "doc": "Copy of the insurance card / coverage info.",
        "table": "InsuranceCoverage",
        "ready": False,
    },
    {
        "title": "Authorization",
        "doc": "The active authorization letter from the payer (the approval, with the real auth number).",
        "table": "Authorization",
        "ready": True,
    },
    {
        "title": "Prior Auth",
        "doc": "The ABA Prior Authorization request form sent to the payer (e.g. Evernorth's form).",
        "table": "PriorAuthForm",
        "ready": True,
    },
    {
        "title": "Treatment Plan",
        "doc": "The treatment plan (re)authorization narrative supporting the prior auth request.",
        "table": "TreatmentPlanAuthorization",
        "ready": True,
    },
]


def render_card(card):
    with st.container(border=True):
        st.markdown(f"**{card['title']}**")
        st.caption(card["doc"])
        if card["ready"]:
            st.success(f"→ `cases.{card['table']}`")
            st.markdown("[Source Doc → JSON](http://localhost:8501/source_doc_to_json)")
        else:
            st.warning(f"→ `cases.{card['table']}` — not built yet")


row1 = st.columns(3)
for col, card in zip(row1, CARDS[:3]):
    with col:
        render_card(card)

row2 = st.columns(3)
for col, card in zip(row2, CARDS[3:]):
    with col:
        render_card(card)
