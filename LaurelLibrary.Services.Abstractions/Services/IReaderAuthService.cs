using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IReaderAuthService
{
    /// <summary>
    /// Validates reader credentials (EAN and date of birth) and sends a verification code via email.
    /// Returns the reader's ID if successful, null if the reader is not found or email is missing.
    /// </summary>
    Task<int?> SendVerificationCodeAsync(ReaderLoginRequestDto loginRequest, Guid libraryId);

    /// <summary>
    /// Verifies the code entered by the reader.
    /// Returns the reader's ID if successful, null if invalid.
    /// </summary>
    Task<int?> VerifyCodeAsync(ReaderVerificationDto verificationDto);

    /// <summary>
    /// Clears the verification code for a reader (after successful login or timeout).
    /// </summary>
    Task ClearVerificationCodeAsync(string ean);
}
