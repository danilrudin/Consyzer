using Consyzer.Core.Models.Resolution;

namespace Consyzer.Core.Resolvers;

internal interface ILibraryResolutionResolver
{
    string PlatformName { get;  }
    LibraryResolution Resolve(LibraryResolutionContext context);
}
