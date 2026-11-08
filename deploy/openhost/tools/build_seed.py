"""Build seed.sql for the Case Management mock database.

Part A copies app SETUP rows from the local dev DB (Option A), with sensitive
business values overridden. Part B writes invented people/cases/sessions.
No client tables are read for output.

usage: python -I build_seed.py config_doc_ids.json seed.sql
"""
import datetime as dt
import decimal
import json
import sys
import uuid

import pyodbc

SRC = r"DRIVER={ODBC Driver 18 for SQL Server};SERVER=LAPTOP-JIH94VS9\SQLEXPRESS;DATABASE=CaseManagement;Trusted_Connection=yes;Encrypt=no"
REAL_NPI_PLACEHOLDER = "1234567893"  # standard test NPI

# setup tables, in FK order, with per-column overrides (None = copy as-is)
SETUP = [
    ("Payer", {"AvailitySubmitterId": "MOCKSUBMITTER"}),
    ("PayerEDI", {}),
    ("PayerSubmissionConfiguration", {"SubmitterId": "MOCKSUBMITTER", "IsProduction": False,
                                      "SubmissionEndpoint": "https://clearinghouse.example.invalid/submit"}),
    ("FeeSchedule", {}),
    ("FeeScheduleLine", {}),
    ("BillingRule", {}),
    ("BillingRuleField", {}),
    ("BillingRuleLine", {}),
    ("ServiceCode", {}),
    ("MyConstants", {}),
    ("Pipeline", {}),
    ("ProjectorRule", {}),
    ("TableFieldMap", {}),
    ("WorkbookType", {}),
    ("WorkbookQ", {}),
    ("WorkbookQRule", {}),
    ("PracticeConfiguration", {
        "PracticeName": "Sunrise ABA Mock Practice", "BillingProviderName": "Sunrise ABA Mock Practice",
        "BillingProviderNPI": REAL_NPI_PLACEHOLDER, "TaxId": "000000000",
        "Address1": "100 Example Street", "Address2": None, "City": "Springfield", "State": "NJ", "Zip": "07081",
        "Phone": "555-010-0100", "Email": "billing@example.invalid",
        "SenderId": "MOCKSENDER", "ReceiverId": "MOCKRECEIVER",
        "SubmitterName": "Sunrise ABA Mock Practice", "SubmitterIdentifier": "MOCKSUBMITTER",
        "TestIndicator": "T"}),
]


def lit(v):
    if v is None:
        return "NULL"
    if isinstance(v, bool):
        return "1" if v else "0"
    if isinstance(v, (int, float, decimal.Decimal)):
        return str(v)
    if isinstance(v, (bytes, bytearray)):
        return "0x" + v.hex() if v else "0x"
    if isinstance(v, uuid.UUID):
        return f"'{v}'"
    if isinstance(v, dt.datetime):
        return f"'{v.isoformat(sep=' ')}'"
    if isinstance(v, (dt.date, dt.time)):
        return f"'{v.isoformat()}'"
    return "N'" + str(v).replace("'", "''") + "'"


def columns(cur, table):
    cur.execute("""SELECT c.name, c.is_identity FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id
                   WHERE c.object_id=OBJECT_ID(?) AND c.is_computed=0 AND t.name NOT IN ('timestamp','rowversion')
                   ORDER BY c.column_id""", f"cases.[{table}]")
    rows = cur.fetchall()
    return [r[0] for r in rows], any(r[1] for r in rows)


def emit_rows(out, table, cols, has_ident, rows):
    if not rows:
        return
    out.write(f"-- {table}: {len(rows)} rows\n")
    if has_ident:
        out.write(f"SET IDENTITY_INSERT cases.[{table}] ON;\n")
    collist = ", ".join(f"[{c}]" for c in cols)
    for r in rows:
        out.write(f"INSERT INTO cases.[{table}] ({collist}) VALUES ({', '.join(lit(v) for v in r)});\n")
    if has_ident:
        out.write(f"SET IDENTITY_INSERT cases.[{table}] OFF;\n")
    out.write("GO\n")


def copy_setup(cur, out, doc_ids):
    out.write("-- ===== Part A: app setup copied from dev DB (sensitive values replaced) =====\n")
    # documents first: setup tables point at them
    cols, ident = columns(cur, "Document")
    cur.execute(f"SELECT {', '.join(f'[{c}]' for c in cols)} FROM cases.Document WHERE DocumentId IN ({','.join('?' * len(doc_ids))})", doc_ids)
    rows = []
    for r in cur.fetchall():
        r = list(r)
        d = dict(zip(cols, r))
        assert d["CaseId"] is None, f"doc {d['DocumentId']} is case-attached"
        r[cols.index("CaseNumber")] = None
        if d["DocumentId"] == 174:  # 837 template carries the real provider NPI
            real = cur.execute("SELECT NPI FROM cases.Provider WHERE NPI IS NOT NULL").fetchall()
            data = r[cols.index("FileData")]
            for (npi,) in real:
                data = data.replace(npi.encode(), REAL_NPI_PLACEHOLDER.encode())
            r[cols.index("FileData")] = data
        rows.append(r)
    emit_rows(out, "Document", cols, ident, rows)
    for table, overrides in SETUP:
        cols, ident = columns(cur, table)
        cur.execute(f"SELECT {', '.join(f'[{c}]' for c in cols)} FROM cases.[{table}]")
        rows = []
        for r in cur.fetchall():
            r = list(r)
            for k, v in overrides.items():
                r[cols.index(k)] = v
            rows.append(r)
        emit_rows(out, table, cols, ident, rows)


FAKE = r"""
-- ===== Part B: invented data (no real people) =====
SET IDENTITY_INSERT cases.Provider ON;
INSERT INTO cases.Provider (Id, ProviderName, NPI, TaxId, Address1, City, State, ZipCode, Phone, Email, IsActive, ProviderCode, ProviderType, TaxonomyCode, FirstName, LastName, ClinicianUsername, ProviderRole) VALUES
 (1, N'Sunrise ABA Mock Practice', '1234567893', '000000000', N'100 Example Street', N'Springfield', 'NJ', '07081', '555-010-0100', 'office@example.invalid', 1, 'SUNRISE', 'Organization', '103K00000X', NULL, NULL, NULL, NULL),
 (2, N'Taylor Brightwater, BCBA', '1245319599', '000000000', N'100 Example Street', N'Springfield', 'NJ', '07081', '555-010-0101', 'tbrightwater@example.invalid', 1, 'TBRIGHTWATER', 'Individual', '103K00000X', N'Taylor', N'Brightwater', 'tbrightwater', 'BCBA');
SET IDENTITY_INSERT cases.Provider OFF;
GO
INSERT INTO cases.ProviderStaff (ProviderId, StaffCode, FirstName, LastName, ProviderType, Credential, Email, Phone, HireDate, IsActive) VALUES
 (1, 'RBT-101', N'Alex', N'Morgan', 'RBT', 'RBT', 'amorgan@example.invalid', '555-010-0201', '2025-01-06', 1),
 (1, 'RBT-102', N'Jordan', N'Lee', 'RBT', 'RBT', 'jlee@example.invalid', '555-010-0202', '2025-03-17', 1),
 (1, 'RBT-103', N'Casey', N'Park', 'RBT', 'RBT', 'cpark@example.invalid', '555-010-0203', '2025-08-04', 1),
 (2, 'BCBA-201', N'Taylor', N'Brightwater', 'BCBA', 'BCBA', 'tbrightwater@example.invalid', '555-010-0101', '2024-09-03', 1);
GO
SET IDENTITY_INSERT cases.[Case] ON;
INSERT INTO cases.[Case] (CaseId, CaseNumber, Title, Description, Status, OpenedDate, CreatedBy, CreatedDate, IsActive) VALUES
 (1, 'CASE-0000000001', N'Sam Testcase - ABA services', N'Mock case for development', 'Open', '2026-06-01', 'mock-seed', SYSUTCDATETIME(), 1),
 (2, 'CASE-0000000002', N'Riley Example - ABA services', N'Mock case for development', 'Open', '2026-08-15', 'mock-seed', SYSUTCDATETIME(), 1),
 (3, 'CASE-0000000003', N'Jamie Sample - ABA services', N'Mock case for development', 'Open', '2026-10-01', 'mock-seed', SYSUTCDATETIME(), 1);
SET IDENTITY_INSERT cases.[Case] OFF;
GO
INSERT INTO cases.Patient (CaseId, MemberId, MedicaidId, FirstName, LastName, DateOfBirth, Gender, Address1, City, State, ZipCode, Phone, Email, IsActive, CreatedBy) VALUES
 (1, 'MOCK000001', NULL, N'Sam', N'Testcase', '2019-04-12', 'M', N'1 Mockingbird Lane', N'Springfield', 'NJ', '07081', '555-010-1001', 'parent1@example.invalid', 1, 'mock-seed'),
 (2, 'MOCK000002', NULL, N'Riley', N'Example', '2020-11-03', 'F', N'2 Sample Road', N'Springfield', 'NJ', '07081', '555-010-1002', 'parent2@example.invalid', 1, 'mock-seed'),
 (3, 'MOCK000003', 'MCD0000003', N'Jamie', N'Sample', '2018-07-22', 'M', N'3 Placeholder Ave', N'Springfield', 'NJ', '07081', '555-010-1003', 'parent3@example.invalid', 1, 'mock-seed');
GO
INSERT INTO cases.InsuranceCoverage (CaseId, PayerId, MemberId, GroupNumber, SubscriberName, RelationshipCode, EffectiveDate, SubscriberFirstName, SubscriberLastName, SubscriberDateOfBirth, SubscriberGender, SubscriberAddress1, SubscriberCity, SubscriberState, SubscriberZipCode, SubscriberPhone, SubscriberEmail) VALUES
 (1, 3,  'MOCK000001', 'GRP-MOCK-1', N'Pat Testcase', '19', '2026-01-01', N'Pat', N'Testcase', '1988-02-14', 'F', N'1 Mockingbird Lane', N'Springfield', 'NJ', '07081', '555-010-1001', 'parent1@example.invalid'),
 (2, 1,  'MOCK000002', 'GRP-MOCK-2', N'Chris Example', '19', '2026-01-01', N'Chris', N'Example', '1990-06-30', 'M', N'2 Sample Road', N'Springfield', 'NJ', '07081', '555-010-1002', 'parent2@example.invalid'),
 (3, 14, 'MOCK000003', NULL, N'Jamie Sample', 'SEL', '2026-01-01', N'Jamie', N'Sample', '2018-07-22', 'M', N'3 Placeholder Ave', N'Springfield', 'NJ', '07081', '555-010-1003', 'parent3@example.invalid');
GO
INSERT INTO cases.Diagnosis (CaseId, DiagnosisCode, Description, IsPrimary) VALUES
 (1, 'F84.0', N'Autistic disorder', 1),
 (2, 'F84.0', N'Autistic disorder', 1),
 (3, 'F84.0', N'Autistic disorder', 1),
 (3, 'F90.2', N'Attention-deficit hyperactivity disorder, combined type', 0);
GO
INSERT INTO cases.[Authorization] (CaseId, AuthorizationNumber, StartDate, EndDate, AuthorizedUnits, Notes) VALUES
 (1, 'AUTH-MOCK-0001', '2026-06-01', '2026-11-30', 480, N'Mock authorization'),
 (2, 'AUTH-MOCK-0002', '2026-08-15', '2027-02-14', 360, N'Mock authorization'),
 (3, 'AUTH-MOCK-0003', '2026-10-01', '2027-03-31', 400, N'Mock authorization');
GO
INSERT INTO cases.SessionSchedule (CaseId, DayOfWeek, StartTime, EndTime, ProviderId, CPTCode, CPTModifier1, IsActive, CreatedBy) VALUES
 (1, 'Mon', '16:00', '18:00', 1, '97153', 'HM', 1, 'mock-seed'),
 (1, 'Wed', '16:00', '18:00', 1, '97153', 'HM', 1, 'mock-seed'),
 (2, 'Tue', '15:00', '17:00', 1, '97153', 'HM', 1, 'mock-seed'),
 (2, 'Thu', '15:00', '17:00', 1, '97153', 'HM', 1, 'mock-seed'),
 (3, 'Fri', '10:00', '12:00', 1, '97153', 'HM', 1, 'mock-seed');
GO
-- sessions: past 6 weeks from the schedules above, plus BCBA supervision (97155)
;WITH s AS (
  SELECT ss.CaseId, ss.StartTime, ss.EndTime, ss.CPTCode, ss.CPTModifier1, ss.DayOfWeek, w.n
  FROM cases.SessionSchedule ss CROSS JOIN (VALUES (1),(2),(3),(4),(5),(6)) w(n)
), d AS (
  SELECT s.*, DATEADD(day, -7*s.n + (CHARINDEX(s.DayOfWeek,'MonTueWedThuFri')-1)/3
         - (DATEDIFF(day, '19000101', CAST(GETDATE() AS date)) % 7), CAST(GETDATE() AS date)) AS SessionDate
  FROM s
)
INSERT INTO cases.Session (CaseId, SessionDate, ProviderId, Location, NotesSummary, CreatedBy, StartTime, EndTime, CPTCode, CPTModifier1, Units, DurationMinutes, BillingStatus, SessionStatus, PlaceOfServiceCode, AuthorizationNumber, ClientName)
SELECT d.CaseId, d.SessionDate, 1, N'Home', N'Mock session notes: worked on manding and transitions.', 'mock-seed',
       d.StartTime, d.EndTime, d.CPTCode, d.CPTModifier1, 8, 120, 'Unbilled', 'Completed', '12',
       'AUTH-MOCK-000' + CAST(d.CaseId AS varchar), p.FirstName + N' ' + p.LastName
FROM d JOIN cases.Patient p ON p.CaseId = d.CaseId
WHERE d.SessionDate >= (SELECT OpenedDate FROM cases.[Case] c WHERE c.CaseId = d.CaseId);
INSERT INTO cases.Session (CaseId, SessionDate, ProviderId, Location, NotesSummary, CreatedBy, StartTime, EndTime, CPTCode, CPTModifier1, Units, DurationMinutes, BillingStatus, SessionStatus, PlaceOfServiceCode, AuthorizationNumber, ClientName)
SELECT p.CaseId, DATEADD(day, -10, CAST(GETDATE() AS date)), 2, N'Home', N'Mock BCBA supervision and protocol update.', 'mock-seed',
       '16:00', '17:00', '97155', 'HO', 4, 60, 'Unbilled', 'Completed', '12',
       'AUTH-MOCK-000' + CAST(p.CaseId AS varchar), p.FirstName + N' ' + p.LastName
FROM cases.Patient p;
GO
"""


def main():
    doc_ids = json.load(open(sys.argv[1]))
    cn = pyodbc.connect(SRC)
    cur = cn.cursor()
    with open(sys.argv[2], "w", encoding="utf-8", newline="\n") as out:
        out.write("-- Case Management MOCK seed. Setup copied from dev with sensitive values replaced; people are invented.\n")
        out.write("SET NOCOUNT ON;\nSET QUOTED_IDENTIFIER ON;\nGO\n")
        copy_setup(cur, out, doc_ids)
        out.write(FAKE)
    print("wrote", sys.argv[2])


if __name__ == "__main__":
    main()
