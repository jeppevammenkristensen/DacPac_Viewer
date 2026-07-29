using DacPac.UI.ApplicationLayer.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DacPac.UI.ApplicationLayer.Tests.Infrastructure;

/// <summary>
/// Verifies machine-derived connection string protection.
/// </summary>
public class StringEncrypterTest
{
    [Fact]
    public void Encrypt_ProducesDifferentPayloads_ForTheSamePlainText()
    {
        var encrypter = CreateEncrypter();

        var first = encrypter.Encrypt("Server=example;Database=dacpac");
        var second = encrypter.Encrypt("Server=example;Database=dacpac");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Decrypt_ReturnsOriginalPlainText_ForAnEncryptedValue()
    {
        var encrypter = CreateEncrypter();
        const string plainText = "Server=example;Database=dacpac";

        var encrypted = encrypter.Encrypt(plainText);

        Assert.Equal(plainText, encrypter.Decrypt(encrypted));
    }

    private static StringEncrypter CreateEncrypter() =>
        new(NullLogger<StringEncrypter>.Instance, new TestMachineIdentityProvider());

    /// <summary>
    /// Supplies a stable machine identity for encryption tests.
    /// </summary>
    private sealed class TestMachineIdentityProvider : IMachineIdentityProvider
    {
        /// <summary>
        /// Gets a deterministic machine identity.
        /// </summary>
        public string GetMachineIdentity() => "test-machine";
    }
}