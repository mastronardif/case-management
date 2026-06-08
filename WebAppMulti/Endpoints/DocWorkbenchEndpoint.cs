public static class DocWorkbenchEndpoint
{
    public static void MapDocWorkbenchEndpoint(this WebApplication app)
    {
        app.MapGet("/api/docWorkbench", () => Results.Content(Html, "text/html"));
    }

    private const string Html = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8"/>
          <title>Doc Workbench</title>
          <style>
            *, *::before, *::after { box-sizing: border-box; }
            body { font-family: Consolas, monospace; background: #f0f2f5; margin: 0; padding: 2rem; color: #222; }
            h1 { font-size: 1.1rem; margin: 0 0 0.25rem; }
            .meta { font-size: 0.8rem; color: #888; margin-bottom: 1.5rem; }
            .card { background: #fff; border-radius: 8px; padding: 1.25rem 1.5rem; margin-bottom: 1.25rem; box-shadow: 0 1px 4px rgba(0,0,0,0.08); }
            .card h2 { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.06em; color: #64748b; margin: 0 0 0.75rem; }
            .row { display: flex; align-items: center; gap: 0.6rem; margin-bottom: 0.6rem; flex-wrap: wrap; }
            label { font-size: 0.8rem; color: #555; min-width: 7rem; }
            input[type="text"], input[type="number"] { border: 1px solid #cbd5e1; border-radius: 4px; padding: 0.3rem 0.5rem; font-size: 0.82rem; font-family: inherit; width: 8rem; }
            input[type="file"] { font-size: 0.82rem; font-family: inherit; }
            input:focus { outline: none; border-color: #3b82f6; }
            .btn { background: #1d4ed8; color: #fff; border: none; border-radius: 4px; padding: 0.3rem 0.9rem; font-size: 0.8rem; cursor: pointer; font-family: inherit; white-space: nowrap; }
            .btn:hover { background: #1e40af; }
            .btn:disabled { background: #93c5fd; cursor: default; }
            .btn-open { background: #475569; }
            .btn-open:hover { background: #334155; }
            .result { display: none; background: #f0fdf4; border: 1px solid #86efac; border-radius: 6px; padding: 0.85rem 1rem; font-size: 0.82rem; }
            .result.error { background: #fef2f2; border-color: #fca5a5; }
            .result .docid { font-size: 1.1rem; font-weight: bold; color: #15803d; }
            .result .snippet { background: #1e293b; color: #86efac; padding: 0.35rem 0.6rem; border-radius: 4px; font-size: 0.78rem; margin-top: 0.5rem; display: inline-block; cursor: pointer; }
            .result .copied { color: #64748b; font-size: 0.75rem; margin-left: 0.5rem; display: none; }
            hr { border: none; border-top: 1px solid #e2e8f0; margin: 1rem 0; }
          </style>
        </head>
        <body>
          <h1>Doc Workbench</h1>
          <div class="meta">View a source doc → create JSON → upload → get docId for pipeline</div>

          <div class="card">
            <h2>1 — View Source Document</h2>
            <div class="row">
              <label>Source Doc ID</label>
              <input id="viewDocId" type="number" placeholder="docId" />
              <button class="btn btn-open" onclick="openDoc()">Open ↗</button>
            </div>
          </div>

          <div class="card">
            <h2>2 — Upload JSON</h2>
            <div class="row">
              <label>Case ID</label>
              <input id="caseId" type="number" placeholder="caseId" />
            </div>
            <div class="row">
              <label>Session ID</label>
              <input id="sessionId" type="number" placeholder="optional" />
            </div>
            <div class="row">
              <label>Doc Type</label>
              <input id="docType" type="text" placeholder="e.g. assessment" style="width:12rem" />
            </div>
            <div class="row">
              <label>JSON File</label>
              <input id="fileInput" type="file" accept=".json,application/json" />
            </div>
            <hr/>
            <div class="row">
              <button id="uploadBtn" class="btn" onclick="uploadDoc()">Upload</button>
              <span id="uploadStatus" style="font-size:0.8rem;color:#64748b"></span>
            </div>
          </div>

          <div id="result" class="result">
            <div>Saved → docId: <span id="resultDocId" class="docid"></span></div>
            <div style="margin-top:0.4rem;font-size:0.8rem;color:#555">Pipeline input token:</div>
            <div>
              <span id="snippet" class="snippet" onclick="copySnippet()" title="Click to copy"></span>
              <span id="copied" class="copied">copied!</span>
            </div>
          </div>

          <script>
          function openDoc() {
            const id = document.getElementById('viewDocId').value.trim();
            if (!id) { document.getElementById('viewDocId').focus(); return; }
            window.open('/api/getDocument?docId=' + encodeURIComponent(id), '_blank');
          }

          async function uploadDoc() {
            const caseId    = document.getElementById('caseId').value.trim();
            const sessionId = document.getElementById('sessionId').value.trim();
            const docType   = document.getElementById('docType').value.trim();
            const fileInput = document.getElementById('fileInput');
            const file      = fileInput.files[0];

            if (!caseId)  { document.getElementById('caseId').focus(); return; }
            if (!file)    { fileInput.click(); return; }

            const btn    = document.getElementById('uploadBtn');
            const status = document.getElementById('uploadStatus');
            const result = document.getElementById('result');
            btn.disabled = true;
            status.textContent = 'Uploading…';
            result.style.display = 'none';

            try {
              const fd = new FormData();
              fd.append('file',    file);
              fd.append('caseId',  caseId);
              if (sessionId) fd.append('sessionId', sessionId);
              if (docType)   fd.append('documentType', docType);

              const resp = await fetch('/api/uploadDocument', { method: 'POST', body: fd });
              if (!resp.ok) throw new Error(await resp.text());
              const data = await resp.json();

              document.getElementById('resultDocId').textContent = data.docId;
              document.getElementById('snippet').textContent = `"${data.docId} source.json"`;
              result.className = 'result';
              result.style.display = 'block';
              status.textContent = '';
            } catch (err) {
              result.className = 'result error';
              result.innerHTML = 'Error: ' + err.message;
              result.style.display = 'block';
              status.textContent = '';
            } finally {
              btn.disabled = false;
            }
          }

          function copySnippet() {
            const text = document.getElementById('snippet').textContent;
            navigator.clipboard.writeText(text).then(() => {
              const c = document.getElementById('copied');
              c.style.display = 'inline';
              setTimeout(() => c.style.display = 'none', 1500);
            });
          }

          document.getElementById('viewDocId').addEventListener('keydown', e => { if (e.key === 'Enter') openDoc(); });
          </script>
        </body>
        </html>
        """;
}
