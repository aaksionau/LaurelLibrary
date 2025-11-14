using System.Net;
using System.Text;
using System.Text.Json;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace LaurelLibrary.Tests.Services;

public class PlanningCenterServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IReadersService> _readersServiceMock;
    private readonly Mock<IAuthenticationService> _authenticationServiceMock;
    private readonly Mock<ILibrariesRepository> _librariesRepositoryMock;
    private readonly Mock<IReadersRepository> _readersRepositoryMock;
    private readonly Mock<ILogger<PlanningCenterService>> _loggerMock;
    private readonly HttpClient _httpClient;
    private readonly PlanningCenterService _planningCenterService;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly Guid _testLibraryId;

    public PlanningCenterServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _readersServiceMock = new Mock<IReadersService>();
        _authenticationServiceMock = new Mock<IAuthenticationService>();
        _librariesRepositoryMock = new Mock<ILibrariesRepository>();
        _readersRepositoryMock = new Mock<IReadersRepository>();
        _loggerMock = new Mock<ILogger<PlanningCenterService>>();

        _testLibraryId = Guid.NewGuid();

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.planningcenteronline.com/"),
        };

        _planningCenterService = new PlanningCenterService(
            _httpClient,
            _readersServiceMock.Object,
            _authenticationServiceMock.Object,
            _librariesRepositoryMock.Object,
            _readersRepositoryMock.Object,
            _loggerMock.Object
        );

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
    }

    #region TestConnectionAsync Tests

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = _testLibraryId,
        };
        var library = new Library
        {
            LibraryId = _testLibraryId,
            Name = "Test Library",
            Alias = "test-library",
            PlanningCenterApplicationId = "test-app-id",
            PlanningCenterSecret = "test-secret",
        };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        _librariesRepositoryMock.Setup(x => x.GetByIdAsync(_testLibraryId)).ReturnsAsync(library);

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        // Act
        var result = await _planningCenterService.TestConnectionAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TestConnectionAsync_UnauthorizedResponse_ReturnsFalse()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = _testLibraryId,
        };
        var library = new Library
        {
            LibraryId = _testLibraryId,
            Name = "Test Library",
            Alias = "test-library",
            PlanningCenterApplicationId = "test-app-id",
            PlanningCenterSecret = "test-secret",
        };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        _librariesRepositoryMock.Setup(x => x.GetByIdAsync(_testLibraryId)).ReturnsAsync(library);

        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        // Act
        var result = await _planningCenterService.TestConnectionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TestConnectionAsync_NoCurrentLibrary_ThrowsInvalidOperationException()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = null,
        };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        // Act
        var result = await _planningCenterService.TestConnectionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TestConnectionAsync_MissingPlanningCenterCredentials_ThrowsInvalidOperationException()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = _testLibraryId,
        };
        var library = new Library
        {
            LibraryId = _testLibraryId,
            Name = "Test Library",
            Alias = "test-library",
            PlanningCenterApplicationId = null,
            PlanningCenterSecret = null,
        };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        _librariesRepositoryMock.Setup(x => x.GetByIdAsync(_testLibraryId)).ReturnsAsync(library);

        // Act
        var result = await _planningCenterService.TestConnectionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TestConnectionAsync_HttpRequestException_ReturnsFalse()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = _testLibraryId,
        };
        var library = new Library
        {
            LibraryId = _testLibraryId,
            Name = "Test Library",
            Alias = "test-library",
            PlanningCenterApplicationId = "test-app-id",
            PlanningCenterSecret = "test-secret",
        };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        _librariesRepositoryMock.Setup(x => x.GetByIdAsync(_testLibraryId)).ReturnsAsync(library);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _planningCenterService.TestConnectionAsync();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetAllPeopleAsync Tests

    [Fact]
    public async Task GetAllPeopleAsync_SuccessfulResponse_ReturnsSummary()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = _testLibraryId,
        };
        var library = new Library
        {
            LibraryId = _testLibraryId,
            Name = "Test Library",
            Alias = "test-library",
            PlanningCenterApplicationId = "test-app-id",
            PlanningCenterSecret = "test-secret",
        };

        var existingEmails = new HashSet<string> { "existing@test.com" };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        _librariesRepositoryMock.Setup(x => x.GetByIdAsync(_testLibraryId)).ReturnsAsync(library);

        _readersRepositoryMock
            .Setup(x => x.GetAllEmailsAsync(_testLibraryId))
            .ReturnsAsync(existingEmails);

        var planningCenterResponse = CreateTestPlanningCenterResponse();
        var responseContent = JsonSerializer.Serialize(
            planningCenterResponse,
            _jsonSerializerOptions
        );
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        // Act
        var result = await _planningCenterService.GetAllPeopleAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.ActiveCount);
        Assert.Equal(0, result.InactiveCount);
        Assert.Equal(2, result.People.Count);
        Assert.Single(result.PeopleNeedingAttention);
    }

    [Fact]
    public async Task GetAllPeopleAsync_MultiplePages_ReturnsAllData()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = _testLibraryId,
        };
        var library = new Library
        {
            LibraryId = _testLibraryId,
            Name = "Test Library",
            Alias = "test-library",
            PlanningCenterApplicationId = "test-app-id",
            PlanningCenterSecret = "test-secret",
        };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        _librariesRepositoryMock.Setup(x => x.GetByIdAsync(_testLibraryId)).ReturnsAsync(library);

        _readersRepositoryMock
            .Setup(x => x.GetAllEmailsAsync(_testLibraryId))
            .ReturnsAsync(new HashSet<string>());

        // First page response
        var firstPageResponse = CreateTestPlanningCenterResponseWithLinks();

        // Second page response
        var secondPageResponse = CreateTestPlanningCenterResponse();
        secondPageResponse = ModifyResponseForSecondPage(secondPageResponse);

        var responses = new Queue<HttpResponseMessage>();
        responses.Enqueue(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(firstPageResponse, _jsonSerializerOptions),
                    Encoding.UTF8,
                    "application/json"
                ),
            }
        );
        responses.Enqueue(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(secondPageResponse, _jsonSerializerOptions),
                    Encoding.UTF8,
                    "application/json"
                ),
            }
        );

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns(() => Task.FromResult(responses.Dequeue()));

        // Act
        var result = await _planningCenterService.GetAllPeopleAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(4, result.People.Count);
    }

    [Fact]
    public async Task GetAllPeopleAsync_NoCurrentLibrary_ThrowsInvalidOperationException()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = null,
        };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _planningCenterService.GetAllPeopleAsync()
        );
    }

    #endregion

    #region ImportPeopleAsReadersAsync Tests

    [Fact]
    public async Task ImportPeopleAsReadersAsync_ValidPeople_ReturnsSuccessResult()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = _testLibraryId,
        };
        var peopleToImport = new List<PlanningCenterPersonDto>
        {
            new()
            {
                Id = "1",
                FirstName = "John",
                LastName = "Doe",
                PrimaryEmail = "john@test.com",
                StreetAddress = "123 Main St",
                City = "Anytown",
                State = "CA",
                Zip = "12345",
            },
            new()
            {
                Id = "2",
                FirstName = "Jane",
                LastName = "Smith",
                PrimaryEmail = "jane@test.com",
            },
        };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        _readersRepositoryMock
            .Setup(x => x.GetAllEmailsAsync(_testLibraryId))
            .ReturnsAsync(new HashSet<string>());

        _readersServiceMock
            .Setup(x => x.CreateOrUpdateReaderAsync(It.IsAny<ReaderDto>()))
            .ReturnsAsync(false); // false indicates creation, not update

        // Act
        var result = await _planningCenterService.ImportPeopleAsReadersAsync(peopleToImport);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalProcessed);
        Assert.Equal(2, result.SuccessfullyCreated);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Skipped);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ImportPeopleAsReadersAsync_PeopleWithoutValidEmail_SkipsThem()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = _testLibraryId,
        };
        var peopleToImport = new List<PlanningCenterPersonDto>
        {
            new()
            {
                Id = "1",
                FirstName = "John",
                LastName = "Doe",
                PrimaryEmail = null, // No email
            },
            new()
            {
                Id = "2",
                FirstName = "Jane",
                LastName = "Smith",
                PrimaryEmail = "invalid-email", // Invalid email
            },
            new()
            {
                Id = "3",
                FirstName = "Bob",
                LastName = "Johnson",
                PrimaryEmail = "bob@test.com", // Valid email
            },
        };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        _readersRepositoryMock
            .Setup(x => x.GetAllEmailsAsync(_testLibraryId))
            .ReturnsAsync(new HashSet<string>());

        _readersServiceMock
            .Setup(x => x.CreateOrUpdateReaderAsync(It.IsAny<ReaderDto>()))
            .ReturnsAsync(false);

        // Act
        var result = await _planningCenterService.ImportPeopleAsReadersAsync(peopleToImport);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalProcessed);
        Assert.Equal(1, result.SuccessfullyCreated);
        Assert.Equal(0, result.Updated);
        Assert.Equal(2, result.Skipped);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task ImportPeopleAsReadersAsync_DuplicateEmails_SkipsThem()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = _testLibraryId,
        };
        var existingEmails = new HashSet<string> { "existing@test.com" };
        var peopleToImport = new List<PlanningCenterPersonDto>
        {
            new()
            {
                Id = "1",
                FirstName = "John",
                LastName = "Doe",
                PrimaryEmail = "existing@test.com", // Duplicate email
            },
            new()
            {
                Id = "2",
                FirstName = "Jane",
                LastName = "Smith",
                PrimaryEmail = "new@test.com", // New email
            },
        };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        _readersRepositoryMock
            .Setup(x => x.GetAllEmailsAsync(_testLibraryId))
            .ReturnsAsync(existingEmails);

        _readersServiceMock
            .Setup(x => x.CreateOrUpdateReaderAsync(It.IsAny<ReaderDto>()))
            .ReturnsAsync(false);

        // Act
        var result = await _planningCenterService.ImportPeopleAsReadersAsync(peopleToImport);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalProcessed);
        Assert.Equal(1, result.SuccessfullyCreated);
        Assert.Equal(0, result.Updated);
        Assert.Equal(1, result.Skipped);
        Assert.Single(result.Errors);
        Assert.Contains("already exists", result.Errors[0]);
    }

    [Fact]
    public async Task ImportPeopleAsReadersAsync_UpdateExistingReader_ReturnsUpdateResult()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = _testLibraryId,
        };
        var peopleToImport = new List<PlanningCenterPersonDto>
        {
            new()
            {
                Id = "1",
                FirstName = "John",
                LastName = "Doe",
                PrimaryEmail = "john@test.com",
            },
        };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        _readersRepositoryMock
            .Setup(x => x.GetAllEmailsAsync(_testLibraryId))
            .ReturnsAsync(new HashSet<string>());

        _readersServiceMock
            .Setup(x => x.CreateOrUpdateReaderAsync(It.IsAny<ReaderDto>()))
            .ReturnsAsync(true); // true indicates update

        // Act
        var result = await _planningCenterService.ImportPeopleAsReadersAsync(peopleToImport);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(0, result.SuccessfullyCreated);
        Assert.Equal(1, result.Updated);
        Assert.Equal(0, result.Skipped);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ImportPeopleAsReadersAsync_ServiceThrowsException_AddsError()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = _testLibraryId,
        };
        var peopleToImport = new List<PlanningCenterPersonDto>
        {
            new()
            {
                Id = "1",
                FirstName = "John",
                LastName = "Doe",
                PrimaryEmail = "john@test.com",
            },
        };

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        _readersRepositoryMock
            .Setup(x => x.GetAllEmailsAsync(_testLibraryId))
            .ReturnsAsync(new HashSet<string>());

        _readersServiceMock
            .Setup(x => x.CreateOrUpdateReaderAsync(It.IsAny<ReaderDto>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _planningCenterService.ImportPeopleAsReadersAsync(peopleToImport);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(0, result.SuccessfullyCreated);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Skipped);
        Assert.Single(result.Errors);
        Assert.Contains("Database error", result.Errors[0]);
    }

    [Fact]
    public async Task ImportPeopleAsReadersAsync_NoCurrentLibrary_ThrowsInvalidOperationException()
    {
        // Arrange
        var currentUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = null,
        };
        var peopleToImport = new List<PlanningCenterPersonDto>();

        _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _planningCenterService.ImportPeopleAsReadersAsync(peopleToImport)
        );
    }

    #endregion

    #region Helper Methods

    private static TestPlanningCenterResponse CreateTestPlanningCenterResponse()
    {
        return new TestPlanningCenterResponse
        {
            Data = new TestPlanningCenterPerson[]
            {
                new()
                {
                    Id = "1",
                    Type = "Person",
                    Attributes = new TestPlanningCenterPersonAttributes
                    {
                        FirstName = "John",
                        LastName = "Doe",
                        Birthdate = "1990-01-01",
                        Status = "active",
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        UpdatedAt = DateTime.UtcNow,
                    },
                    Relationships = new TestPlanningCenterPersonRelationships
                    {
                        Emails = new TestPlanningCenterRelationship
                        {
                            Data = new[]
                            {
                                new TestPlanningCenterRelationshipData { Id = "1", Type = "Email" },
                            },
                        },
                        Addresses = new TestPlanningCenterRelationship
                        {
                            Data = new[]
                            {
                                new TestPlanningCenterRelationshipData
                                {
                                    Id = "1",
                                    Type = "Address",
                                },
                            },
                        },
                    },
                },
                new()
                {
                    Id = "2",
                    Type = "Person",
                    Attributes = new TestPlanningCenterPersonAttributes
                    {
                        FirstName = "Jane",
                        LastName = "Smith",
                        Birthdate = "1985-05-15",
                        Status = "active",
                        CreatedAt = DateTime.UtcNow.AddDays(-60),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    },
                    Relationships = new TestPlanningCenterPersonRelationships
                    {
                        Emails = new TestPlanningCenterRelationship
                        {
                            Data = new[]
                            {
                                new TestPlanningCenterRelationshipData { Id = "2", Type = "Email" },
                            },
                        },
                        Addresses = new TestPlanningCenterRelationship
                        {
                            Data = new[]
                            {
                                new TestPlanningCenterRelationshipData
                                {
                                    Id = "2",
                                    Type = "Address",
                                },
                            },
                        },
                    },
                },
            },
            Included = new TestPlanningCenterIncluded[]
            {
                new()
                {
                    Id = "1",
                    Type = "Email",
                    Attributes = new TestPlanningCenterIncludedAttributes
                    {
                        Address = "john@test.com",
                        Primary = true,
                    },
                },
                new()
                {
                    Id = "2",
                    Type = "Email",
                    Attributes = new TestPlanningCenterIncludedAttributes
                    {
                        Address = "existing@test.com",
                        Primary = true,
                    },
                },
                new()
                {
                    Id = "1",
                    Type = "Address",
                    Attributes = new TestPlanningCenterIncludedAttributes
                    {
                        Street = "123 Main St",
                        City = "Anytown",
                        State = "CA",
                        Zip = "12345",
                        Primary = true,
                    },
                },
                new()
                {
                    Id = "2",
                    Type = "Address",
                    Attributes = new TestPlanningCenterIncludedAttributes
                    {
                        Street = "456 Oak Ave",
                        City = "Somewhere",
                        State = "NY",
                        Zip = "67890",
                        Primary = true,
                    },
                },
            },
            Links = null,
        };
    }

    private static TestPlanningCenterResponse CreateTestPlanningCenterResponseWithLinks()
    {
        var response = CreateTestPlanningCenterResponse();
        response.Links = new TestPlanningCenterLinks
        {
            Next = "people/v2/people?page=2&per_page=100&include=emails,addresses",
        };
        return response;
    }

    private static TestPlanningCenterResponse ModifyResponseForSecondPage(object originalResponse)
    {
        return new TestPlanningCenterResponse
        {
            Data = new TestPlanningCenterPerson[]
            {
                new()
                {
                    Id = "3",
                    Type = "Person",
                    Attributes = new TestPlanningCenterPersonAttributes
                    {
                        FirstName = "Jane",
                        LastName = "Updated",
                        Birthdate = "1990-01-01",
                        Status = "active",
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        UpdatedAt = DateTime.UtcNow,
                    },
                    Relationships = new TestPlanningCenterPersonRelationships
                    {
                        Emails = new TestPlanningCenterRelationship
                        {
                            Data = new[]
                            {
                                new TestPlanningCenterRelationshipData { Id = "3", Type = "Email" },
                            },
                        },
                        Addresses = new TestPlanningCenterRelationship
                        {
                            Data = new[]
                            {
                                new TestPlanningCenterRelationshipData
                                {
                                    Id = "3",
                                    Type = "Address",
                                },
                            },
                        },
                    },
                },
                new()
                {
                    Id = "4",
                    Type = "Person",
                    Attributes = new TestPlanningCenterPersonAttributes
                    {
                        FirstName = "Bob",
                        LastName = "Updated",
                        Birthdate = "1985-05-15",
                        Status = "active",
                        CreatedAt = DateTime.UtcNow.AddDays(-60),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    },
                    Relationships = new TestPlanningCenterPersonRelationships
                    {
                        Emails = new TestPlanningCenterRelationship
                        {
                            Data = new[]
                            {
                                new TestPlanningCenterRelationshipData { Id = "4", Type = "Email" },
                            },
                        },
                        Addresses = new TestPlanningCenterRelationship
                        {
                            Data = new[]
                            {
                                new TestPlanningCenterRelationshipData
                                {
                                    Id = "4",
                                    Type = "Address",
                                },
                            },
                        },
                    },
                },
            },
            Included = new TestPlanningCenterIncluded[]
            {
                new()
                {
                    Id = "3",
                    Type = "Email",
                    Attributes = new TestPlanningCenterIncludedAttributes
                    {
                        Address = "jane2@test.com",
                        Primary = true,
                    },
                },
                new()
                {
                    Id = "4",
                    Type = "Email",
                    Attributes = new TestPlanningCenterIncludedAttributes
                    {
                        Address = "bob2@test.com",
                        Primary = true,
                    },
                },
                new()
                {
                    Id = "3",
                    Type = "Address",
                    Attributes = new TestPlanningCenterIncludedAttributes
                    {
                        Street = "789 Pine St",
                        City = "Newtown",
                        State = "TX",
                        Zip = "54321",
                        Primary = true,
                    },
                },
                new()
                {
                    Id = "4",
                    Type = "Address",
                    Attributes = new TestPlanningCenterIncludedAttributes
                    {
                        Street = "101 Elm St",
                        City = "Oldtown",
                        State = "FL",
                        Zip = "98765",
                        Primary = true,
                    },
                },
            },
            Links = null,
        };
    }

    #endregion

    #region Test Helper Classes

    internal class TestPlanningCenterResponse
    {
        public TestPlanningCenterPerson[]? Data { get; set; }
        public TestPlanningCenterIncluded[]? Included { get; set; }
        public TestPlanningCenterLinks? Links { get; set; }
    }

    internal class TestPlanningCenterPerson
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public TestPlanningCenterPersonAttributes Attributes { get; set; } = new();
        public TestPlanningCenterPersonRelationships? Relationships { get; set; }
    }

    internal class TestPlanningCenterPersonAttributes
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Birthdate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    internal class TestPlanningCenterPersonRelationships
    {
        public TestPlanningCenterRelationship? Emails { get; set; }
        public TestPlanningCenterRelationship? Addresses { get; set; }
    }

    internal class TestPlanningCenterRelationship
    {
        public TestPlanningCenterRelationshipData[]? Data { get; set; }
    }

    internal class TestPlanningCenterRelationshipData
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    internal class TestPlanningCenterIncluded
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public TestPlanningCenterIncludedAttributes? Attributes { get; set; }
    }

    internal class TestPlanningCenterIncludedAttributes
    {
        public string? Address { get; set; }
        public bool Primary { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
    }

    internal class TestPlanningCenterLinks
    {
        public string? Next { get; set; }
        public string? Prev { get; set; }
    }

    #endregion

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
