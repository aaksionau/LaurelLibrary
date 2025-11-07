# LaurelLibrary.Domain

[![GitHub Packages](https://img.shields.io/badge/GitHub%20Packages-aaksionau.LaurelLibrary.Domain-blue)](https://github.com/aaksionau/LaurelLibrary.Domain/packages)

The domain layer for the MyLibrarian Management System, containing core business entities, enums, and domain logic.

## Overview

This project represents the domain layer in a clean architecture implementation for a library management system. It contains the core business entities and enums that define the fundamental concepts and rules of the library domain.

## Technology Stack

- **.NET 9.0** - Target framework
- **Entity Framework Core** - Data modeling and relationships
- **ASP.NET Core Identity** - User authentication and authorization support

## Project Structure

### Entities

Core business entities that represent the main concepts in the library domain:

- **`AppUser`** - Application user extending Identity framework
- **`Audit`** - Base audit entity with common tracking fields
- **`Author`** - Book authors and their information
- **`Book`** - Books in the library catalog
- **`BookInstance`** - Physical copies of books
- **`Category`** - Book categorization
- **`ImportHistory`** - Track data import operations
- **`Kiosk`** - Self-service kiosks in libraries
- **`Library`** - Library branches and locations
- **`Reader`** - Library patrons/members

### Enums

Business enumerations that define valid states and types:

- **`BookInstanceStatus`** - Status of individual book copies (Available, Checked Out, etc.)
- **`ImportStatus`** - Status of data import operations

## Key Features

- **Multi-library support** - Manages multiple library branches
- **Book catalog management** - Books, authors, categories, and physical instances
- **User management** - Integration with ASP.NET Core Identity
- **Audit tracking** - Built-in audit trails for entity changes
- **Import tracking** - History of data import operations
- **Kiosk integration** - Support for self-service library kiosks

## Entity Relationships

The domain model supports complex relationships between entities:

- Libraries contain multiple books and kiosks
- Books can have multiple authors and belong to multiple categories
- Books can have multiple physical instances (BookInstance)
- Readers can interact with multiple libraries
- All entities inherit audit tracking capabilities

## Getting Started

### Installation

Install the package from GitHub Packages:

First, configure your NuGet sources to include GitHub Packages:

```bash
# Add GitHub Packages as a source (one-time setup)
dotnet nuget add source --username YOUR_GITHUB_USERNAME --password YOUR_GITHUB_TOKEN --store-password-in-clear-text --name github "https://nuget.pkg.github.com/aaksionau/index.json"
```

Then install the package:

```bash
# .NET CLI
dotnet add package aaksionau.LaurelLibrary.Domain

# PackageReference (add to .csproj)
<PackageReference Include="aaksionau.LaurelLibrary.Domain" Version="1.0.0" />
```

**Alternative: Using nuget.config file**

Create a `nuget.config` file in your solution root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/aaksionau/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_TOKEN" />
    </github>
  </packageSourceCredentials>
</configuration>
```

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or JetBrains Rider (recommended IDEs)

### Building the Project

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests (when available)
dotnet test
```

## Development Guidelines

### Entity Design Principles

- All entities inherit from `Audit` base class for tracking
- Use required properties for mandatory fields
- Apply appropriate string length constraints
- Use GUIDs for primary keys
- Implement proper navigation properties for relationships

### Code Style

- Enable nullable reference types
- Use implicit usings
- Follow C# naming conventions
- Apply data annotations for validation
- Use Entity Framework Core conventions and configurations

## Dependencies

- **Microsoft.AspNetCore.Identity.EntityFrameworkCore** (9.0.9) - Identity framework integration

## Architecture

This domain layer follows Domain-Driven Design (DDD) principles:

- **Entities** - Objects with identity and lifecycle
- **Value Objects** - Objects defined by their attributes (enums)
- **Domain Logic** - Business rules embedded in entities
- **Separation of Concerns** - Pure domain logic without infrastructure dependencies

## Contributing

When adding new entities or modifying existing ones:

1. Ensure all entities inherit from `Audit` base class
2. Apply appropriate data annotations for validation
3. Configure Entity Framework relationships properly
4. Update this README with any new entities or significant changes
5. Follow established naming and coding conventions

## License

[Add your license information here]

## Related Projects

This domain layer is part of a larger library management system. Related projects may include:

- LaurelLibrary.Infrastructure - Data access and external services
- LaurelLibrary.Application - Application services and use cases
- LaurelLibrary.Web - Web API or MVC presentation layer

---

*This project is part of the MyLibrarian Management System ecosystem.*