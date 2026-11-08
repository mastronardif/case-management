"""Build the Case Management MOCK images on this PC and ship them to the OpenHost HP.

Only allow-listed source goes into each build, the UI's public files are scrubbed of
real names, and everything staged is checked against the real values in the local dev
database before anything is built. The HP gets finished images, compose.yaml and db/.

  python -I build_and_ship.py            # stage, check, build
  python -I build_and_ship.py --ship     # ...and load on the HP, copy compose + db/
"""
import argparse
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile

import pyodbc

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parent.parent
API_SRC = REPO / "WebAppMulti"
UI_SRC = REPO / "CaseManagementUI"
DEV_DB = r"DRIVER={ODBC Driver 18 for SQL Server};SERVER=LAPTOP-JIH94VS9\SQLEXPRESS;DATABASE=CaseManagement;Trusted_Connection=yes;Encrypt=no"
HP = "frank@192.168.1.45"
HP_DIR = "casemanagement"  # under the HP user's home, next to ~/joeschedule

# What the API build needs. Everything else in WebAppMulti stays home
# (42A/, Database/Scripts/, bin/, obj/, notes, local data files).
API_DIRS = ["Configuration", "Controllers", "Endpoints", "Extensions", "Helpers", "Middleware",
            "Models", "Modules", "Reports", "Services", "Workbooks", "wwwroot", "Properties",
            "Database/Auth", "Database/Configurations", "Database/Dtos", "Database/Generators",
            "Database/Models", "Database/Repository", "Database/Schema"]
API_FILES = ["Program.cs", "WeatherForecast.cs", "WebAppMulti.csproj", "appsettings.json", "Database/MyDbContext.cs"]
UI_SKIP_DIRS = {"node_modules", "dist", ".git", ".vite"}
UI_DROP_FILES = ["public/files/buzcard.jpg"]  # business card
TEXT_EXT = {".cs", ".json", ".js", ".jsx", ".ts", ".tsx", ".html", ".txt", ".md", ".css", ".svg", ".csv", ".xml", ".config"}
# Whole-word matches already reviewed by hand and harmless (a common word that equals a short name).
ALLOWED_HITS = {("ui/public/files/MonthOneABA.txt", "Patient.LastName"),
                ("ui/src/components/Header.jsx", "Provider.LastName")}  # the owner's own name in the header


def real_values():
    """Sensitive values from the dev DB. Kept in memory, never printed."""
    q = {"Patient": "SELECT FirstName, LastName, MemberId, MedicaidId, Email, Phone, Address1 FROM cases.Patient",
         "Coverage": "SELECT SubscriberFirstName, SubscriberLastName, MemberId, GroupNumber, SubscriberEmail, SubscriberPhone, SubscriberAddress1 FROM cases.InsuranceCoverage",
         "Provider": "SELECT FirstName, LastName, Email, Phone, NPI, TaxId, Address1 FROM cases.Provider",
         "Staff": "SELECT FirstName, LastName, Email, Phone, StaffCode, NULL, NULL FROM cases.ProviderStaff",
         "Case": "SELECT CaseNumber, Title, NULL, NULL, NULL, NULL, NULL FROM cases.[Case] WHERE Title <> 'Unknown Case'",
         "PriorAuth": "SELECT PatientName, MemberId, AuthorizationNumber, ProviderNPI, ProviderName, NULL, NULL FROM cases.PriorAuthForm",
         "Auth": "SELECT AuthorizationNumber, NULL, NULL, NULL, NULL, NULL, NULL FROM cases.[Authorization]",
         "Practice": "SELECT BillingProviderNPI, TaxId, SenderId, SubmitterIdentifier, Phone, Email, Address1 FROM cases.PracticeConfiguration",
         "Session": "SELECT ClientName, NULL, NULL, NULL, NULL, NULL, NULL FROM cases.Session"}
    names, values = set(), {}
    with pyodbc.connect(DEV_DB) as cn:
        for table, sql in q.items():
            cur = cn.execute(sql)
            cols = [d[0] for d in cur.description]
            for row in cur.fetchall():
                for col, v in zip(cols, row):
                    if v is None or len(str(v).strip()) < 4:
                        continue
                    values.setdefault(str(v).strip().lower(), set()).add(f"{table}.{col}")
                    if col.endswith(("FirstName", "LastName")):
                        names.add(str(v).strip())
    return names, values


def stage_api(dest):
    for d in API_DIRS:
        if (API_SRC / d).exists():
            shutil.copytree(API_SRC / d, dest / d, ignore=shutil.ignore_patterns("bin", "obj"))
    for f in API_FILES:
        (dest / f).parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(API_SRC / f, dest / f)
    shutil.copy2(HERE / "api.Dockerfile", dest / "Dockerfile")


def stage_ui(dest, names):
    shutil.copytree(UI_SRC, dest, ignore=lambda d, items: [i for i in items if i in UI_SKIP_DIRS])
    for f in UI_DROP_FILES:
        (dest / f).unlink(missing_ok=True)
    # Scrub real names from the static files only (never from code).
    pats = [re.compile(r"(?<![A-Za-z0-9])" + re.escape(n) + r"(?![A-Za-z0-9])", re.I) for n in names]
    for p in (dest / "public").rglob("*"):
        if p.is_file() and p.suffix.lower() in TEXT_EXT:
            text = p.read_text(encoding="utf-8", errors="ignore")
            new = text
            for pat in pats:
                new = pat.sub("Mock", new)
            if new != text:
                p.write_text(new, encoding="utf-8")
                print(f"  scrubbed names in ui/{p.relative_to(dest).as_posix()}")
    shutil.copy2(HERE / "web.Dockerfile", dest / "Dockerfile")
    shutil.copy2(HERE / "web-nginx.conf", dest / "web-nginx.conf")


def leak_check(root, values):
    hits = []
    for p in root.rglob("*"):
        if not p.is_file() or p.suffix.lower() not in TEXT_EXT:
            continue
        low = p.read_text(encoding="utf-8", errors="ignore").lower()
        rel = p.relative_to(root).as_posix()
        for v, where in values.items():
            if re.search(r"(?<![a-z0-9])" + re.escape(v) + r"(?![a-z0-9])", low):
                for w in where:
                    if (rel, w) not in ALLOWED_HITS:
                        hits.append((rel, w))
    return sorted(set(hits))


def run(cmd):
    print(">", cmd, flush=True)
    subprocess.run(cmd, shell=True, check=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--ship", action="store_true", help="load images on the HP and copy compose.yaml + db/")
    args = ap.parse_args()

    names, values = real_values()
    with tempfile.TemporaryDirectory(prefix="cm-stage-") as tmp:
        stage = pathlib.Path(tmp)
        print("Staging API and UI...")
        stage_api(stage / "api")
        stage_ui(stage / "ui", names)

        print("Checking staged files and db/ for real values...")
        hits = leak_check(stage, values)
        db_dir = HERE / "db"
        hits += [("db/" + h[0], h[1]) for h in leak_check(db_dir, values)]
        if hits:
            print("STOP: real values found (file, source column). Nothing was built:")
            for h in hits:
                print("  ", *h)
            sys.exit(1)
        print("  clean")

        run(f'docker build --platform linux/amd64 -t casemanagement-api:mock "{stage / "api"}"')
        run(f'docker build --platform linux/amd64 -t casemanagement-web:mock "{stage / "ui"}"')

    if args.ship:
        run(f'ssh {HP} "mkdir -p ~/{HP_DIR}/db"')
        run(f"docker save casemanagement-api:mock casemanagement-web:mock | ssh {HP} docker load")
        run(f'scp "{HERE / "compose.yaml"}" {HP}:{HP_DIR}/compose.yaml')
        # One file per scp with an explicit target name: Windows scp with several sources
        # names the remote files after the full Windows path.
        for f in ("init.sh", "schema.sql", "seed.sql"):
            run(f'scp "{db_dir / f}" {HP}:{HP_DIR}/db/{f}')
        print("Shipped. On the HP: cd ~/casemanagement && docker compose up -d")


if __name__ == "__main__":
    main()
