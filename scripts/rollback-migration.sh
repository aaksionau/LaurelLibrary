#!/bin/bash

# Rollback Database Migration Script
# This script helps rollback to a previous migration safely

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
PERSISTENCE_PROJECT="$PROJECT_ROOT/LaurelLibrary.Persistence"
STARTUP_PROJECT="$PROJECT_ROOT/LaurelLibrary.UI"

echo -e "${BLUE}=== Database Migration Rollback Tool ===${NC}\n"

print_step() {
    echo -e "\n${BLUE}>>> $1${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Show current migrations
print_step "Current migrations:"
echo -e "${YELLOW}"
dotnet ef migrations list \
    --project "$PERSISTENCE_PROJECT" \
    --startup-project "$STARTUP_PROJECT" \
    --no-build
echo -e "${NC}"

# Get target migration
print_warning "Enter the migration name to rollback to (or press Enter to list migrations):"
read -r TARGET_MIGRATION

if [[ -z "$TARGET_MIGRATION" ]]; then
    echo "Migration rollback cancelled."
    exit 0
fi

# Confirm rollback
print_warning "This will rollback the database to migration: $TARGET_MIGRATION"
print_warning "All data changes from subsequent migrations will be LOST!"
read -p "Type 'yes' to confirm rollback: " confirm

if [[ "$confirm" != "yes" ]]; then
    echo "Rollback cancelled."
    exit 0
fi

# Generate rollback script
print_step "Generating rollback script..."
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
SQL_FILE="$PROJECT_ROOT/migrations/rollback_${TIMESTAMP}.sql"
mkdir -p "$PROJECT_ROOT/migrations"

dotnet ef migrations script \
    "$TARGET_MIGRATION" \
    --project "$PERSISTENCE_PROJECT" \
    --startup-project "$STARTUP_PROJECT" \
    --output "$SQL_FILE"

print_success "Rollback script generated: $SQL_FILE"

# Apply rollback
read -p "Review script and proceed with rollback? (yes/no): " proceed
if [[ "$proceed" == "yes" ]]; then
    dotnet ef database update "$TARGET_MIGRATION" \
        --project "$PERSISTENCE_PROJECT" \
        --startup-project "$STARTUP_PROJECT"
    
    if [[ $? -eq 0 ]]; then
        print_success "Database rolled back successfully to: $TARGET_MIGRATION"
    else
        print_error "Rollback failed!"
        exit 1
    fi
else
    echo "Rollback cancelled. Script saved at: $SQL_FILE"
fi
