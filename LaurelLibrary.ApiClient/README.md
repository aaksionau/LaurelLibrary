# LaurelLibrary API Client

This project contains the auto-generated API client for the MyLibrarian API, created using [Kiota](https://learn.microsoft.com/openapi/kiota/overview).

## Generated Files

The client code is located in the `Generated/` directory and includes:
- Models for all API request/response DTOs
- Fluent API for all endpoints
- Automatic serialization/deserialization

## Usage Example

```csharp
using LaurelLibrary.ApiClient;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

// Create the HTTP client
var httpClient = new HttpClient();

// Create the request adapter with the base URL
var adapter = new HttpClientRequestAdapter(
    new AnonymousAuthenticationProvider(), 
    httpClient: httpClient
);
adapter.BaseUrl = "http://localhost:5083";

// Create the API client
var client = new ApiClient(adapter);

// Example: Search for authors
var authors = await client.Api.Authors.Search.GetAsync(config => 
{
    config.QueryParameters.Query = "tolkien";
});

// Example: Get books
var books = await client.Api.Books.GetAsync(config => 
{
    config.QueryParameters.PageNumber = 1;
    config.QueryParameters.PageSize = 10;
});

// Example: Get a specific book by ID
var book = await client.Api.Books[bookId].GetAsync();
```

## Regenerating the Client

When the API changes, regenerate the client:

```bash
cd LaurelLibrary.ApiClient
kiota generate -l CSharp -d http://localhost:5083/api/docs/v1/swagger.json -o ./Generated -n LaurelLibrary.ApiClient --clean-output
```

## Authentication

For APIs requiring authentication, replace `AnonymousAuthenticationProvider` with an appropriate provider:

```csharp
// For Azure AD
var authProvider = new AzureIdentityAuthenticationProvider(
    new DefaultAzureCredential()
);

// For Bearer token
var authProvider = new BaseBearerTokenAuthenticationProvider(
    new TokenProvider()
);
```

## Dependencies

- Microsoft.Kiota.Bundle (1.20.1)
- Microsoft.Kiota.Authentication.Azure (1.20.1)
