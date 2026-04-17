using Consyzer.Core.Models;
using Consyzer.Core.Resolvers.Platform;

namespace Consyzer.Core.Resolvers;

internal sealed class MultiPlatformLibraryResolutionResolver(
    string analyzedDirectory
) : ILibraryResolutionResolver
{
    private readonly PlatformLibraryResolutionResolverBase _resolver = true switch
    {
        _ when OperatingSystem.IsWindows() 
            => new WindowsLibraryResolutionResolver(analyzedDirectory),
        _ when OperatingSystem.IsLinux() 
            => new LinuxLibraryResolutionResolver(analyzedDirectory),
        _ => throw new PlatformNotSupportedException()
    };

    public LibraryResolutionResult Resolve(string file) => _resolver.Resolve(file);
}
