## Database Setup


CREATE DATABASE CaseManagement;
GO

CREATE SCHEMA cases;
GO

"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=CaseManagement;Trusted_Connection=True;Encrypt=False;"
}

dotnet run -- import.csv "Server=LAPTOP-JIH94VS9\SQLEXPRESS;Database=CaseManagement;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;"

