namespace Consyzer.Tests.TestSupport.Collections;

// Resolver tests change process-wide environment variables and the current directory.
[CollectionDefinition(DisableParallelization = true)]
public sealed class ResolverEnvironmentCollection
{
}
