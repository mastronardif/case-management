------------------------------------------------------------
-- Export all Stored Procedures to individual .sql files
-- Requires xp_cmdshell enabled and write access to folder
------------------------------------------------------------
--sqlcmd -S localhost -d YourDatabaseName -E -i export_sps.sql
--sqlcmd -S "LAPTOP-JIH94VS9\SQLEXPRESS" -d AdventureWorksDW -E -i "C:\Users\mastronardif\source\repos\CaseMangement\WebAppMulti\Database\Scripts\export_sps.sql"
-- python export_stored_procs.py
--     "DefaultConnection": "Server=LAPTOP-JIH94VS9\\SQLEXPRESS;Database=AdventureWorksDW;Trusted_Connection=True;Encrypt=False;"

-- === CONFIGURATION ===
--DECLARE @ExportPath NVARCHAR(4000) = 'C:\Exports\SPs';  -- no trailing backslash

------------------------------------------------------------
-- Enable xp_cmdshell if needed
------------------------------------------------------------
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'xp_cmdshell', 1;
RECONFIGURE;

------------------------------------------------------------
-- Create folder if it doesn’t exist
------------------------------------------------------------
DECLARE @ExportPath NVARCHAR(4000) = '.\SPs';  -- relative path from current folder
DECLARE @mkdir NVARCHAR(4000);
SET @mkdir = 'if not exist "' + @ExportPath + '" mkdir "' + @ExportPath + '"';
EXEC xp_cmdshell @mkdir, NO_OUTPUT;


------------------------------------------------------------
-- Cursor through all user stored procedures
------------------------------------------------------------
DECLARE @ProcName SYSNAME;
DECLARE @SchemaName SYSNAME;
DECLARE @FullName NVARCHAR(512);
DECLARE @FileName NVARCHAR(4000);
DECLARE @SQL NVARCHAR(MAX);
DECLARE @cmd NVARCHAR(4000);

DECLARE cur CURSOR FOR
SELECT s.name AS SchemaName, p.name AS ProcName
FROM sys.procedures p
INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
ORDER BY s.name, p.name;

OPEN cur;
FETCH NEXT FROM cur INTO @SchemaName, @ProcName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @FullName = QUOTENAME(@SchemaName) + '.' + QUOTENAME(@ProcName);
    SET @FileName = @ExportPath + '\' + @SchemaName + '.' + @ProcName + '.sql';

    -- Get definition text
    SELECT @SQL = OBJECT_DEFINITION(OBJECT_ID(@FullName));

    IF @SQL IS NOT NULL
    BEGIN
        -- Add DROP + GO + CREATE text for cleaner export
        SET @SQL = 'IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N''' 
            + @FullName + ''') AND type = N''P'') DROP PROCEDURE ' + @FullName 
            + CHAR(13) + 'GO' + CHAR(13) + CHAR(13)
            + @SQL + CHAR(13) + 'GO' + CHAR(13);

        -- Use sqlcmd-style echo to file via PowerShell (handles long text better)
        SET @cmd = 'powershell -Command "Set-Content -Path ''' + @FileName + ''' -Value @'''' + REPLACE(@SQL, '''', '''''''') + ''''"';

        EXEC xp_cmdshell @cmd, NO_OUTPUT;
        PRINT 'Exported: ' + @FileName;
    END

    FETCH NEXT FROM cur INTO @SchemaName, @ProcName;
END

CLOSE cur;
DEALLOCATE cur;

PRINT '✅ Export completed successfully!';
