# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and project files for dependency restoration
COPY LaurelLibrary.sln ./
COPY LaurelLibrary.Functions/LaurelLibrary.Functions.csproj LaurelLibrary.Functions/
COPY LaurelLibrary.Domain/LaurelLibrary.Domain.csproj LaurelLibrary.Domain/
COPY LaurelLibrary.Persistence/LaurelLibrary.Persistence.csproj LaurelLibrary.Persistence/
COPY LaurelLibrary.Services/LaurelLibrary.Services.csproj LaurelLibrary.Services/
COPY LaurelLibrary.Services.Abstractions/LaurelLibrary.Services.Abstractions.csproj LaurelLibrary.Services.Abstractions/
COPY LaurelLibrary.EmailSenderServices/LaurelLibrary.EmailSenderServices.csproj LaurelLibrary.EmailSenderServices/

# Restore dependencies
RUN dotnet restore LaurelLibrary.Functions/LaurelLibrary.Functions.csproj

# Copy source code
COPY LaurelLibrary.Functions/ LaurelLibrary.Functions/
COPY LaurelLibrary.Domain/ LaurelLibrary.Domain/
COPY LaurelLibrary.Persistence/ LaurelLibrary.Persistence/
COPY LaurelLibrary.Services/ LaurelLibrary.Services/
COPY LaurelLibrary.Services.Abstractions/ LaurelLibrary.Services.Abstractions/
COPY LaurelLibrary.EmailSenderServices/ LaurelLibrary.EmailSenderServices/

# Build and publish the application
WORKDIR /src/LaurelLibrary.Functions
RUN dotnet publish -c Release -o /home/site/wwwroot --no-restore

# Use the Azure Functions runtime image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS runtime

# Copy the published function app
COPY --from=build ["/home/site/wwwroot", "/home/site/wwwroot"]

# Set environment variables for Azure Functions
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated