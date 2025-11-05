# Safe Production Database Migration Workflow

This directory contains scripts for safely applying EF Core migrations to production databases.

## Quick Start

### 1. Verify Migrations
```bash
./scripts/verify-migrations.sh
```

### 2. Generate Migration Script (Dry Run)
```bash
./scripts/apply-migrations.sh Production true
```

### 3. Review Generated Script
Check `migrations/migration_<timestamp>.sql`

### 4. Apply to Production
```bash
./scripts/apply-migrations.sh Production false
```

## Available Scripts

### `apply-migrations.sh`
Main script for applying migrations safely.

**Usage:**
```bash
./scripts/apply-migrations.sh [Environment] [DryRun]

# Examples:
./scripts/apply-migrations.sh Production true   # Generate script only
./scripts/apply-migrations.sh Production false  # Apply to production
./scripts/apply-migrations.sh Staging false     # Apply to staging
```

**Features:**
- ✅ Generates idempotent SQL scripts
- ✅ Shows pending migrations
- ✅ Requires backup confirmation
- ✅ Multiple confirmation prompts for production
- ✅ Archives applied scripts

### `verify-migrations.sh`
Validates migrations before applying.

**Usage:**
```bash
./scripts/verify-migrations.sh
```

**Checks:**
- Migration script generation
- SQL syntax validity
- Pending model changes

### `rollback-migration.sh`
Safely rollback to a previous migration.

**Usage:**
```bash
./scripts/rollback-migration.sh
```

**Features:**
- Lists available migrations
- Generates rollback script
- Requires explicit confirmation

## Workflow Overview

```
┌─────────────────────┐
│  Verify Migrations  │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│   Dry Run (Script)  │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│   Review Script     │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  Backup Database    │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  Apply Migration    │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Verify & Monitor    │
└─────────────────────┘
```

## Safety Features

1. **Idempotent Scripts** - Can be run multiple times safely
2. **Backup Checks** - Prompts for backup confirmation
3. **Multi-step Confirmation** - Multiple "yes" confirmations required
4. **Script Archival** - All applied scripts are saved
5. **Dry Run Mode** - Test without applying changes
6. **Rollback Support** - Easy rollback to previous state

## Directory Structure

```
LaurelLibrary/
├── scripts/
│   ├── apply-migrations.sh      # Main migration script
│   ├── verify-migrations.sh     # Verification script
│   └── rollback-migration.sh    # Rollback script
├── migrations/
│   ├── migration_*.sql          # Generated scripts
│   └── applied/                 # Archive of applied scripts
└── MIGRATION_GUIDE.md           # Complete documentation
```

## GitHub Actions

A workflow is available at `.github/workflows/database-migration.yml` for CI/CD integration.

**Trigger manually via GitHub UI:**
1. Go to Actions tab
2. Select "Database Migration"
3. Click "Run workflow"
4. Choose environment and dry-run option

## Best Practices

✅ **Always run dry-run first**
✅ **Review generated SQL carefully**
✅ **Backup before applying**
✅ **Test in staging environment**
✅ **Have rollback plan ready**

❌ **Never skip backup**
❌ **Never apply untested migrations**
❌ **Never rush production deployments**

## Troubleshooting

See `MIGRATION_GUIDE.md` for detailed troubleshooting information.

## Manual Commands

If you prefer to run commands directly:

```bash
# Generate script
dotnet ef migrations script \
    --project LaurelLibrary.Persistence \
    --startup-project LaurelLibrary.UI \
    --idempotent \
    --output migration.sql

# Apply migration
dotnet ef database update \
    --project LaurelLibrary.Persistence \
    --startup-project LaurelLibrary.UI

# List migrations
dotnet ef migrations list \
    --project LaurelLibrary.Persistence \
    --startup-project LaurelLibrary.UI
```
