USE master;
GO

IF NOT EXISTS (
        SELECT name
        FROM sys.databases
        WHERE name = N'DtOsNHSDB'
        )
    CREATE DATABASE [DtOsNHSDB];
GO

IF SERVERPROPERTY('ProductVersion') > '12'
    ALTER DATABASE [DtOsNHSDB] SET QUERY_STORE = ON;
GO


