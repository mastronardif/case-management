import pyodbc

# -------------------------
# Configuration
# -------------------------
server = r"LAPTOP-JIH94VS9\SQLEXPRESS"
database = "AdventureWorksDW"
username = 'YOUR_USERNAME'        # SQL authentication
password = 'YOUR_PASSWORD'        # SQL authentication

json_file_path = r'C:\Users\mastronardif\Downloads\session.template.json'
html_file_path = r'C:\Users\mastronardif\Downloads\session.template.html'
template_name = 'Session Template 1'
template_description = 'Template imported from files'

# -------------------------
# Read files
# -------------------------
with open(json_file_path, 'r', encoding='utf-8') as f:
    json_schema = f.read()

with open(html_file_path, 'r', encoding='utf-8') as f:
    html_template = f.read()

# -------------------------
# Connect to SQL Server
# -------------------------
# /******
# conn_str = (
#     f'DRIVER={{ODBC Driver 18 for SQL Server}};'
#     f'SERVER={server};'
#     f'DATABASE={database};'
#     f'UID={username};'
#     f'PWD={password}'
# )
# ******/
conn_str = f"DRIVER={{ODBC Driver 17 for SQL Server}};SERVER={server};DATABASE={database};Trusted_Connection=yes;"

conn = pyodbc.connect(conn_str)
cursor = conn.cursor()

# -------------------------
# Call stored procedure
# -------------------------
# Create an output parameter
new_template_id = cursor.execute("""
    DECLARE @NewTemplateId INT;
    EXEC sp_InsertFormTemplate 
        @Name = ?, 
        @Description = ?, 
        @JsonSchema = ?, 
        @HtmlTemplate = ?, 
        @NewTemplateId = @NewTemplateId OUTPUT;
    SELECT @NewTemplateId AS TemplateId;
""", template_name, template_description, json_schema, html_template).fetchone()[0]

print(f"Inserted FormTemplate ID: {new_template_id}")

# Commit and close
conn.commit()
cursor.close()
conn.close()
