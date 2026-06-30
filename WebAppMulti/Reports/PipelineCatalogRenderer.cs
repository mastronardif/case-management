using System.Net;
using System.Text;
using System.Text.Json;

namespace WebAppMulti.Reports;

public record PipelineInfo(int PipelineId, string Name, string? Description, int? DocId, string? Template, string? ParamsSchemaJson);

public record OperatorParamInfo(string Name, string Type, bool Required, string Description);
public record OperatorCatalogItem(string Operator, string Description, string[] InputLabels, string[] OutputLabels, OperatorParamInfo[] Params);

public static class PipelineCatalogRenderer
{
    private static JsonElement TryParseSchema(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return JsonSerializer.Deserialize<JsonElement>("[]");
        try { return JsonSerializer.Deserialize<JsonElement>(json); }
        catch { return JsonSerializer.Deserialize<JsonElement>("[]"); }
    }

    // Extracts the raw substring for a top-level "propertyName" value from a JSON-shaped document
    // without fully parsing it — needed because pipeline templates can contain unquoted {placeholder}
    // tokens that make the document technically invalid JSON until the catalog form fills them in.
    // Brace/bracket balance still holds (every {placeholder} is self-closing), so a string-aware
    // depth counter finds the correct boundaries even though JsonDocument.Parse would throw.
    public static string? ExtractJsonValue(string json, string propertyName)
    {
        var keyPattern = $"\"{propertyName}\"";
        var keyIdx = json.IndexOf(keyPattern, StringComparison.Ordinal);
        if (keyIdx < 0) return null;

        var idx = keyIdx + keyPattern.Length;
        while (idx < json.Length && (char.IsWhiteSpace(json[idx]) || json[idx] == ':')) idx++;
        if (idx >= json.Length) return null;

        var start     = idx;
        var openChar  = json[idx];
        if (openChar != '{' && openChar != '[') return null;
        var closeChar = openChar == '{' ? '}' : ']';

        var depth    = 0;
        var inString = false;
        var escaped  = false;

        for (; idx < json.Length; idx++)
        {
            var c = json[idx];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            if (c == '"') { inString = true; continue; }
            if (c == openChar) depth++;
            else if (c == closeChar)
            {
                depth--;
                if (depth == 0) { idx++; break; }
            }
        }

        return json[start..idx];
    }

    public static string Render(IEnumerable<PipelineInfo> pipelines, string? operatorsJson = null)
    {
        List<OperatorCatalogItem>? operators = null;
        if (!string.IsNullOrWhiteSpace(operatorsJson))
        {
            try { operators = JsonSerializer.Deserialize<List<OperatorCatalogItem>>(operatorsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch { /* operators section omitted if JSON is malformed */ }
        }

        var list = pipelines.ToList();

        var dataJson = JsonSerializer.Serialize(list.Select(p => new
        {
            id          = p.PipelineId,
            name        = p.Name,
            description = p.Description ?? "",
            template    = p.Template ?? "",
            schema      = TryParseSchema(p.ParamsSchemaJson),
            docId       = p.DocId
        }));

        var sb = new StringBuilder();
        sb.AppendLine($$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8"/>
              <title>Pipeline Catalog</title>
              <style>
                *, *::before, *::after { box-sizing: border-box; }
                body { font-family: Consolas, monospace; background: #f0f2f5; margin: 0; color: #222; height: 100vh; display: flex; flex-direction: column; }
                h1 { font-size: 1.1rem; margin: 0; padding: 0.75rem 1.25rem; background: #1e293b; color: #e2e8f0; flex-shrink: 0; }
                .layout { display: flex; flex: 1; min-height: 0; }

                /* Left panel */
                .left { width: 220px; flex-shrink: 0; background: #1e293b; overflow-y: auto; }
                .left-label { font-size: 0.65rem; text-transform: uppercase; letter-spacing: 0.08em; color: #64748b; padding: 0.75rem 1rem 0.35rem; }
                .pipeline-item { padding: 0.55rem 1rem; font-size: 0.82rem; color: #94a3b8; cursor: pointer; border-left: 3px solid transparent; }
                .pipeline-item:hover { background: #273548; color: #e2e8f0; }
                .pipeline-item.active { background: #1d4ed8; color: #fff; border-left-color: #60a5fa; }

                /* Right panel */
                .right { flex: 1; overflow-y: auto; padding: 1.5rem; display: flex; flex-direction: column; gap: 1.25rem; }
                .placeholder { color: #aaa; font-size: 0.85rem; margin-top: 2rem; text-align: center; }
                .detail-name { font-size: 1rem; font-weight: bold; color: #1d4ed8; }
                .detail-desc { font-size: 0.8rem; color: #555; margin-top: 0.2rem; }

                /* Params form */
                .section-label { font-size: 0.65rem; text-transform: uppercase; letter-spacing: 0.07em; color: #999; margin-bottom: 0.5rem; padding-bottom: 0.3rem; border-bottom: 1px solid #e2e8f0; }
                .param-row { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.5rem; }
                .param-label { font-size: 0.78rem; color: #555; width: 200px; flex-shrink: 0; }
                .param-input { border: 1px solid #cbd5e1; border-radius: 4px; padding: 0.3rem 0.5rem; font-size: 0.78rem; font-family: inherit; flex: 1; max-width: 280px; }
                .param-input:focus { outline: none; border-color: #3b82f6; }

                /* JSON preview */
                .json-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.4rem; }
                .btn-row { display: flex; gap: 0.4rem; align-items: center; }
                .copy-btn { background: #1d4ed8; color: #fff; border: none; border-radius: 4px; padding: 0.25rem 0.75rem; font-size: 0.72rem; cursor: pointer; font-family: inherit; }
                .copy-btn:hover { background: #1e40af; }
                .copy-btn.copied { background: #16a34a; }
                .save-wf-btn { background: #16a34a; color: #fff; border: none; border-radius: 4px; padding: 0.25rem 0.75rem; font-size: 0.72rem; cursor: pointer; font-family: inherit; }
                .save-wf-btn:hover { background: #15803d; }
                .save-wf-btn:disabled { background: #86efac; cursor: default; }
                pre.json-code { background: #1e293b; color: #86efac; padding: 1rem; border-radius: 6px; font-size: 0.75rem; line-height: 1.6; overflow-x: auto; white-space: pre; margin: 0; }
                .wf-saved { background: #f0fdf4; border: 1px solid #86efac; border-radius: 6px; padding: 0.65rem 0.85rem; font-size: 0.78rem; display: none; }
                .wf-saved .docid { font-weight: bold; color: #15803d; }
                .wf-saved .run-cmd { background: #1e293b; color: #86efac; padding: 0.2rem 0.5rem; border-radius: 4px; font-size: 0.73rem; cursor: pointer; }
                .wf-error { color: #dc2626; font-size: 0.75rem; }
              </style>
            </head>
            <body>
              <h1>Pipeline Catalog &nbsp;<span style="font-weight:normal;font-size:0.8rem;color:#64748b">{{list.Count}} pipeline{{(list.Count == 1 ? "" : "s")}}</span></h1>
              <div class="layout">
                <div class="left" id="pipeline-list">
                  <div class="left-label">Pipelines</div>
                </div>
                <div class="right" id="right-panel">
                  <div class="placeholder" id="placeholder">&#8592; Select a pipeline</div>
                  <div id="detail" style="display:none">
                    <div class="detail-name" id="detail-name"></div>
                    <div class="detail-desc" id="detail-desc"></div>
                    <div style="margin-top:1rem">
                      <div class="section-label">Parameters</div>
                      <div id="params-form"></div>
                    </div>
                    <div>
                      <div class="json-header">
                        <span class="section-label" style="margin:0;border:none">Workflow JSON</span>
                        <div class="btn-row">
                          <button class="copy-btn" id="copy-btn" onclick="copyJson()">Copy</button>
                          <button class="save-wf-btn" id="save-wf-btn" onclick="saveWorkflow()">Save Workflow</button>
                        </div>
                      </div>
                      <pre class="json-code" id="json-preview"></pre>
                      <div class="wf-saved" id="wf-saved">
                        Saved ✓ &nbsp; docId: <span class="docid" id="wf-docid"></span>
                        &nbsp;&mdash;&nbsp;
                        <span class="run-cmd" id="wf-cmd" title="Click to copy" onclick="copyCmd()"></span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <script type="application/json" id="catalog-data">
            """);

        sb.AppendLine(dataJson);

        sb.AppendLine("""
              </script>
              <script>
              const pipelines = JSON.parse(document.getElementById('catalog-data').textContent);
              let current = null;

              // Render left list
              const listEl = document.getElementById('pipeline-list');
              pipelines.forEach(p => {
                const item = document.createElement('div');
                item.className = 'pipeline-item';
                item.dataset.id = p.id;

                const nameSpan = document.createElement('span');
                nameSpan.textContent = p.name;
                item.appendChild(nameSpan);

                if (p.docId) {
                  const link = document.createElement('a');
                  link.href = '/api/getDocument?docId=' + p.docId;
                  link.target = '_blank';
                  link.textContent = ' (' + p.docId + ' ↗)';
                  link.style.color = '#60a5fa';
                  link.style.fontSize = '0.72rem';
                  link.style.textDecoration = 'none';
                  link.title = 'View pipeline definition doc';
                  link.addEventListener('click', e => e.stopPropagation());
                  item.appendChild(link);
                }

                item.addEventListener('click', () => select(p.id));
                listEl.appendChild(item);
              });

              function select(id) {
                current = pipelines.find(p => p.id === id);
                document.querySelectorAll('.pipeline-item').forEach(el =>
                  el.classList.toggle('active', parseInt(el.dataset.id) === id));

                document.getElementById('placeholder').style.display = 'none';
                document.getElementById('detail').style.display = '';

                document.getElementById('detail-name').textContent = current.name;
                document.getElementById('detail-desc').textContent = current.description;

                renderForm(current.schema || []);
                updatePreview();
              }

              function renderForm(schema) {
                const form = document.getElementById('params-form');
                form.innerHTML = '';
                if (!schema.length) {
                  form.innerHTML = '<span style="color:#aaa;font-size:0.78rem">No parameters required.</span>';
                  return;
                }
                schema.forEach(p => {
                  const row = document.createElement('div');
                  row.className = 'param-row';
                  const lbl = document.createElement('span');
                  lbl.className = 'param-label';
                  lbl.textContent = p.label + (p.required ? ' *' : '');
                  const inp = document.createElement('input');
                  inp.className = 'param-input';
                  inp.type = 'text';
                  inp.placeholder = p.type === 'raw' ? 'e.g. 39, 40' : p.type === 'int' ? '0' : '';
                  inp.dataset.param = p.name;
                  inp.dataset.type  = p.type;
                  inp.addEventListener('input', updatePreview);
                  row.appendChild(lbl);
                  row.appendChild(inp);
                  form.appendChild(row);
                });
              }

              function getValues() {
                const vals = {};
                document.querySelectorAll('.param-input').forEach(inp => {
                  vals[inp.dataset.param] = { value: inp.value.trim(), type: inp.dataset.type };
                });
                return vals;
              }

              function fillTemplate(template, values) {
                let result = template;
                Object.entries(values).forEach(([name, { value, type }]) => {
                  const placeholder       = '{' + name + '}';
                  const quotedPlaceholder = '"' + placeholder + '"';
                  if (type === 'int') {
                    // Replace "quoted" placeholder → bare number (removes the JSON string quotes)
                    result = result.split(quotedPlaceholder).join(value || '0');
                    result = result.split(placeholder).join(value || '0');
                  } else {
                    // raw / string — replace as-is
                    result = result.split(placeholder).join(value);
                  }
                });
                return result;
              }

              function updatePreview() {
                if (!current) return;
                const values = getValues();
                const json   = fillTemplate(current.template, values);
                document.getElementById('json-preview').textContent = json;
              }

              function copyJson() {
                const text = document.getElementById('json-preview').textContent;
                navigator.clipboard.writeText(text).then(() => {
                  const btn = document.getElementById('copy-btn');
                  btn.textContent = 'Copied ✓';
                  btn.classList.add('copied');
                  setTimeout(() => { btn.textContent = 'Copy'; btn.classList.remove('copied'); }, 1500);
                });
              }

              async function saveWorkflow() {
                const json = document.getElementById('json-preview').textContent.trim();
                if (!json) return;
                const btn = document.getElementById('save-wf-btn');
                const saved = document.getElementById('wf-saved');
                btn.disabled = true;
                btn.textContent = 'Saving…';
                saved.style.display = 'none';
                try {
                  const resp = await fetch('/api/saveWorkflow', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ json, name: current?.name ?? 'workflow' })
                  });
                  if (!resp.ok) throw new Error(await resp.text());
                  const data = await resp.json();
                  document.getElementById('wf-docid').textContent = data.docId;
                  const cmd = `dotnet run -- --workflow ${data.docId}`;
                  document.getElementById('wf-cmd').textContent = cmd;
                  saved.style.display = 'block';
                } catch (err) {
                  saved.innerHTML = `<span class="wf-error">Error: ${err.message}</span>`;
                  saved.style.display = 'block';
                } finally {
                  btn.disabled = false;
                  btn.textContent = 'Save Workflow';
                }
              }

              function copyCmd() {
                const text = document.getElementById('wf-cmd').textContent;
                navigator.clipboard.writeText(text).then(() => {
                  const el = document.getElementById('wf-cmd');
                  const orig = el.textContent;
                  el.textContent = 'copied!';
                  setTimeout(() => el.textContent = orig, 1200);
                });
              }
              </script>
            """);

        sb.AppendLine($$"""
              <div style="border-top:1px solid #e2e8f0;background:#f8fafc;flex-shrink:0">
                <div id="ops-header" onclick="toggleOperators()" style="cursor:pointer;user-select:none;padding:0.6rem 1.5rem;font-size:0.65rem;text-transform:uppercase;letter-spacing:0.08em;color:#64748b;display:flex;align-items:center;gap:0.4rem">
                  <span id="ops-chevron">▸</span> Operators &nbsp;<span style="color:#94a3b8">({{operators?.Count ?? 0}})</span>
                </div>
                <div id="ops-content" style="display:none;resize:vertical;overflow:auto;height:260px;max-height:75vh;min-height:80px;padding:0 1.5rem 1.5rem">
            """);

        if (operators is { Count: > 0 })
        {
            sb.AppendLine($$"""
                    <div>
                    <table style="width:100%;border-collapse:collapse;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,0.08)">
                      <thead>
                        <tr>
                          <th style="background:#1e293b;color:#e2e8f0;text-align:left;padding:0.5rem 0.9rem;font-size:0.72rem;text-transform:uppercase;letter-spacing:0.05em">Operator</th>
                          <th style="background:#1e293b;color:#e2e8f0;text-align:left;padding:0.5rem 0.9rem;font-size:0.72rem;text-transform:uppercase;letter-spacing:0.05em">Description</th>
                          <th style="background:#1e293b;color:#e2e8f0;text-align:left;padding:0.5rem 0.9rem;font-size:0.72rem;text-transform:uppercase;letter-spacing:0.05em">Inputs</th>
                          <th style="background:#1e293b;color:#e2e8f0;text-align:left;padding:0.5rem 0.9rem;font-size:0.72rem;text-transform:uppercase;letter-spacing:0.05em">Outputs</th>
                          <th style="background:#1e293b;color:#e2e8f0;text-align:left;padding:0.5rem 0.9rem;font-size:0.72rem;text-transform:uppercase;letter-spacing:0.05em">Params</th>
                        </tr>
                      </thead>
                      <tbody>
                """);

            foreach (var op in operators)
            {
                string Badge(string text) =>
                    $"<span style='display:inline-block;background:#f1f5f9;border:1px solid #e2e8f0;border-radius:4px;padding:0.1rem 0.35rem;margin:0.1rem;font-size:0.73rem'>{WebUtility.HtmlEncode(text)}</span>";

                var inputs  = op.InputLabels.Length  == 0 ? "<span style='color:#aaa'>—</span>" : string.Concat(op.InputLabels.Select(Badge));
                var outputs = op.OutputLabels.Length == 0 ? "<span style='color:#aaa'>—</span>" : string.Concat(op.OutputLabels.Select(Badge));
                var parms   = op.Params.Length       == 0 ? "<span style='color:#aaa'>—</span>"
                            : string.Concat(op.Params.Select(p =>
                                $"<span style='display:inline-block;background:#f1f5f9;border:1px solid #e2e8f0;border-radius:4px;padding:0.1rem 0.35rem;margin:0.1rem;font-size:0.73rem'>" +
                                $"{WebUtility.HtmlEncode(p.Name)} <span style='color:#888'>{WebUtility.HtmlEncode(p.Type)}</span>" +
                                $"{(p.Required ? "<span style='color:#dc2626;font-weight:bold'>*</span>" : "")}</span>"));

                sb.AppendLine($"""
                            <tr>
                              <td style="padding:0.55rem 0.9rem;font-size:0.8rem;border-bottom:1px solid #f1f5f9;font-weight:bold;color:#1d4ed8">{WebUtility.HtmlEncode(op.Operator)}</td>
                              <td style="padding:0.55rem 0.9rem;font-size:0.8rem;border-bottom:1px solid #f1f5f9;color:#555">{WebUtility.HtmlEncode(op.Description)}</td>
                              <td style="padding:0.55rem 0.9rem;font-size:0.8rem;border-bottom:1px solid #f1f5f9">{inputs}</td>
                              <td style="padding:0.55rem 0.9rem;font-size:0.8rem;border-bottom:1px solid #f1f5f9">{outputs}</td>
                              <td style="padding:0.55rem 0.9rem;font-size:0.8rem;border-bottom:1px solid #f1f5f9">{parms}</td>
                            </tr>
                    """);
            }

            sb.AppendLine("""
                      </tbody>
                    </table>
                    </div>
                """);
        }
        else
        {
            sb.AppendLine("""
                    <div style="background:#fff;border-radius:8px;padding:1rem 1.25rem;box-shadow:0 1px 4px rgba(0,0,0,0.08);font-size:0.82rem;color:#64748b">
                      Operators not loaded. Run the following command then refresh:
                      <div style="margin-top:0.5rem;background:#1e293b;color:#86efac;padding:0.4rem 0.75rem;border-radius:4px;font-size:0.78rem;display:inline-block">
                        dotnet run -- --list-operators --html
                      </div>
                    </div>
                """);
        }

        sb.AppendLine("""
                </div>
              </div>
              <script>
              function toggleOperators() {
                const content = document.getElementById('ops-content');
                const chevron = document.getElementById('ops-chevron');
                const show = content.style.display === 'none';
                content.style.display = show ? 'block' : 'none';
                chevron.textContent = show ? '▾' : '▸';
              }
              </script>
            </body>
            </html>
            """);

        return sb.ToString();
    }
}
