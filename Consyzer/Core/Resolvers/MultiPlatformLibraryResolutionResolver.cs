using Consyzer.Core.Models.Resolution;
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

    public string PlatformName => _resolver.PlatformName;
    public LibraryResolution Resolve(LibraryResolutionContext context) => _resolver.Resolve(context);
}
