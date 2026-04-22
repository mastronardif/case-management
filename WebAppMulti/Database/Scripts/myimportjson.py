import json
import pyodbc

# -----------------------------
# CONFIGURATION
# -----------------------------
SERVER = r"LAPTOP-JIH94VS9\SQLEXPRESS"
DATABASE = "AdventureWorksDW"
JSON_FILE = r"C:\path\to\your.json"
TABLE_NAME = "dbo.JsonImportTable"    # permanent SQL table

# -----------------------------
# CONNECT TO SQL SERVER
# -----------------------------
conn_str = (
    "DRIVER={ODBC Driver 17 for SQL Server};"
    f"SERVER={SERVER};"
    f"DATABASE={DATABASE};"
    "Trusted_Connection=yes;"
)

conn = pyodbc.connect(conn_str, autocommit=True)
cursor = conn.cursor()

# -----------------------------
# READ JSON FILE
# -----------------------------
with open(JSON_FILE, "r", encoding="utf-8") as f:
    data = json.load(f)

if not isinstance(data, list) or len(data) == 0:
    raise ValueError("JSON must contain a list of objects with at least one item.")

# -----------------------------
# EXTRACT COLUMNS FROM JSON
# -----------------------------
columns = list(data[0].keys())

# Create column list with NVARCHAR(MAX) for all
column_definitions = ", ".join([f"[{col}] NVARCHAR(MAX)" for col in columns])

# -----------------------------
# CREATE TABLE IF NOT EXISTS
# -----------------------------
create_table_sql = f"""
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = '{TABLE_NAME.split('.')[-1]}' AND type = 'U')
BEGIN
    CREATE TABLE {TABLE_NAME} (
        {column_definitions}
    );
END
"""
cursor.execute(create_table_sql)
print(f"Table ensured: {TABLE_NAME}")

# -----------------------------
# INSERT ROWS
# -----------------------------
placeholders = ", ".join(["?"] * len(columns))
insert_sql = f"INSERT INTO {TABLE_NAME} ({','.join(f'[{c}]' for c in columns)}) VALUES ({placeholders})"

row_count = 0
for row in data:
    values = [str(row.get(col, "")) for col in columns]
    cursor.execute(insert_sql, values)
    row_count += 1

print(f"Inserted {row_count} rows into {TABLE_NAME}.")

cursor.close()
conn.close()

print("✔ Done.")
