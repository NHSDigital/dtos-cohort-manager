USE master;
GO

IF NOT EXISTS (
        SELECT name
        FROM sys.databases
        WHERE name = N'DtOsNHSDB'
        )
    CREATE DATABASE [DToSDB];
GO

IF SERVERPROPERTY('ProductVersion') > '12'
    ALTER DATABASE [DToSDB] SET QUERY_STORE = ON;
GO


