using System.Reflection;
using Microsoft.OpenApi.Models;

namespace LaurelLibrary.UI.Utilities.Configuration;

/// <summary>
/// Configuration for Swagger/OpenAPI documentation
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Adds Swagger services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Basic API info
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Version = "v1",
                    Title = "MyLibrarian API",
                    Description =
                        "A comprehensive library management system API for managing books, readers, authors, categories, and library operations.",
                    Contact = new OpenApiContact
                    {
                        Name = "MyLibrarian Support",
                        Email = "support@mylibrarian.org",
                    },
                }
            );

            // Enable XML documentation comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Enable annotations for better documentation
            options.EnableAnnotations();

            // Add JWT Bearer authentication support
            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Description =
                        "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                }
            );

            options.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer",
                            },
                        },
                        Array.Empty<string>()
                    },
                }
            );

            // Group endpoints by controller
            options.TagActionsBy(api =>
            {
                if (api.GroupName != null)
                {
                    return new[] { api.GroupName };
                }

                var controllerName = api.ActionDescriptor.RouteValues["controller"];
                return new[] { controllerName ?? "Default" };
            });

            // Custom operation filters for better documentation
            options.OperationFilter<SwaggerOperationFilter>();
        });

        return services;
    }

    /// <summary>
    /// Configures Swagger UI for the application
    /// </summary>
    /// <param name="app">The web application</param>
    public static void UseSwaggerDocumentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.UseSwagger(options =>
            {
                options.RouteTemplate = "api/docs/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/api/docs/v1/swagger.json", "Laurel Library API v1");
                options.RoutePrefix = "api/docs";
                options.DocumentTitle = "Laurel Library API Documentation";
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();
                options.EnableValidator();
                options.ShowExtensions();
                options.ShowCommonExtensions();
            });
        }
    }
}
