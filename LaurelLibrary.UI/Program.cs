using LaurelLibrary.UI.Utilities.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Get connection string
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add all application services using the utility configuration classes
await builder.Services.AddApplicationServices(builder.Configuration, connectionString);

var app = builder.Build();

// Configure the application pipeline using the utility configuration classes
app.ConfigureApplication();

app.Run();
