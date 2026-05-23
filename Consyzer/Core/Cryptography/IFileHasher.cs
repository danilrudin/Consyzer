namespace Consyzer.Core.Cryptography;

internal interface IFileHasher
{
    string CalculateHash(FileInfo file);
}
