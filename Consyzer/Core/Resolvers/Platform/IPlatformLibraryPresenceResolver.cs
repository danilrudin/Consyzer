using Consyzer.Core.Models;

namespace Consyzer.Core.Resolvers.Platform;

internal interface IPlatformLibraryPresenceResolver
{
    LibraryPresence Resolve(string file);
}
