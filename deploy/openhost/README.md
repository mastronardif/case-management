# Case Management on OpenHost (mock data)

First pass at running Case Management on the OpenHost HP (`frank@192.168.1.45`) with a
**mock database**. No real client data goes to the server.

| What | Where |
|---|---|
| App | `https://192.168.1.45:8443/` |
| Swagger | `https://192.168.1.45:8443/swagger/` |
| SSMS | Server `192.168.1.45,1433`, SQL Server Authentication, user `sa`, tick **Trust server certificate**. The password is `MSSQL_SA_PASSWORD` in `~/casemanagement/.env` on the HP. |
| On the HP | `~/casemanagement/` (compose.yaml, db/, .env) |

## Pieces

- `compose.yaml` runs four containers: `db` is SQL Server 2022 Developer (free, not for
  production), `db-init` builds the database once, `api` is WebAppMulti, and `web` serves the
  React build and passes `/api`, `/s/` and `/swagger` to the API. OpenHost's Nginx handles
  HTTPS on 8443 and forwards to `web` over the `casemanagement-web` network.
- `db/schema.sql` is the dev database's structure only, with a real NPI, case number and
  payer email replaced. `db/seed.sql` is the app's setup (pipelines, projector rules, the
  837P definition, payers, fee schedule, and the 55 setup documents they point to), with
  business IDs replaced, plus three invented cases with patients, coverage, authorizations
  and 32 sessions.
- `build_and_ship.py` builds both images **on this PC** from an allow-listed copy of the
  source, so `42A/`, `Database/Scripts/` and other local files never reach the HP. It
  scrubs real names from the UI's static files, checks everything staged against the real
  values in the dev database, and stops if it finds any.
- `tools/` holds the generators for `db/`: `SchemaExport` (structure), `find_config_docs.py`
  (which documents are setup) and `build_seed.py` (the seed).

## Update the app

```
python -I build_and_ship.py --ship
ssh frank@192.168.1.45 "cd ~/casemanagement && docker compose up -d"
```

## Start the database over

`db-init` leaves an existing database alone. To reload it from `db/`:

```
ssh frank@192.168.1.45 "cd ~/casemanagement && docker compose down -v && docker compose up -d"
```

## Not done yet (before real use)

- Port 1433 is open on the home network. Close it and use an SSH tunnel for SSMS.
- Login is still `DummyAuthStore` with a fixed password. The JWT key is random and kept in `.env`.
- Background jobs (`CaseManagement.Jobs`) and the Python jobs aren't deployed.
- Seeded sessions have no session JSON documents, so pages built from those documents will be empty.
- The plan is to move to Postgres later. SQL Server here is a stopgap.
