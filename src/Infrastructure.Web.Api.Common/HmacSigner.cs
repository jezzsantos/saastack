using System.Security.Cryptography;
using System.Text;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common;

/// <summary>
///     Provides a signer of HMAC signatures
/// </summary>
public class HmacSigner
{
    private const string EmptyRequestJson = "{}";
    private const string SignatureFormat = @"sha1={0}";
    internal static readonly Encoding SignatureEncoding = Encoding.UTF8;
    private readonly byte[] _data;
    private readonly string _secret;

    public HmacSigner(IWebRequest request, string secret)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(secret);

        _data = GetRequestData(request);
        _secret = secret;
    }

    public string Sign()
    {
        var key = SignatureEncoding.GetBytes(_secret);
        var signature = SignatureFormat.Format(SignBody(_data, key));

        return signature;
    }

    private static byte[] GetRequestData(IWebRequest request)
    {
        var json = request.ToJson(false, StringExtensions.JsonCasing.Pascal) ?? EmptyRequestJson;
        if (json == EmptyRequestJson)
        {
            return Array.Empty<byte>();
        }

        return SignatureEncoding.GetBytes(json);
    }

    private static string SignBody(byte[] body, byte[] key)
    {
        var signature = new HMACSHA256(key)
            .ComputeHash(body);

        return ToHex(signature);
    }

    private static string ToHex(byte[] bytes)
    {
        var builder = new StringBuilder();
        bytes
            .ToList()
            .ForEach(b => { builder.Append(b.ToString("x2")); });

        return builder.ToString();
    }
}

/// <summary>
///     Provides a verifier of HMAC signatures
/// </summary>
public class HmacVerifier
{
    private readonly HmacSigner _signer;

    public HmacVerifier(HmacSigner signer)
    {
        ArgumentNullException.ThrowIfNull(signer);

        _signer = signer;
    }

    public bool Verify(string signature)
    {
        ArgumentException.ThrowIfNullOrEmpty(signature);

        var expectedSignature = _signer.Sign();

        return expectedSignature == signature;
    }
}