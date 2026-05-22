using Consyzer.Core.Models;

namespace Consyzer.Core.Resolvers;

internal interface ILibraryResolutionResolver
{
    LibraryResolutionResult Resolve(LibraryResolutionContext context);
}
