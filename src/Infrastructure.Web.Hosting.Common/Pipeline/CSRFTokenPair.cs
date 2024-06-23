using Common;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Services.Shared;
using Infrastructure.Web.Api.Common;

namespace Infrastructure.Web.Hosting.Common.Pipeline;

/// <summary>
///     Creates a Token with a Signature.
///     The <see cref="Token" /> part contains a AES(256) combined value of: user_identifier + timestamp,
///     The <see cref="Signature" /> part is a HMAC signature of the user_identifier.
/// </summary>
public class CSRFTokenPair
{
    internal const string NullUserIdTokenValue = CallerConstants.AnonymousUserId;
    private const string TokenDelimiter = "||";

    private CSRFTokenPair(Optional<string> token, Optional<string> signature)
    {
        Token = token;
        Signature = signature;
    }

    public Optional<string> Signature { get; }

    public Optional<string> Token { get; }

    public static CSRFTokenPair CreateTokens(IEncryptionService encryptionService, string hmacSecret,
        Optional<string> userId)
    {
        hmacSecret.ThrowIfNotValuedParameter(nameof(hmacSecret));

        var qualifiedUserId = QualifyUserId(userId);
        var token = CreateEncryptedTokenForQualifiedUserId(encryptionService, qualifiedUserId);
        var signature = SignQualifiedUserId(hmacSecret, qualifiedUserId);

        return new CSRFTokenPair(token, signature);
    }

    public static CSRFTokenPair FromTokens(Optional<string> token, Optional<string> signature)
    {
        return new CSRFTokenPair(token, signature);
    }

    public bool IsValid(IEncryptionService encryptionService, string hmacSecret, Optional<string> userId)
    {
        hmacSecret.ThrowIfNotValuedParameter(nameof(hmacSecret));

        if (!Token.HasValue)
        {
            return false;
        }

        if (!Signature.HasValue)
        {
            return false;
        }

        var tokenValue = DecryptTokenValue(encryptionService, Token);
        var qualifiedUserId = GetUserIdFromTokenValue(tokenValue);
        var isVerified = VerifyQualifiedUserIdSignature(hmacSecret, qualifiedUserId, Signature);
        if (!isVerified)
        {
            return false;
        }

        return IsQualifiedUserId(qualifiedUserId, userId);
    }

    private static bool IsQualifiedUserId(string qualifiedUserId, string userId)
    {
        return userId.HasNoValue()
            ? qualifiedUserId == NullUserIdTokenValue
            : qualifiedUserId == userId;
    }

    private static string QualifyUserId(string userId)
    {
        return userId.HasValue()
            ? userId
            : NullUserIdTokenValue;
    }

    private static string GetUserIdFromTokenValue(string tokenValue)
    {
        return tokenValue.Substring(0, tokenValue.IndexOf(TokenDelimiter, StringComparison.Ordinal));
    }

    private static string CreateTokenValueFromUserId(string userId)
    {
        return $"{userId}{TokenDelimiter}{DateTime.UtcNow.Ticks}";
    }

    private static string CreateEncryptedTokenForQualifiedUserId(IEncryptionService encryptionService,
        string qualifiedUserId)
    {
        var tokenValue = CreateTokenValueFromUserId(qualifiedUserId);

        return EncryptTokenValue(encryptionService, tokenValue);
    }

    private static string SignQualifiedUserId(string hmacSecret, string qualifiedUserId)
    {
        var signer = new HMACSigner(qualifiedUserId, hmacSecret);
        return signer.Sign();
    }

    private static bool VerifyQualifiedUserIdSignature(string hmacSecret, string qualifiedUserId, string signature)
    {
        var signer = new HMACSigner(qualifiedUserId, hmacSecret);
        var verifier = new HMACVerifier(signer);
        return verifier.Verify(signature);
    }

    private static string EncryptTokenValue(IEncryptionService encryptionService, string userId)
    {
        userId.ThrowIfNotValuedParameter(nameof(userId));

        try
        {
            return encryptionService.Encrypt(userId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(Resources.CSFRTokenPair_FailedEncryptUserId, ex);
        }
    }

    private static string DecryptTokenValue(IEncryptionService encryptionService, string encryptedUserId)
    {
        try
        {
            return encryptionService.Decrypt(encryptedUserId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(Resources.CSRFTokenPair_FailedDecryptUserId, ex);
        }
    }
}