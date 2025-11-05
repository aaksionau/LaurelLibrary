# Database Migration Guide

This guide provides safe procedures for applying Entity Framework Core migrations to the LaurelLibrary database in production.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Development Workflow](#development-workflow)
- [Production Deployment Workflow](#production-deployment-workflow)
- [Emergency Rollback](#emergency-rollback)
- [Troubleshooting](#troubleshooting)

## Prerequisites

1. **Install EF Core Tools**
   ```bash
   dotnet tool install --global dotnet-ef
   # or update existing
   dotnet tool update --global dotnet-ef
   ```

2. **Verify Installation**
   ```bash
   dotnet ef --version
   ```

3. **Database Backup Access**
   - Ensure you have the ability to backup and restore the database
   - Test your backup/restore process before any production migration

## Development Workflow

### Creating a New Migration

1. **Make your model changes** in the `LaurelLibrary.Domain` project

2. **Create a migration**:
   ```bash
   cd /home/alex/Code/LaurelLibrary
   dotnet ef migrations add YourMigrationName \
       --project LaurelLibrary.Persistence \
       --startup-project LaurelLibrary.UI
   ```

3. **Review the generated migration** in `LaurelLibrary.Persistence/Migrations/`

4. **Test locally**:
   ```bash
   dotnet ef database update \
       --project LaurelLibrary.Persistence \
       --startup-project LaurelLibrary.UI
   ```

5. **Verify the changes** work with your application

### Automated Migration in Development

For development only, you can enable automatic migrations in `Program.cs`:

```csharp
// After: var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
}
```

⚠️ **Never use automatic migration in production!**

## Production Deployment Workflow

### Step 1: Pre-Deployment Checklist

- [ ] All migrations tested in development
- [ ] All migrations tested in staging environment
- [ ] Database backup confirmed and tested
- [ ] Rollback plan prepared
- [ ] Maintenance window scheduled (if needed)
- [ ] Stakeholders notified

### Step 2: Verify Migrations

```bash
cd /home/alex/Code/LaurelLibrary
./scripts/verify-migrations.sh
```

This script:
- Lists all migrations
- Verifies SQL syntax
- Checks for unmigrated model changes

### Step 3: Generate Migration Script (Dry Run)

```bash
./scripts/apply-migrations.sh Production true
```

This will:
1. Generate an idempotent SQL script
2. Show pending migrations
3. Display script preview
4. Save script to `migrations/` directory
5. **NOT apply** any changes

### Step 4: Review the Generated Script

```bash
# View the latest migration script
ls -lt migrations/migration_*.sql | head -1
less migrations/migration_<timestamp>.sql
```

Review for:
- Unexpected table drops
- Data loss operations
- Missing indexes
- Performance concerns

### Step 5: Backup Production Database

**Azure SQL Database:**
```bash
# Manual backup via Azure Portal or CLI
az sql db export \
    --resource-group <resource-group> \
    --server <server-name> \
    --name <database-name> \
    --admin-user <admin> \
    --admin-password <password> \
    --storage-key-type StorageAccessKey \
    --storage-key <key> \
    --storage-uri <blob-uri>
```

**SQL Server:**
```sql
BACKUP DATABASE [LaurelLibrary]
TO DISK = 'C:\Backups\LaurelLibrary_PreMigration_<date>.bak'
WITH FORMAT, COMPRESSION;
```

### Step 6: Apply Migrations to Production

```bash
./scripts/apply-migrations.sh Production false
```

This will:
1. Regenerate the migration script
2. Show pending migrations
3. Prompt for confirmation
4. Verify backup exists
5. Apply migrations to the database
6. Archive the applied script

### Step 7: Verify Deployment

1. **Check migration status**:
   ```bash
   dotnet ef migrations list \
       --project LaurelLibrary.Persistence \
       --startup-project LaurelLibrary.UI
   ```

2. **Verify application functionality**:
   - Test critical features
   - Check logs for errors
   - Monitor performance

3. **Database verification queries**:
   ```sql
   -- Check applied migrations
   SELECT * FROM __EFMigrationsHistory 
   ORDER BY MigrationId DESC;
   
   -- Verify table structure
   SELECT TABLE_NAME 
   FROM INFORMATION_SCHEMA.TABLES 
   WHERE TABLE_TYPE = 'BASE TABLE';
   ```

## Emergency Rollback

If you need to rollback a migration:

### Option 1: Using Rollback Script

```bash
./scripts/rollback-migration.sh
```

Follow the prompts to select the target migration.

### Option 2: Manual Rollback

```bash
# List migrations to find target
dotnet ef migrations list \
    --project LaurelLibrary.Persistence \
    --startup-project LaurelLibrary.UI

# Rollback to specific migration
dotnet ef database update <PreviousMigrationName> \
    --project LaurelLibrary.Persistence \
    --startup-project LaurelLibrary.UI
```

### Option 3: Restore from Backup

If data integrity is compromised, restore from backup:

**Azure SQL:**
```bash
az sql db restore \
    --resource-group <resource-group> \
    --server <server-name> \
    --name <database-name> \
    --source-database <backup-name> \
    --time <restore-point-time>
```

## Alternative Approaches

### Using SQL Scripts Directly

For maximum control, apply the generated SQL script manually:

```bash
# Generate script
dotnet ef migrations script \
    --project LaurelLibrary.Persistence \
    --startup-project LaurelLibrary.UI \
    --idempotent \
    --output migration.sql

# Review and apply with your preferred SQL tool
sqlcmd -S <server> -d <database> -i migration.sql
```

### CI/CD Pipeline Integration

Add to your deployment pipeline:

```yaml
# Example GitHub Actions step
- name: Apply Database Migrations
  run: |
    dotnet ef database update \
      --project LaurelLibrary.Persistence \
      --startup-project LaurelLibrary.UI \
      --connection "${{ secrets.PRODUCTION_CONNECTION_STRING }}"
```

## Troubleshooting

### Migration Fails with Timeout

Increase command timeout in your connection string:
```
"DefaultConnection": "Server=...;Command Timeout=300;"
```

### Migration Already Applied

The idempotent scripts handle this automatically. They check `__EFMigrationsHistory` table.

### Pending Model Changes

```bash
# Check what's different
dotnet ef migrations add CheckChanges \
    --project LaurelLibrary.Persistence \
    --startup-project LaurelLibrary.UI

# If changes are expected, keep the migration
# If not, remove it:
dotnet ef migrations remove \
    --project LaurelLibrary.Persistence \
    --startup-project LaurelLibrary.UI
```

### Connection String Issues

Verify your connection string in:
- `appsettings.json` (template)
- `appsettings.Production.json` (production values)
- Azure App Configuration / Key Vault (if used)

### Permission Errors

Ensure your database user has these permissions:
- CREATE TABLE
- ALTER TABLE
- DROP TABLE (for rollbacks)
- INSERT, UPDATE, DELETE on `__EFMigrationsHistory`

## Best Practices

✅ **Always backup before migrations**
✅ **Test migrations in staging first**
✅ **Use idempotent scripts**
✅ **Review generated SQL**
✅ **Have a rollback plan**
✅ **Monitor application after migration**
✅ **Document breaking changes**

❌ **Never use automatic migration in production**
❌ **Never skip testing migrations**
❌ **Never modify applied migrations**
❌ **Never delete migration files from source control**

## Quick Reference

```bash
# Create migration
dotnet ef migrations add <Name> --project LaurelLibrary.Persistence --startup-project LaurelLibrary.UI

# List migrations
dotnet ef migrations list --project LaurelLibrary.Persistence --startup-project LaurelLibrary.UI

# Apply migrations
./scripts/apply-migrations.sh Production false

# Rollback
./scripts/rollback-migration.sh

# Generate script only
./scripts/apply-migrations.sh Production true

# Remove last migration (if not applied)
dotnet ef migrations remove --project LaurelLibrary.Persistence --startup-project LaurelLibrary.UI
```

## Support

For issues or questions:
1. Check the [troubleshooting section](#troubleshooting)
2. Review EF Core documentation: https://docs.microsoft.com/ef/core/
3. Check application logs in Azure Portal
