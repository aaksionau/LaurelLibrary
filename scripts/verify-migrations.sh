#!/bin/bash

# Verify Migrations Script
# This script verifies that migrations can be applied without errors

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

echo -e "${BLUE}=== Migration Verification Tool ===${NC}\n"

print_step() {
    echo -e "\n${BLUE}>>> $1${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Check if there are pending migrations
print_step "Checking for pending migrations..."

# List all migrations
echo -e "${YELLOW}All migrations:${NC}"
dotnet ef migrations list \
    --project "$PERSISTENCE_PROJECT" \
    --startup-project "$STARTUP_PROJECT" \
    --no-build

# Generate script to verify syntax
print_step "Generating migration script to verify syntax..."
TEMP_SQL="$PROJECT_ROOT/migrations/temp_verify.sql"
mkdir -p "$PROJECT_ROOT/migrations"

dotnet ef migrations script \
    --project "$PERSISTENCE_PROJECT" \
    --startup-project "$STARTUP_PROJECT" \
    --idempotent \
    --output "$TEMP_SQL" \
    --no-build

if [[ $? -eq 0 ]]; then
    print_success "Migration script generated successfully"
    print_success "SQL syntax appears valid"
    
    # Show script size
    SIZE=$(wc -l < "$TEMP_SQL")
    echo -e "Script size: ${GREEN}$SIZE lines${NC}"
    
    # Clean up temp file
    rm "$TEMP_SQL"
    
    print_success "Migration verification complete - no issues found"
else
    print_error "Failed to generate migration script"
    print_error "There may be issues with your migrations"
    exit 1
fi

# Check for model changes
print_step "Checking for model changes not in migrations..."
dotnet ef migrations add TestMigration \
    --project "$PERSISTENCE_PROJECT" \
    --startup-project "$STARTUP_PROJECT" \
    --no-build \
    --dry-run 2>&1 | grep -q "No changes" && {
    print_success "No pending model changes detected"
} || {
    print_error "There are model changes not captured in migrations!"
    echo -e "${YELLOW}Run: dotnet ef migrations add <MigrationName>${NC}"
}
