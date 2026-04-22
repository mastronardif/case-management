import os
import pyodbc

# === CONFIGURATION ===
server = r"LAPTOP-JIH94VS9\SQLEXPRESS"
database = "AdventureWorksDW"
export_dir = r"C:\Users\mastronardif\source\repos\CaseMangement\WebAppMulti\Database\Scripts\SPs"

# Create export directory if missing
os.makedirs(export_dir, exist_ok=True)

# === CONNECT TO SQL SERVER ===
conn_str = f"DRIVER={{ODBC Driver 17 for SQL Server}};SERVER={server};DATABASE={database};Trusted_Connection=yes;"
conn = pyodbc.connect(conn_str)
cursor = conn.cursor()

# === FETCH ONLY USER STORED PROCEDURES (exclude system diagram SPs) ===
cursor.execute("""
SELECT s.name AS SchemaName, p.name AS ProcName, sm.definition
FROM sys.procedures p
JOIN sys.schemas s ON p.schema_id = s.schema_id
JOIN sys.sql_modules sm ON p.object_id = sm.object_id
WHERE p.is_ms_shipped = 0
  AND p.name NOT LIKE 'sp_%diagram%'
ORDER BY s.name, p.name;
""")

# === EXPORT EACH PROCEDURE ===
count = 0
for schema_name, proc_name, definition in cursor.fetchall():
    filename = f"{schema_name}.{proc_name}.sql"
    filepath = os.path.join(export_dir, filename)

    content = (
        f"IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[{schema_name}].[{proc_name}]') AND type = N'P')\n"
        f"    DROP PROCEDURE [{schema_name}].[{proc_name}]\nGO\n\n"
        f"{definition.strip()}\nGO\n"
    )

    with open(filepath, "w", encoding="utf-8") as f:
        f.write(content)

    print(f"Exported: {filepath}")
    count += 1

cursor.close()
conn.close() 

print(f"\n✅ Export complete! {count} stored procedures written to {export_dir}")
