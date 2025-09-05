using Consyzer.Core.Models;
using Consyzer.Core.Resolvers.Platform;

namespace Consyzer.Core.Resolvers;

internal sealed class PlatformLibraryPresenceResolver : ILibraryPresenceResolver
{
    private readonly IPlatformLibraryPresenceResolver _libraryResolver;

    public PlatformLibraryPresenceResolver(string analyzedDirectory)
    {
        if (OperatingSystem.IsWindows())
            _libraryResolver = new WindowsLibraryPresenceResolver(analyzedDirectory);
        else
            throw new PlatformNotSupportedException();
    }

    public LibraryPresence Resolve(string file) => _libraryResolver.Resolve(file);
}
