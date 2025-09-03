namespace Consyzer.Options;

[Flags]
public enum OutputFormats
{
    Console = 1 << 0,
    Json = 1 << 1,
    Csv = 1 << 2,
    Xml = 1 << 3
}
