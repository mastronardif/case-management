"""
Layer 1 — flow only. No new schema, no AI calls, no persistence. Just the shape of the four
stages and how they relate, so the sequence feels right before anything underneath is real.

Layer 2 (next): Sheet Generator and Progress Report produce real, self-contained HTML from
dummy/hardcoded data — proves what the output looks like, still decoupled from a real data model.

Later, explicitly not yet: the human-in-the-middle review/approve step and real persistence
(bx.BehaviorPlan, bx.Observation, projection/rule pairs, AI-drafting) — deliberately deferred
until the flow and the output are both already proven on screen.
"""

from datetime import date
from html import escape

import pyodbc
import streamlit as st

from job_logging import DATABASE, SERVER

st.title("Behavior Plan & Progress Reports")
st.caption("Layer 1 — flow skeleton only. Nothing on this page is real yet; stages are stubs.")


def get_conn():
    return pyodbc.connect(
        "DRIVER={ODBC Driver 18 for SQL Server};"
        f"SERVER={SERVER};DATABASE={DATABASE};"
        "Trusted_Connection=yes;TrustServerCertificate=yes;"
    )


def get_case_context(case_id):
    with get_conn() as conn:
        case_row = conn.execute(
            "SELECT CaseNumber, Title FROM cases.[Case] WHERE CaseId = ?", case_id
        ).fetchone()
        if case_row is None:
            return None
        patient_row = conn.execute(
            "SELECT FirstName, LastName, MemberId, DateOfBirth FROM cases.Patient WHERE CaseId = ?", case_id
        ).fetchone()
    return {
        "case_number": case_row.CaseNumber,
        "title": case_row.Title,
        "patient_name": f"{patient_row.FirstName} {patient_row.LastName}" if patient_row else None,
        "member_id": patient_row.MemberId if patient_row else None,
        "dob": patient_row.DateOfBirth if patient_row else None,
    }


# Modeled directly on the real ABA_ABC_Data_Collection_Sheet.xlsx — same header fields, same
# A/B/C + Duration/Intensity/Notes columns. Self-contained HTML, not .xlsx, so it's the same
# "generate -> save to cases.Document -> print/scan back in" shape every other doc in this
# system already uses — and it's what makes the hand-fill-then-scan plan actually work.
def render_abc_sheet_html(context, case_id):
    client_name = escape(context["patient_name"] or context["title"])
    client_id = escape(context["member_id"] or context["case_number"])
    dob = context["dob"].isoformat() if context["dob"] else ""
    rows = "\n".join(
        f'<tr><td>{i}</td><td></td><td></td><td></td><td></td><td></td><td></td><td></td></tr>'
        for i in range(1, 21)
    )
    return f"""<!DOCTYPE html>
<html><head><meta charset="UTF-8"><title>ABC Data Collection Sheet</title>
<style>
  body {{ font-family: Arial, sans-serif; margin: 24px; color: #222; }}
  h1 {{ font-size: 1.3rem; margin-bottom: 4px; }}
  .sub {{ color: #666; margin-bottom: 20px; }}
  table.header td {{ padding: 4px 8px; border: 1px solid #ccc; }}
  table.header {{ border-collapse: collapse; width: 100%; margin-bottom: 16px; }}
  .label {{ font-weight: bold; background: #f5f5f5; width: 140px; }}
  table.data {{ border-collapse: collapse; width: 100%; }}
  table.data th, table.data td {{ border: 1px solid #ccc; padding: 6px; text-align: left; font-size: 0.85rem; }}
  table.data th {{ background: #f0f0f0; }}
  .placeholder {{ color: #999; font-style: italic; }}
</style></head>
<body>
  <h1>ABA ABC Data Collection Sheet</h1>
  <p class="sub">Record observable events using the client's approved behavior-plan definitions.</p>

  <table class="header">
    <tr><td class="label">Client Name</td><td>{client_name}</td><td class="label">Client ID</td><td>{client_id}</td><td class="label">Session Date</td><td></td></tr>
    <tr><td class="label">Date of Birth</td><td>{dob}</td><td class="label">Staff Name</td><td></td><td class="label">Location</td><td></td></tr>
    <tr><td class="label">Target Behavior</td><td colspan="5" class="placeholder">(from the approved Behavior Plan — Stage 1 not built yet)</td></tr>
    <tr><td class="label">Operational Definition</td><td colspan="5" class="placeholder">(from the approved Behavior Plan — Stage 1 not built yet)</td></tr>
  </table>

  <table class="data">
    <tr><th>#</th><th>Date / Time</th><th>Antecedent (A)</th><th>Behavior (B)</th><th>Consequence (C)</th><th>Duration</th><th>Intensity (1-5)</th><th>Notes / Context</th></tr>
    {rows}
  </table>
</body></html>"""


def save_document(case_id, title, filename, html, document_type):
    # Mirrors usp_Document_Save's own fresh-insert branch directly (VersionId/CreatedDate/
    # IsActive) rather than EXEC-ing the SP — pyodbc chokes on DECLARE/EXEC/@x OUTPUT/SELECT
    # batched in one execute() (hit this exact issue earlier building the PriorAuth bundle).
    html_bytes = html.encode("utf-8")
    with get_conn() as conn:
        row = conn.execute("""
            INSERT INTO [cases].[Document]
                (VersionId, CaseId, CaseNumber, SessionId, WorkbookQId,
                 DocumentType, Title, FileName, ContentType, FileData,
                 CreatedDate, CreatedBy, IsActive)
            OUTPUT INSERTED.DocumentId
            VALUES
                (NEWID(), ?, NULL, NULL, NULL,
                 ?, ?, ?, 'text/html', CAST(? AS VARBINARY(MAX)),
                 SYSUTCDATETIME(), 'behavior_plan_job', 1)
        """, case_id, document_type, title, filename, html_bytes).fetchval()
        conn.commit()
    return row


case_id = st.number_input("Case ID", min_value=1, step=1, value=None, placeholder="e.g. 13")

if case_id:
    context = get_case_context(int(case_id))
    if context is None:
        st.error(f"CaseId {int(case_id)} does not exist.")
    else:
        label = context["patient_name"] or context["title"]
        st.success(f"Case {context['case_number']} — {label}")
else:
    st.info("Enter a Case ID to continue.")

st.divider()

STAGES = [
    {
        "title": "1. Behavior Plan Builder",
        "description": "A BCBA creates and approves the plan — measurable goals and a "
                       "data-collection method per goal. AI drafts from BCBA-provided "
                       "information; the BCBA modifies/guides it before it's final.",
    },
    {
        "title": "2. Sheet Generator",
        "description": "Generates a client-specific fillable data-collection sheet from the "
                       "approved plan — printable for now, filled out by hand.",
    },
    {
        "title": "3. Data & Graphing",
        "description": "Filled sheets get scanned in and extracted the same way Session notes "
                       "are today — structured observations, stored per goal/date/phase. "
                       "Trends plotted deterministically, never by AI.",
    },
    {
        "title": "4. Progress Report",
        "description": "Plan summary, graphs, baseline-vs-current, goal status, and a BCBA "
                       "review/sign-off section — one polished document.",
    },
]

for i, stage in enumerate(STAGES):
    with st.container(border=True):
        st.markdown(f"**{stage['title']}**")
        st.caption(stage["description"])
        if i == 1:  # Sheet Generator — the one real Layer 2 piece for now
            if st.button("Generate sheet", key="stage_1", disabled=not case_id):
                html = render_abc_sheet_html(context, int(case_id))
                doc_id = save_document(
                    int(case_id), "ABC Data Collection Sheet",
                    f"abc-sheet-case{int(case_id)}-{date.today().isoformat()}.html",
                    html, "BehaviorDataSheet",
                )
                st.success(f"Saved — docId {doc_id}")
                st.markdown(f"[Open sheet](http://localhost:5173/api/getDocument?docId={doc_id})")
        else:
            st.button("Run (not built yet)", key=f"stage_{i}", disabled=not case_id)
