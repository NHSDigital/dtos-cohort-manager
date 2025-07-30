
# Cohort Manager Database Management Guide

## 1. Create an `appsettings.json` file (required for migrations to run)

If it doesn’t already exist, create a file named `appsettings.json` in the root of the `DataServices.Migrations` project with the following content:

```json
{
  "DtOsDatabaseConnectionString": "Server=localhost,1433;Database=DToSDB;User Id=SA;Password=<your-password>;TrustServerCertificate=True"
}
```

Please replace "\<your-password\>" with the database password you've chosen in your local .env file

> 🔒 **Note:** Do not commit this file to source control. Add it to `.gitignore` if it isn’t already.

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

## 5. Run the migration

You can run migrations using either of the following methods:

### ✅ Option A: Run manually

From within the `DataServices.Migrations` folder:

```bash
dotnet run
```

This applies all pending migrations and seeds reference data if required.

### ✅ Option B: Use the VS Code Task (recommended)

Run the **"Run DB Migrations"** task from the VS Code task list.

This runs the following for you:

- Rebuilds the migrations container (if needed)
- Applies the new migration to your local DB via Docker Compose

Behind the scenes, this is equivalent to:

```bash
docker compose -f compose.core.yaml up db-migration
```

Make sure `db-migration` is defined as a service in `compose.core.yaml`.

---

## ✅ Summary

- Use `dotnet ef migrations add` to generate a migration
- Use `dotnet run` or the VS Code task to apply it
- Use `appsettings.json` locally to supply your DB connection string

