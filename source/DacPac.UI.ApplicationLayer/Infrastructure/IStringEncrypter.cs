namespace DacPac.UI.ApplicationLayer.Infrastructure;

/// <summary>
/// Defines machine-bound string encryption and decryption operations.
/// </summary>
public interface IStringEncrypter
{
    /// <summary>
    /// Decrypts a protected string.
    /// </summary>
    string? Decrypt(string source);

    /// <summary>
    /// Encrypts a string for protected storage.
    /// </summary>
    string Encrypt(string source);
}