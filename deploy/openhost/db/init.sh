#!/bin/bash
# Runs in the db-init container. Creates the mock CaseManagement database on first run.
set -euo pipefail

export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"
SQL=(/opt/mssql-tools18/bin/sqlcmd -S db -U sa -C -b)

exists=$("${SQL[@]}" -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = 'CaseManagement'")

if [ "$exists" = "1" ]; then
  echo "CaseManagement already exists; leaving its data alone."
else
  echo "Creating CaseManagement from mock schema and seed..."
  "${SQL[@]}" -Q "CREATE DATABASE CaseManagement"
  if ! "${SQL[@]}" -d CaseManagement -i /init/schema.sql -o /dev/null \
     || ! "${SQL[@]}" -d CaseManagement -i /init/seed.sql -o /dev/null; then
    echo "Load failed; dropping the half-built database so the next run starts clean." >&2
    "${SQL[@]}" -Q "ALTER DATABASE CaseManagement SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE CaseManagement;" || true
    exit 1
  fi
  echo "Database created."
fi

# App login, kept in sync with APP_DB_PASSWORD in .env.
"${SQL[@]}" -v AppPw="$APP_DB_PASSWORD" -Q "
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'cmapp')
  CREATE LOGIN cmapp WITH PASSWORD = '\$(AppPw)', CHECK_POLICY = OFF, DEFAULT_DATABASE = CaseManagement;
ELSE
  ALTER LOGIN cmapp WITH PASSWORD = '\$(AppPw)';"
"${SQL[@]}" -d CaseManagement -Q "
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'cmapp')
  CREATE USER cmapp FOR LOGIN cmapp;
ALTER ROLE db_owner ADD MEMBER cmapp;"
echo "Login cmapp ready."
