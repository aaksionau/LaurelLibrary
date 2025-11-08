# LaurelLibrary

A modern library management system built with ASP.NET Core, designed to help libraries manage their book collections, readers, and check-out operations through both web and kiosk interfaces.

## 🚀 Features

### For Libraries (Administrators)

#### Library Management
- **Multi-library support**: Manage multiple library branches within a single application
- **Book cataloging**: Comprehensive book information including titles, authors, categories, publishers, synopses, ISBN, and cover images
- **Book instances**: Track individual physical copies of books with unique barcodes and availability status (Available, Borrowed, Reserved, Lost, Damaged)
- **Inventory management**: Monitor book availability and location across libraries
- **Batch book imports**: Import multiple books from ISBN databases with automated metadata retrieval
- **AI-powered age classification**: Automatically categorize books by appropriate age groups (Premium feature)

#### Reader Management
- **Reader registration**: Complete patron management with contact information and demographics
- **EAN barcode generation**: Automatically generate unique EAN-13 barcodes for readers
- **Barcode image storage**: Store barcode images in Azure Blob Storage for scalability
- **Reader activity tracking**: Monitor borrowing history and current checkouts
- **Multi-library reader access**: Readers can be registered across multiple library branches

#### Check-out & Returns System
- **Configurable loan periods**: Set custom checkout durations per library (default: 14 days)
- **Book instance tracking**: Monitor which specific copy is checked out to which reader
- **Complete audit trail**: Track all checkout and return actions with timestamps
- **Automated due date reminders**: Email notifications sent to readers before books are due
- **Checkout confirmations**: Email receipts sent immediately after successful checkouts

#### Advanced Search & Discovery
- **Traditional search**: Filter by title, author, category, ISBN, and publication details
- **Semantic search**: Natural language search powered by AI (e.g., "fantasy books for teenagers")
- **Category browsing**: Organized book discovery by subject areas
- **Availability filtering**: Find only available books for immediate checkout

#### Analytics & Reporting
- **Reader activity reports**: View borrowing patterns and popular books
- **Library usage statistics**: Monitor checkout frequency and book circulation
- **Action history**: Detailed logs of all reader and book activities

### For Readers (Library Patrons)

#### Self-Service Kiosk Interface
- **Touch-friendly design**: Optimized for tablet and touch screen devices
- **Browser fingerprinting**: Secure kiosk identification without passwords
- **Multi-language support**: User interface available in multiple languages

#### Book Discovery
- **Intuitive search**: Find books by title, author, or subject
- **Smart recommendations**: AI-powered semantic search for natural language queries
- **Browse by category**: Discover books organized by subject and age group
- **Real-time availability**: See which books are immediately available for checkout

#### Self-Checkout Process
- **Barcode scanning**: Scan reader EAN barcode for identification
- **Multiple book checkout**: Check out several books in a single transaction
- **Due date display**: Clear visibility of return dates for each book
- **Email confirmation**: Immediate checkout receipt with book details and due dates

#### Self-Return Process
- **Easy returns**: Return books by scanning ISBN barcodes
- **Multiple book returns**: Process several returns in one session
- **Instant confirmation**: Immediate feedback when books are successfully returned
- **Return receipt**: Email confirmation of returned books

#### Account Management
- **Borrowing history**: View complete history of checked-out and returned books
- **Current checkouts**: See all currently borrowed books and their due dates
- **Due date reminders**: Receive email notifications before books are due
- **Library information**: Access library contact details and policies

### Subscription-Based Features

#### Library Lover Plan ($11.99/month)
- Up to 1,000 books and 100 readers
- Advanced semantic search capabilities
- Email notifications and barcode generation
- Mobile app checkout (planned)
- Standard support

#### Bibliotheca Pro Plan ($14.99/month)
- Unlimited books, readers, and library branches
- AI-powered age classification for books
- Multi-library support and management
- Priority customer support
- Advanced analytics and reporting

### Kiosk Mode
- **Self-service kiosks**: Browser-based kiosk interface for library patrons
- **Browser fingerprinting**: Identify and manage kiosk terminals securely
- **Location tracking**: Associate kiosks with specific physical locations
- **Offline capability**: Continue basic operations during network interruptions

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

- [ ] Mobile app for readers (iOS/Android)
- [ ] Advanced reporting and analytics dashboard
- [ ] Book reservation system with hold queues
- [ ] Fine calculation for late returns
- [ ] Integration with additional ISBN databases
- [ ] Multi-language support for kiosk interface
- [ ] Offline mode for network interruptions
- [ ] Library events and program management
- [ ] Integration with library card systems
- [ ] Advanced user roles and permissions
- [ ] Book recommendation engine based on reading history
- [ ] Social features (book reviews, ratings, reading lists)

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
