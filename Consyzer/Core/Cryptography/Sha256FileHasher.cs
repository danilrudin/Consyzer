using System.Security.Cryptography;

namespace Consyzer.Core.Cryptography;

internal sealed class Sha256FileHasher : IFileHasher
{
    public string CalculateHash(FileInfo file)
    {
        using var stream = file.OpenRead();
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
