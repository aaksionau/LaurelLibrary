# LaurelLibrary

A modern library management system built with ASP.NET Core, designed to help libraries manage their book collections, readers, and check-out operations through both web and kiosk interfaces.

## 🚀 Features

### Library Management
- **Multi-library support**: Manage multiple library branches within a single application
- **Book cataloging**: Comprehensive book information including titles, authors, categories, publishers, and synopses
- **Book instances**: Track individual physical copies of books with unique barcodes
- **Inventory management**: Monitor book availability and location across libraries

### Reader Management
- **Reader registration**: Register library patrons with personal information
- **EAN barcode generation**: Automatically generate unique EAN-13 barcodes for readers
- **Barcode image storage**: Store barcode images in Azure Blob Storage for scalability
- **Date of birth tracking**: Maintain reader demographic information

### Check-out System
- **Configurable loan periods**: Set custom checkout durations per library (default: 14 days)
- **Book instance tracking**: Monitor which specific copy is checked out
- **Check-out history**: Audit trail of all library transactions

### Kiosk Mode
- **Self-service kiosks**: Browser-based kiosk interface for library patrons
- **Browser fingerprinting**: Identify and manage kiosk terminals
- **Location tracking**: Associate kiosks with specific physical locations

### Authentication & Authorization
- **ASP.NET Core Identity**: Secure user authentication and management
- **Microsoft Account integration**: Allow users to sign in with Microsoft accounts (Outlook, Office 365, etc.)
- **Multi-library access**: Users can be administrators of multiple libraries
- **External login support**: Ready for additional OAuth providers (Google, Facebook, etc.)

### Azure Integration
- **Azure Blob Storage**: Cloud-based storage for barcode images and library logos
- **SQL Server database**: Robust data persistence with Entity Framework Core
- **Scalable architecture**: Cloud-ready design for production deployments

## 🏗️ Architecture

The project follows a clean architecture pattern with clear separation of concerns:

```
LaurelLibrary/
├── LaurelLibrary.Domain/          # Domain entities and enums
│   ├── Entities/                  # Core business entities
│   │   ├── AppUser.cs            # Application user (Identity)
│   │   ├── Library.cs            # Library branch
│   │   ├── Book.cs               # Book catalog
│   │   ├── BookInstance.cs       # Physical book copies
│   │   ├── Author.cs             # Book authors
│   │   ├── Category.cs           # Book categories
│   │   ├── Reader.cs             # Library patrons
│   │   ├── Kiosk.cs              # Self-service terminals
│   │   └── Audit.cs              # Base audit entity
│   └── Enums/
│       └── BookInstanceStatus.cs # Available, CheckedOut, Lost, etc.
│
├── LaurelLibrary.Persistence/     # Data access layer
│   ├── AppDbContext.cs           # Entity Framework context
│   ├── Migrations/               # Database migrations
│   └── Repositories/             # Data access implementations
│
├── LaurelLibrary.Services.Abstractions/  # Service contracts
│   ├── Services/                 # Service interfaces
│   ├── Repositories/             # Repository interfaces
│   └── Dtos/                     # Data transfer objects
│
├── LaurelLibrary.Services/        # Business logic layer
│   └── Services/                 # Service implementations
│       ├── BooksService.cs
│       ├── ReadersService.cs
│       ├── BarcodeService.cs
│       └── BlobStorageService.cs
│
└── LaurelLibrary.UI/              # Presentation layer (ASP.NET Core MVC)
    ├── Controllers/              # MVC controllers
    ├── Views/                    # Razor views
    ├── ViewComponents/           # Reusable UI components
    ├── ViewModels/               # View models
    ├── Areas/Identity/           # Identity scaffolding
    └── wwwroot/                  # Static files
```

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core 9.0
- **Language**: C# 
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity + Microsoft Account OAuth
- **Cloud Services**: Azure Blob Storage
- **UI**: Razor Pages/MVC with Bootstrap
- **Patterns**: Repository Pattern, Service Layer, Clean Architecture

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or SQL Server Express/LocalDB)
- [Azure Storage Account](https://azure.microsoft.com/en-us/services/storage/) (for barcode and logo storage)
- [Azure AD App Registration](https://portal.azure.com/) (for Microsoft authentication - optional)

## 🚦 Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd LaurelLibrary
```

### 2. Configure the Database

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LaurelLibraryDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 3. Configure Azure Storage

Set up Azure Blob Storage for barcode images:

```bash
cd LaurelLibrary.UI
dotnet user-secrets set "ConnectionStrings:AzureStorage" "DefaultEndpointsProtocol=https;AccountName=youraccount;AccountKey=yourkey;EndpointSuffix=core.windows.net"
dotnet user-secrets set "AzureStorage:BarcodeContainerName" "barcodes"
```

See [AZURE_STORAGE_SETUP.md](AZURE_STORAGE_SETUP.md) for detailed instructions.

### 4. Configure Microsoft Authentication (Optional)

If you want to enable Microsoft account login:

```bash
cd LaurelLibrary.UI
dotnet user-secrets set "Authentication:Microsoft:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "YOUR_CLIENT_SECRET"
```

See [MICROSOFT_AUTH_SETUP.md](MICROSOFT_AUTH_SETUP.md) for detailed setup instructions.

### 5. Apply Database Migrations

```bash
cd LaurelLibrary.UI
dotnet ef database update
```

### 6. Run the Application

```bash
dotnet watch
```

Navigate to `https://localhost:5001` in your browser.

## 📦 Database Migrations

The project includes several migrations that implement key features:

- **AddIdentity**: ASP.NET Core Identity tables
- **AddBookRelatedEntities**: Books, Authors, Categories, BookInstances
- **AddCurrentLibrary**: Library management
- **AddReaderEan**: EAN barcode support for readers
- **AddBarcodeImageUrl**: Azure Blob Storage URL for barcode images
- **AddReaderDateOfBirth**: Date of birth field for readers
- **AddLibraryLogoAndDescription**: Library branding support
- **AddKiosks**: Kiosk terminal management
- **AddCheckoutDuration**: Configurable loan periods

To create a new migration:

```bash
cd LaurelLibrary.Persistence
dotnet ef migrations add MigrationName --startup-project ../LaurelLibrary.UI
```

## 🔐 Security Features

- **User secrets** for development (sensitive data never committed to source control)
- **HTTPS** required for all connections
- **ASP.NET Core Identity** with secure password hashing
- **OAuth 2.0** integration for Microsoft accounts
- **Audit trail** for all entity changes (created/modified timestamps and users)

## 📚 Documentation

- [MICROSOFT_AUTH_SETUP.md](MICROSOFT_AUTH_SETUP.md) - Microsoft Account authentication setup
- [AZURE_STORAGE_SETUP.md](AZURE_STORAGE_SETUP.md) - Azure Blob Storage configuration
- Additional implementation notes in various `*_IMPLEMENTATION.md` files

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙋 Support

For questions or issues, please open an issue on the GitHub repository.

## 🎯 Future Enhancements

- [ ] Additional OAuth providers (Google, Facebook)
- [ ] Email notifications for overdue books
- [ ] Advanced search and filtering
- [ ] Book reservation system
- [ ] Mobile app for readers
- [ ] Reporting and analytics dashboard
- [ ] Fine calculation for late returns
- [ ] Integration with ISBN databases for automatic book data entry

## 👨‍💻 Development

### Running Tests

```bash
dotnet test
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

### Code Style

This project follows standard C# coding conventions. Please ensure your code:
- Uses meaningful variable and method names
- Includes XML documentation comments for public APIs
- Follows the repository and service patterns established in the project
- Includes appropriate error handling

---

**Built with ❤️ using ASP.NET Core**
