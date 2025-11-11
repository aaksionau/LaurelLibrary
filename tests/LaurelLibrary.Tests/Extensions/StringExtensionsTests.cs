using LaurelLibrary.Services.Abstractions.Extensions;

namespace LaurelLibrary.Tests.Extensions;

public class StringExtensionsTests
{
    public class NormalizeIsbnTests
    {
        [Fact]
        public void NormalizeIsbn_WithNullInput_ReturnsEmptyString()
        {
            // Arrange
            string? isbn = null;

            // Act
            var result = isbn.NormalizeIsbn();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void NormalizeIsbn_WithEmptyString_ReturnsEmptyString()
        {
            // Arrange
            var isbn = string.Empty;

            // Act
            var result = isbn.NormalizeIsbn();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void NormalizeIsbn_WithWhitespaceOnly_ReturnsEmptyString()
        {
            // Arrange
            var isbn = "   \t\n  ";

            // Act
            var result = isbn.NormalizeIsbn();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData("123456789", "9781234567897")] // 9 digits -> complete ISBN-10 -> ISBN-13
        [InlineData("987654321", "9789876543217")] // 9 digits -> complete ISBN-10 -> ISBN-13
        public void NormalizeIsbn_WithNineDigits_CalculatesChecksumAndConvertsToIsbn13(
            string input,
            string expected
        )
        {
            // Act
            var result = input.NormalizeIsbn();

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("0123456789", "9780123456786")] // Valid ISBN-10 -> ISBN-13
        [InlineData("123456789X", "9781234567897")] // ISBN-10 with X checksum -> ISBN-13
        [InlineData("0-123-45678-9", "9780123456786")] // Formatted ISBN-10 -> ISBN-13
        [InlineData("978-0-123456-78-6", "9780123456786")] // Already ISBN-13, remove formatting
        public void NormalizeIsbn_WithValidIsbn_ReturnsCorrectIsbn13(string input, string expected)
        {
            // Act
            var result = input.NormalizeIsbn();

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("9780123456786")] // Valid ISBN-13
        [InlineData("9781234567897")] // Another valid ISBN-13
        [InlineData("9789876543210")] // Another valid ISBN-13
        public void NormalizeIsbn_WithThirteenDigits_ReturnsAsIs(string input)
        {
            // Act
            var result = input.NormalizeIsbn();

            // Assert
            Assert.Equal(input, result);
        }

        [Theory]
        [InlineData("978-0-123456-78-6", "9780123456786")] // Formatted ISBN-13
        [InlineData("978 0 123456 78 6", "9780123456786")] // Spaced ISBN-13
        [InlineData("978.0.123456.78.6", "9780123456786")] // Dot-separated ISBN-13
        [InlineData("ISBN: 978-0-123456-78-6", "9780123456786")] // With prefix
        public void NormalizeIsbn_WithFormattedIsbn13_RemovesFormattingAndReturnsDigitsOnly(
            string input,
            string expected
        )
        {
            // Act
            var result = input.NormalizeIsbn();

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("0-123-45678-9", "9780123456786")] // Hyphenated ISBN-10
        [InlineData("0 123 45678 9", "9780123456786")] // Spaced ISBN-10
        public void NormalizeIsbn_WithFormattedIsbn10_RemovesFormattingAndConvertsToIsbn13(
            string input,
            string expected
        )
        {
            // Act
            var result = input.NormalizeIsbn();

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("12345", "12345")] // Too short
        [InlineData("12345678901234567890", "12345678901234567890")] // Too long
        [InlineData("abcdefghij", "")] // Invalid characters (except no digits)
        public void NormalizeIsbn_WithInvalidLength_ReturnsDigitsOnly(string input, string expected)
        {
            // Act
            var result = input.NormalizeIsbn();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void NormalizeIsbn_WithRealWorldExample_HandlesCorrectly()
        {
            // Real ISBN examples
            var isbn10 = "0-262-03384-8"; // Real ISBN-10
            var expectedIsbn13 = "9780262033848";

            var result = isbn10.NormalizeIsbn();

            Assert.Equal(expectedIsbn13, result);
        }

        [Theory]
        [InlineData("123456789012", "123456789012")] // 12 digits - invalid length
        [InlineData("12345678901234", "12345678901234")] // 14 digits - invalid length
        public void NormalizeIsbn_WithInvalidLengths_ReturnsAsNormalized(
            string input,
            string expected
        )
        {
            // Act
            var result = input.NormalizeIsbn();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void NormalizeIsbn_Isbn10ChecksumCalculation_WorksCorrectly()
        {
            // Test known ISBN-10 checksum calculations
            // For "123456789", the checksum should be:
            // (1×1 + 2×2 + 3×3 + 4×4 + 5×5 + 6×6 + 7×7 + 8×8 + 9×9) mod 11
            // = (1 + 4 + 9 + 16 + 25 + 36 + 49 + 64 + 81) mod 11
            // = 285 mod 11 = 10 = X

            var result = "123456789".NormalizeIsbn();

            // Should become "123456789X" (ISBN-10) then convert to ISBN-13
            Assert.Equal("9781234567897", result);
        }

        [Fact]
        public void NormalizeIsbn_Isbn13ChecksumCalculation_WorksCorrectly()
        {
            // When converting ISBN-10 "0123456789" to ISBN-13:
            // Start with "978" + first 9 digits: "978012345678"
            // Calculate checksum: sum of alternating 1x and 3x weights
            // (9×1 + 7×3 + 8×1 + 0×3 + 1×1 + 2×3 + 3×1 + 4×3 + 5×1 + 6×3 + 7×1 + 8×3) mod 10
            // = (9 + 21 + 8 + 0 + 1 + 6 + 3 + 12 + 5 + 18 + 7 + 24) mod 10
            // = 114 mod 10 = 4
            // So checksum = (10 - 4) mod 10 = 6

            var result = "0123456789".NormalizeIsbn();

            Assert.Equal("9780123456786", result);
        }
    }
}
