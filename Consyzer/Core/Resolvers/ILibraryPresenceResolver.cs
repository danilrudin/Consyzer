using Consyzer.Core.Models;

namespace Consyzer.Core.Resolvers;

internal interface ILibraryPresenceResolver
{
    LibraryPresence Resolve(string file);
}
