using Consyzer.Core.Models;
using static Consyzer.Constants.LibrarySpace;

namespace Consyzer.Core.Resolvers.Platform;

internal sealed class WindowsLibraryPresenceResolver(
    string analyzedDirectory
) : IPlatformLibraryPresenceResolver
{
    private readonly Func<string, LibraryPresence?>[] _resolvers =
    [
        name => ResolveAnalyzedDirectory(analyzedDirectory, name),
        ResolveSystemDirectory,
        ResolveInEnvironmentPath,
        ResolveAbsolutePath,
        ResolveRelativePath
    ];

    public LibraryPresence Resolve(string file)
    {
        var candidateName = ResolveLibraryName(file);

        foreach (var resolver in _resolvers)
        {
            var presence = resolver(candidateName);
            if (presence is not null)
            {
                return presence;
            }
        }

        return new LibraryPresence
        {
            LibraryName = file,
            ResolvedPath = null,
            LocationKind = LibraryLocationKind.Missing
        };
    }

    private static LibraryPresence? ResolveAnalyzedDirectory(string analyzedDirectory, string file)
    {
        var candidate = GetCandidatePath(analyzedDirectory, file);
        if (candidate is null || !IsPathInsideDirectory(analyzedDirectory, candidate)) return null;

        return new LibraryPresence
        {
            LibraryName = file,
            ResolvedPath = candidate,
            LocationKind = LibraryLocationKind.InAnalyzedDirectory
        };
    }

    private static LibraryPresence? ResolveSystemDirectory(string file)
    {
        var candidate = GetCandidatePath(Environment.SystemDirectory, file);
        if (candidate is null || !IsPathInsideDirectory(Environment.SystemDirectory, candidate)) return null;

        return new LibraryPresence
        {
            LibraryName = file,
            ResolvedPath = candidate,
            LocationKind = LibraryLocationKind.InSystemDirectory
        };
    }

    private static LibraryPresence? ResolveInEnvironmentPath(string file)
    {
        if (Path.IsPathRooted(file) || Path.GetFileName(file) != file) return null;

        var pathDirectories = (Environment.GetEnvironmentVariable(Variable.Path) ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var directory in pathDirectories)
        {
            var candidate = Path.Combine(directory, file);
            if (File.Exists(candidate))
            {
                return new LibraryPresence
                {
                    LibraryName = file,
                    ResolvedPath = Path.GetFullPath(candidate),
                    LocationKind = LibraryLocationKind.InEnvironmentPath
                };
            }
        }

        return null;
    }

    private static LibraryPresence? ResolveAbsolutePath(string file)
    {
        if (!Path.IsPathRooted(file)) return null;

        var candidate = GetCandidatePath(null, file);
        if (candidate is null) return null;

        return new LibraryPresence
        {
            LibraryName = file,
            ResolvedPath = candidate,
            LocationKind = LibraryLocationKind.OnAbsolutePath
        };
    }

    private static LibraryPresence? ResolveRelativePath(string file)
    {
        if (Path.IsPathRooted(file)) return null;

        var candidate = Path.Combine(Directory.GetCurrentDirectory(), file);

        if (File.Exists(candidate))
        {
            return new LibraryPresence
            {
                LibraryName = file,
                ResolvedPath = Path.GetFullPath(candidate),
                LocationKind = LibraryLocationKind.OnRelativePath
            };
        }

        return null;
    }

    private static string ResolveLibraryName(string file)
    {
        if (Path.HasExtension(file))
        {
            return file;
        }

        return file + Extension.WindowsExtension;
    }

    private static string? GetCandidatePath(string? baseDirectory, string file)
    {
        var candidate = baseDirectory is not null
            ? Path.Combine(baseDirectory, file)
            : file;

        return File.Exists(candidate) ? candidate : null;
    }

    private static bool IsPathInsideDirectory(string baseDirectory, string path)
    {
        var baseFull = Path.GetFullPath(baseDirectory);
        var fileFull = Path.GetFullPath(path);
        var relative = Path.GetRelativePath(baseFull, fileFull);
        return !relative.StartsWith("..", StringComparison.OrdinalIgnoreCase);
    }
}
