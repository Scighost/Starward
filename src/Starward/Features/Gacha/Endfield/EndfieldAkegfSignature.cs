using System;
using System.Buffers.Text;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Gacha.Endfield;

internal enum EndfieldAkegfVerification
{
    Unsigned,
    LegacySignature,
    Example,
    Verified,
    KeyUnknown,
    KeyRevoked,
    KeyInvalid,
    RegistryUnavailable,
    SignatureVersionMismatch,
    SignatureMismatch,
}

internal static class EndfieldAkegfSignature
{
    private static readonly HttpClient KeyClient = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
    })
    {
        Timeout = TimeSpan.FromSeconds(8),
    };

    internal static async Task<EndfieldAkegfVerification> VerifyAsync(JsonElement archive, string provider, string signature,
        CancellationToken cancellationToken)
    {
        string[] parts = signature.Split('.');
        string fingerprint = parts[0];
        try
        {
            using var response = await KeyClient.GetAsync(
                $"https://verify.akedata.wiki/v1/providers/{Uri.EscapeDataString(provider)}/keys/{Uri.EscapeDataString(fingerprint)}.json",
                cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return EndfieldAkegfVerification.KeyUnknown;
            }
            if (!response.IsSuccessStatusCode)
            {
                return EndfieldAkegfVerification.RegistryUnavailable;
            }
            using JsonDocument responseBody = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonDocumentOptions { AllowDuplicateProperties = false }, cancellationToken);
            JsonElement key = responseBody.RootElement;
            if (key.GetProperty("provider").GetString() != provider
                || key.GetProperty("kid").GetString() != fingerprint
                || key.GetProperty("alg").GetString() != "PS256")
            {
                return EndfieldAkegfVerification.KeyInvalid;
            }
            string? status = key.GetProperty("status").GetString();
            if (status != "active")
            {
                return status == "revoked" ? EndfieldAkegfVerification.KeyRevoked : EndfieldAkegfVerification.KeyInvalid;
            }
            byte[] publicKey = Base64Url.DecodeFromChars(key.GetProperty("public_key").GetString()!);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(publicKey), Base64Url.DecodeFromChars(fingerprint)))
            {
                return EndfieldAkegfVerification.KeyInvalid;
            }
            using RSA rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out int bytesRead);
            byte[] signatureBytes = Base64Url.DecodeFromChars(parts[1]);
            if (bytesRead != publicKey.Length || rsa.KeySize is not (2048 or 3072 or 4096))
            {
                return EndfieldAkegfVerification.KeyInvalid;
            }
            if (signatureBytes.Length * 8 != rsa.KeySize)
            {
                return EndfieldAkegfVerification.SignatureMismatch;
            }
            if (rsa.VerifyData(GetSignedBytes(archive), signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
            {
                return EndfieldAkegfVerification.Verified;
            }
            // Diagnose old exporters that changed the version after signing.
            // The legacy payload is never accepted as a valid v1.2 archive.
            return rsa.VerifyData(GetSignedBytes(archive, versionOverride: "v0.4"), signatureBytes,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pss)
                ? EndfieldAkegfVerification.SignatureVersionMismatch : EndfieldAkegfVerification.SignatureMismatch;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return EndfieldAkegfVerification.RegistryUnavailable;
        }
        catch (Exception error) when (error is HttpRequestException or JsonException)
        {
            return EndfieldAkegfVerification.RegistryUnavailable;
        }
        catch (Exception error) when (error is FormatException or CryptographicException
                                      or InvalidOperationException or System.Collections.Generic.KeyNotFoundException)
        {
            return EndfieldAkegfVerification.KeyInvalid;
        }
    }

    internal static byte[] GetSignedBytes(JsonElement archive, string? versionOverride = null)
    {
        var message = new StringBuilder("AKEGF-RSA-PSS-v1\n");
        AppendJson(message, archive, depth: 0, versionOverride: versionOverride);
        return new UTF8Encoding(false, throwOnInvalidBytes: true).GetBytes(message.ToString());
    }

    // AKEGF contains only safe integers, strings, booleans, arrays and objects.
    private static void AppendJson(StringBuilder output, JsonElement value, int depth,
        bool isInfo = false, string? versionOverride = null)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                output.Append('{');
                bool firstMember = true;
                foreach (JsonProperty member in value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    if (isInfo && member.NameEquals("rsa_signature"))
                    {
                        continue;
                    }
                    if (!firstMember) output.Append(',');
                    firstMember = false;
                    AppendString(output, member.Name);
                    output.Append(':');
                    if (isInfo && versionOverride is not null && member.NameEquals("version"))
                    {
                        AppendString(output, versionOverride);
                    }
                    else
                    {
                        AppendJson(output, member.Value, depth + 1, depth == 0 && member.NameEquals("info"), versionOverride);
                    }
                }
                output.Append('}');
                break;
            case JsonValueKind.Array:
                output.Append('[');
                bool firstValue = true;
                foreach (JsonElement item in value.EnumerateArray())
                {
                    if (!firstValue) output.Append(',');
                    firstValue = false;
                    AppendJson(output, item, depth + 1);
                }
                output.Append(']');
                break;
            case JsonValueKind.String:
                AppendString(output, value.GetString()!);
                break;
            case JsonValueKind.Number:
                output.Append(((long)value.GetDecimal()).ToString(CultureInfo.InvariantCulture));
                break;
            case JsonValueKind.True:
                output.Append("true");
                break;
            case JsonValueKind.False:
                output.Append("false");
                break;
            default:
                throw new InvalidOperationException("Unexpected JSON value in AKEGF.");
        }
    }

    private static void AppendString(StringBuilder output, string value)
    {
        output.Append('"');
        foreach (char character in value)
        {
            switch (character)
            {
                case '"': output.Append("\\\""); break;
                case '\\': output.Append("\\\\"); break;
                case '\b': output.Append("\\b"); break;
                case '\f': output.Append("\\f"); break;
                case '\n': output.Append("\\n"); break;
                case '\r': output.Append("\\r"); break;
                case '\t': output.Append("\\t"); break;
                case < ' ': output.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture)); break;
                default: output.Append(character); break;
            }
        }
        output.Append('"');
    }
}
