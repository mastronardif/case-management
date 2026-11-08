import pyodbc, re, json, sys
cn = pyodbc.connect(r"DRIVER={ODBC Driver 18 for SQL Server};SERVER=LAPTOP-JIH94VS9\SQLEXPRESS;DATABASE=CaseManagement;Trusted_Connection=yes;Encrypt=no")
cur = cn.cursor()
seeds = set()
for sql in ["SELECT DocumentId FROM cases.Pipeline","SELECT ProjectionDocumentId FROM cases.ProjectorRule","SELECT RuleDocumentId FROM cases.ProjectorRule","SELECT JsonDocId FROM cases.TableFieldMap"]:
    seeds |= {r[0] for r in cur.execute(sql) if r[0]}
for (v,) in cur.execute("SELECT Value FROM cases.MyConstants"):
    seeds |= {int(x) for x in re.findall(r"\d+", v or "")}
CONFIG_TYPES = ('operators-catalog','operators-catalog-html','blank-837P','Provider837P.projection','Session837P.projection','projection','Pipeline','jobs-manifest')
seeds |= {r[0] for r in cur.execute("SELECT DocumentId FROM cases.Document WHERE DocumentType IN (%s)" % ",".join("?"*len(CONFIG_TYPES)), CONFIG_TYPES)}
# repo schema.json references
try:
    sj = open(r"C:\Users\mastronardif\source\repos\CaseMangement\WebAppMulti\Database\Schema\schema.json", encoding="utf-8").read()
    for m in re.finditer(r'"(\w*(?:doc|document|workflow|projection|definition|pipeline)\w*id)"\s*:\s*"?(\d+)', sj, re.I): seeds.add(int(m.group(2)))
except Exception as e: print("schema.json:", e)
REF = re.compile(r'"(\w*(?:doc|document|workflow|projection|definition|rule|pipeline)\w*id)"\s*:\s*"?(\d+)', re.I)
docs, todo, skipped = {}, list(seeds), {}
while todo:
    i = todo.pop()
    if i in docs or i in skipped: continue
    r = cur.execute("SELECT DocumentId, CaseId, DocumentType, Title, ContentType, FileData FROM cases.Document WHERE DocumentId=?", i).fetchone()
    if not r: skipped[i] = "missing"; continue
    if r.CaseId and r.CaseId > 0: skipped[i] = f"case-attached {r.DocumentType} case={r.CaseId}"; continue
    docs[i] = r
    txt = (r.FileData or b"").decode("utf-8", "ignore")
    for m in REF.finditer(txt): todo.append(int(m.group(2)))
print("config docs:", len(docs))
from collections import Counter
print(Counter(d.DocumentType for d in docs.values()))
print("skipped:", skipped)
json.dump(sorted(docs), open(sys.argv[1], "w"))
