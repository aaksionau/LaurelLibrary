using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LaurelLibrary.UI.Utilities.Configuration;

/// <summary>
/// Custom operation filter for enhancing Swagger documentation
/// </summary>
public class SwaggerOperationFilter : IOperationFilter
{
    /// <summary>
    /// Apply additional documentation to Swagger operations
    /// </summary>
    /// <param name="operation">The OpenAPI operation</param>
    /// <param name="context">The operation filter context</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add response examples for common HTTP status codes
        if (!operation.Responses.ContainsKey("400"))
        {
            operation.Responses.Add(
                "400",
                new OpenApiResponse { Description = "Bad Request - Invalid input parameters" }
            );
        }

        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses.Add(
                "401",
                new OpenApiResponse { Description = "Unauthorized - Authentication required" }
            );
        }

        if (!operation.Responses.ContainsKey("403"))
        {
            operation.Responses.Add(
                "403",
                new OpenApiResponse { Description = "Forbidden - Insufficient permissions" }
            );
        }

        if (!operation.Responses.ContainsKey("404"))
        {
            operation.Responses.Add(
                "404",
                new OpenApiResponse { Description = "Not Found - Requested resource not found" }
            );
        }

        if (!operation.Responses.ContainsKey("500"))
        {
            operation.Responses.Add(
                "500",
                new OpenApiResponse
                {
                    Description =
                        "Internal Server Error - An error occurred processing the request",
                }
            );
        }
    }
}
