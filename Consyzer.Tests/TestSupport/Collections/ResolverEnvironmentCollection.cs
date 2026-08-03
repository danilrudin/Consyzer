namespace Consyzer.Tests.TestSupport.Collections;

internal static class TestCollectionNames
{
    public const string ResolverEnvironment = "Resolver environment";
}

[CollectionDefinition(TestCollectionNames.ResolverEnvironment, DisableParallelization = true)]
public sealed class ResolverEnvironmentCollection
{
}
