using Consyzer.Core.Caching;
using Consyzer.Core.Extractors;
using Consyzer.Core.Cryptography;
using static Consyzer.Tests.TestSupport.Samples.TestFiles;
using static Consyzer.Tests.TestSupport.Helpers.FileAssertionHelper;

namespace Consyzer.Tests.Core.Extractors;

public sealed class AssemblyMetadataExtractorTests
{
    private const string SemanticVersionRegex = @"^\d+\.\d+\.\d+(\.\d+)?$";

    [Fact]
    public void Extract_ShouldReturnCorrectMetadata_WhenCalled()
    {
        using var peAccessor = new MetadataOnlyPEReaderCache();
        var hasher = new Sha256FileHasher();

        var extractor = new AssemblyMetadataExtractor(hasher, peAccessor);

        var metadata = extractor.Extract(AssemblyWithPInvoke);

        EqualPath(AssemblyWithPInvoke, metadata.File);
        EqualCreationTimeUtc(AssemblyWithPInvoke, metadata.CreationDateUtc);
        Assert.NotEmpty(metadata.Sha256);
        Assert.Matches(SemanticVersionRegex, metadata.Version);
    }
}
