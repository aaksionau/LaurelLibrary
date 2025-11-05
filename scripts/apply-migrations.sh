#!/bin/bash

# Safe Database Migration Script for Production
# This script helps apply EF Core migrations safely to production databases

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
PERSISTENCE_PROJECT="$PROJECT_ROOT/LaurelLibrary.Persistence"
STARTUP_PROJECT="$PROJECT_ROOT/LaurelLibrary.UI"
BACKUP_DIR="$PROJECT_ROOT/backups"

echo -e "${BLUE}=== Safe Database Migration Tool ===${NC}\n"

# Function to print step headers
print_step() {
    echo -e "\n${BLUE}>>> $1${NC}"
}

# Function to print success
print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

# Function to print warning
print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

# Function to print error
print_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Check if dotnet ef is installed
print_step "Checking prerequisites..."
if ! command -v dotnet-ef &> /dev/null; then
    print_error "dotnet-ef tool is not installed"
    echo "Install it with: dotnet tool install --global dotnet-ef"
    exit 1
fi
print_success "dotnet-ef is installed"

# Parse arguments
ENVIRONMENT="${1:-Production}"
DRY_RUN="${2:-false}"

echo -e "\nEnvironment: ${GREEN}$ENVIRONMENT${NC}"
echo -e "Dry Run: ${YELLOW}$DRY_RUN${NC}\n"

# Warning for production
if [[ "$ENVIRONMENT" == "Production" ]]; then
    print_warning "You are about to apply migrations to PRODUCTION!"
    read -p "Type 'yes' to continue: " confirm
    if [[ "$confirm" != "yes" ]]; then
        echo "Migration cancelled."
        exit 0
    fi
fi

# Step 1: Generate idempotent SQL script
print_step "Step 1: Generating idempotent migration script..."
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
SQL_FILE="$PROJECT_ROOT/migrations/migration_${TIMESTAMP}.sql"
mkdir -p "$PROJECT_ROOT/migrations"

dotnet ef migrations script \
    --project "$PERSISTENCE_PROJECT" \
    --startup-project "$STARTUP_PROJECT" \
    --idempotent \
    --output "$SQL_FILE"

if [[ $? -eq 0 ]]; then
    print_success "Migration script generated: $SQL_FILE"
else
    print_error "Failed to generate migration script"
    exit 1
fi

# Step 2: Show pending migrations
print_step "Step 2: Checking pending migrations..."
echo -e "${YELLOW}"
dotnet ef migrations list \
    --project "$PERSISTENCE_PROJECT" \
    --startup-project "$STARTUP_PROJECT" \
    --no-build
echo -e "${NC}"

# Step 3: Review the script
print_step "Step 3: Review the migration script"
echo -e "Migration script location: ${GREEN}$SQL_FILE${NC}"
echo -e "\nFirst 50 lines of the script:"
echo -e "${YELLOW}----------------------------------------${NC}"
head -n 50 "$SQL_FILE"
echo -e "${YELLOW}----------------------------------------${NC}"

read -p "Do you want to view the full script? (y/n): " view_full
if [[ "$view_full" == "y" ]]; then
    less "$SQL_FILE"
fi

# Step 4: Backup recommendation
print_step "Step 4: Database backup"
print_warning "IMPORTANT: Ensure you have a recent database backup before proceeding!"
print_warning "Recommended backup location: $BACKUP_DIR"

if [[ "$ENVIRONMENT" == "Production" ]]; then
    read -p "Have you confirmed a backup exists? (yes/no): " backup_confirm
    if [[ "$backup_confirm" != "yes" ]]; then
        echo "Please create a backup first. Migration cancelled."
        exit 0
    fi
fi

# Step 5: Apply migrations
if [[ "$DRY_RUN" == "true" ]]; then
    print_step "Step 5: Dry run mode - Script generated but NOT applied"
    print_success "Dry run complete. Review the script at: $SQL_FILE"
    echo -e "\nTo apply migrations, run:"
    echo -e "  ${GREEN}./scripts/apply-migrations.sh $ENVIRONMENT false${NC}"
else
    print_step "Step 5: Applying migrations to database..."
    
    read -p "Proceed with migration? (yes/no): " apply_confirm
    if [[ "$apply_confirm" != "yes" ]]; then
        echo "Migration cancelled."
        exit 0
    fi
    
    # Apply the migration
    dotnet ef database update \
        --project "$PERSISTENCE_PROJECT" \
        --startup-project "$STARTUP_PROJECT"
    
    if [[ $? -eq 0 ]]; then
        print_success "Migrations applied successfully!"
        
        # Archive the script
        ARCHIVE_DIR="$PROJECT_ROOT/migrations/applied"
        mkdir -p "$ARCHIVE_DIR"
        cp "$SQL_FILE" "$ARCHIVE_DIR/"
        print_success "Script archived to: $ARCHIVE_DIR"
    else
        print_error "Migration failed!"
        print_error "Script saved at: $SQL_FILE"
        exit 1
    fi
fi

print_step "Migration process complete!"
