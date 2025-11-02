using LaurelLibrary.Domain.Exceptions;

namespace LaurelLibrary.Tests.Exceptions
{
    public class SubscriptionUpgradeRequiredExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_SetsCorrectProperties()
        {
            // Arrange
            var message = "Test subscription upgrade message";

            // Act
            var exception = new SubscriptionUpgradeRequiredException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal("/Subscription", exception.RedirectUrl);
            Assert.Null(exception.Feature);
            Assert.Null(exception.CurrentTier);
            Assert.Null(exception.RecommendedTier);
        }

        [Fact]
        public void Constructor_WithFullDetails_SetsAllProperties()
        {
            // Arrange
            var message = "Cannot add more books";
            var feature = "Additional Books";
            var currentTier = "Free";
            var recommendedTier = "Pro";
            var redirectUrl = "/CustomSubscription";

            // Act
            var exception = new SubscriptionUpgradeRequiredException(
                message,
                feature,
                currentTier,
                recommendedTier,
                redirectUrl
            );

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(feature, exception.Feature);
            Assert.Equal(currentTier, exception.CurrentTier);
            Assert.Equal(recommendedTier, exception.RecommendedTier);
            Assert.Equal(redirectUrl, exception.RedirectUrl);
        }

        [Fact]
        public void Constructor_WithInnerException_SetsCorrectProperties()
        {
            // Arrange
            var message = "Test message";
            var innerException = new InvalidOperationException("Inner exception");
            var redirectUrl = "/Test";

            // Act
            var exception = new SubscriptionUpgradeRequiredException(
                message,
                innerException,
                redirectUrl
            );

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(redirectUrl, exception.RedirectUrl);
        }
    }
}
