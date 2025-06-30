# Cohort Manager Database Management Guide

## 0. Create an `appsettings.json` file (required for migrations to run)

If it doesn’t already exist, create a file named `appsettings.json` in the root of the `DataServices.Migrations` project with the following content:

```json
{
  "DtOsDatabaseConnectionString": "Server=localhost,1433;Database=DToSDB;User Id=SA;Password=Password123!;TrustServerCertificate=True"
}
```

> 🔒 **Note:** Do not commit this file to source control. Add it to `.gitignore` if it isn’t already.

---

## 1. Create the new tables / objects in your local database

Update your local SQL Server database with the desired changes (new tables, columns, etc.).

---

## 2. Update the `DbContext`, mapping classes, and EF models to reflect the changes

Update the Entity Framework model classes, mapping configuration, and `DbContext` class to match your schema changes.

---

## 3. Generate the migration script

From the `DataServices.Migrations` folder, run:

```bash
dotnet ef migrations add <MigrationName>
```

Replace `<MigrationName>` with a meaningful name that reflects the change (e.g., `add-nems-subscription-id`).

If this command fails with a "command not found" error, install the EF CLI tool with:

```bash
dotnet tool install --global dotnet-ef
```

---

## 4. Two files should be added to the `Migrations` folder

These are:

- A new C# file containing the migration logic.
- An updated model snapshot file.

---

## 5. Test the migration

Reset your local database schema if needed and apply the migration by running:

```bash
dotnet run
```

(from within the `DataServices.Migrations` folder)

This will apply the new migration and optionally seed any required data.

---
