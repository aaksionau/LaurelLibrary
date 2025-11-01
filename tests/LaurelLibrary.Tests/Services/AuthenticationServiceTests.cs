using System;
using System.Security.Claims;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<UserManager<AppUser>> userManagerMock;
        private readonly Mock<IHttpContextAccessor> httpContextAccessorMock;
        private readonly AuthenticationService authenticationService;

        public AuthenticationServiceTests()
        {
            var store = new Mock<IUserStore<AppUser>>();
            userManagerMock = new Mock<UserManager<AppUser>>(
                store.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );
            httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            authenticationService = new AuthenticationService(
                userManagerMock.Object,
                httpContextAccessorMock.Object
            );
        }

        [Fact]
        public async Task GetAppUserAsync_ReturnsUser_WhenAuthenticated()
        {
            // Arrange
            var user = new AppUser { UserName = "testuser" };
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "mock")
            );
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            userManagerMock.Setup(x => x.GetUserAsync(claimsPrincipal)).ReturnsAsync(user);

            // Act
            var result = await authenticationService.GetAppUserAsync();

            // Assert
            Assert.Equal(user, result);
        }

        [Fact]
        public async Task GetAppUserAsync_ThrowsException_WhenNotAuthenticated()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => authenticationService.GetAppUserAsync());
        }

        [Fact]
        public async Task GetAppUserAsync_ThrowsException_WhenUserNotFound()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "mock")
            );
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            userManagerMock.Setup(x => x.GetUserAsync(claimsPrincipal)).ReturnsAsync((AppUser)null);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => authenticationService.GetAppUserAsync());
        }
    }
}
