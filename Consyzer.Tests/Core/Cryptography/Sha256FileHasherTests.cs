using Consyzer.Core.Cryptography;
using Consyzer.Tests.TestSupport.FileSystem;
using static Consyzer.Tests.TestSupport.Samples.TestFiles;

namespace Consyzer.Tests.Core.Cryptography;

public sealed class Sha256FileHasherTests
{
    [Fact]
    public void CalculateHash_ShouldReturnExpectedHash_ForKnownContent()
    {
        using var directory = new TemporaryDirectory();
        var file = directory.CreateFile("sample.txt", "abc");
        var hasher = new Sha256FileHasher();

        var hash = hasher.CalculateHash(file);

        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", hash);
    }

    [Fact]
    public void CalculateHash_ShouldReturnDifferentHashes_WhenFilesAreDifferent()
    {
        var hasher = new Sha256FileHasher();

        var hash1 = hasher.CalculateHash(AssemblyWithPInvoke);
        var hash2 = hasher.CalculateHash(NonEcmaModule);

        Assert.NotEqual(hash1, hash2);
    }
}
