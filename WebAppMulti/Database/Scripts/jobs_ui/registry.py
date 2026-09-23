"""
The list of back-office jobs shown on the hub's root page. To add a job: drop a page script in
jobs_ui/pages/ and add an entry here (entries without a "page" show as a card only).

status: "ready" | "in progress" | "planned"
"""

STATUS_COLORS = {"ready": "green", "in progress": "orange", "planned": "gray"}

JOBS = [
    {
        "id": "source_doc_to_json",
        "title": "Source Doc → Session JSON",
        "description": "Extract a session document into session JSON with Claude, validate it, "
                       "and get the review link.",
        "page": "jobs_ui/pages/source_doc_to_json.py",
        "status": "ready",
    },
    {
        "id": "import_session_doc",
        "title": "Import Session Doc",
        "description": "Load session documents into the doc store from an import.csv "
                       "(run from WebAppMulti/Database/Scripts/ImportDocs). To be integrated later.",
        "command": 'dotnet run -- import.csv "Server=LAPTOP-JIH94VS9\\SQLEXPRESS;Database=CaseManagement;'
                   'Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;"',
        "status": "planned",
    },
    {
        "id": "availity",
        "title": "Availity — Send Claims & Check Status",
        "description": "Send ReadyToSubmit claims to Availity over SFTP and check their status. "
                       "Being built against a local mock until real credentials exist.",
        "page": "jobs_ui/pages/availity.py",
        "status": "in progress",
    },
    {
        "id": "tbd",
        "title": "TBD",
        "description": "Reserved for the next job.",
        "status": "planned",
    },
]
