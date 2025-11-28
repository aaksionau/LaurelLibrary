# Use the official .NET 10 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution file and project files for dependency restoration
COPY LaurelLibrary.sln ./
COPY LaurelLibrary.UI/LaurelLibrary.UI.csproj LaurelLibrary.UI/
COPY LaurelLibrary.Domain/LaurelLibrary.Domain.csproj LaurelLibrary.Domain/
COPY LaurelLibrary.Persistence/LaurelLibrary.Persistence.csproj LaurelLibrary.Persistence/
COPY LaurelLibrary.Services/LaurelLibrary.Services.csproj LaurelLibrary.Services/
COPY LaurelLibrary.Services.Abstractions/LaurelLibrary.Services.Abstractions.csproj LaurelLibrary.Services.Abstractions/
COPY LaurelLibrary.EmailSenderServices/LaurelLibrary.EmailSenderServices.csproj LaurelLibrary.EmailSenderServices/

# Restore dependencies
RUN dotnet restore LaurelLibrary.UI/LaurelLibrary.UI.csproj

# Copy source code
COPY LaurelLibrary.UI/ LaurelLibrary.UI/
COPY LaurelLibrary.Domain/ LaurelLibrary.Domain/
COPY LaurelLibrary.Persistence/ LaurelLibrary.Persistence/
COPY LaurelLibrary.Services/ LaurelLibrary.Services/
COPY LaurelLibrary.Services.Abstractions/ LaurelLibrary.Services.Abstractions/
COPY LaurelLibrary.EmailSenderServices/ LaurelLibrary.EmailSenderServices/

# Build and publish the application
WORKDIR /src/LaurelLibrary.UI
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the official .NET 10 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' --shell /bin/sh --uid 1001 appuser

# Copy published application
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "LaurelLibrary.UI.dll"]