using Xunit;
using System.Security.Cryptography;
using Consyzer.Core.Cryptography;
using static Consyzer.Tests.TestInfrastructure.Constants;

namespace Consyzer.Tests.Core.Cryptography;

public sealed class Sha256FileHasherTests
{
    [Fact]
    public void CalculateHash_ShouldReturnCorrectByteLength_WhenConverted()
    {
        var hasher = new Sha256FileHasher();

        var hexHash = hasher.CalculateHash(EcmaAssemblyWithPInvoke);
        var byteHash = Convert.FromHexString(hexHash);

        Assert.Equal(SHA256.HashSizeInBytes, byteHash.Length);
    }

    [Fact]
    public void CalculateHash_ShouldReturnDifferentHashes_WhenFilesAreDifferent()
    {
        var hasher = new Sha256FileHasher();

        var hash1 = hasher.CalculateHash(EcmaAssemblyWithPInvoke);
        var hash2 = hasher.CalculateHash(NonEcmaAssembly);

        Assert.NotEqual(hash1, hash2);
    }
}
