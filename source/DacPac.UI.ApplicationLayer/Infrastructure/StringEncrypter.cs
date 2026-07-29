using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

/// <summary>
/// Encrypts and decrypts strings using Windows DPAPI or a Linux machine-derived AES-GCM key.
/// </summary>
public class StringEncrypter : IStringEncrypter
{
    private static readonly byte[] ConnectionStringEntropy = "DacPac Viewer connection string"u8.ToArray();
    private const string LinuxPrefix = "aesgcm:";
    private readonly ILogger<StringEncrypter> _logger;
    private readonly IMachineIdentityProvider _machineIdentityProvider;

    /// <summary>
    /// Initializes a string encrypter with the Linux machine identity source and decryption logging.
    /// </summary>
    public StringEncrypter(ILogger<StringEncrypter> logger, IMachineIdentityProvider machineIdentityProvider)
    {
        _logger = logger;
        _machineIdentityProvider = machineIdentityProvider;
    }

    /// <summary>
    /// Decrypts a protected string or returns <see langword="null"/> when it cannot be decrypted.
    /// </summary>
    public string? Decrypt(string source)
    {
        try
        {
            return OperatingSystem.IsWindows() ? DecryptWindows(source) : DecryptLinux(source);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            _logger.LogWarning(ex, "Failed to decrypt the saved connection string; ignoring it");
            return null;
        }
    }

    /// <summary>
    /// Encrypts a string using the current platform's machine-bound protection.
    /// </summary>
    public string Encrypt(string source)
    {
        if (OperatingSystem.IsWindows())
        {
            var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(source), ConnectionStringEntropy,
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }

        return EncryptLinux(source);
    }

    /// <summary>
    /// Decrypts a string protected with Windows DPAPI.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static string DecryptWindows(string source)
    {
        var protectedBytes = Convert.FromBase64String(source);
        var plainText =
            ProtectedData.Unprotect(protectedBytes, ConnectionStringEntropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainText);
    }

    /// <summary>
    /// Encrypts a string with AES-GCM using a key derived from Linux machine identity and user name.
    /// </summary>
    private string EncryptLinux(string source)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainText = Encoding.UTF8.GetBytes(source);
        var cipherText = new byte[plainText.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(DeriveLinuxKey(), tag.Length);
        aes.Encrypt(nonce, plainText, cipherText, tag);

        return LinuxPrefix + Convert.ToBase64String([.. nonce, .. tag, .. cipherText]);
    }

    /// <summary>
    /// Decrypts an AES-GCM payload encrypted by <see cref="EncryptLinux"/>.
    /// </summary>
    private string DecryptLinux(string source)
    {
        if (!source.StartsWith(LinuxPrefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("The saved connection string is not a Linux-encrypted value.");
        }

        var payload = Convert.FromBase64String(source[LinuxPrefix.Length..]);
        if (payload.Length < 28)
        {
            throw new CryptographicException("The saved connection string is incomplete.");
        }

        var nonce = payload[..12];
        var tag = payload[12..28];
        var cipherText = payload[28..];
        var plainText = new byte[cipherText.Length];

        using var aes = new AesGcm(DeriveLinuxKey(), tag.Length);
        aes.Decrypt(nonce, cipherText, tag, plainText);

        return Encoding.UTF8.GetString(plainText);
    }

    /// <summary>
    /// Derives a fixed AES key from Linux machine identity, the current user, and application-specific entropy.
    /// </summary>
    private byte[] DeriveLinuxKey()
    {
        var material = $"{_machineIdentityProvider.GetMachineIdentity()}:{Environment.UserName}";
        return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(material), ConnectionStringEntropy,
            iterations: 100_000, HashAlgorithmName.SHA256, outputLength: 32);
    }
}