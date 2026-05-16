namespace Consyzer.Options;

internal sealed class AppSettingsOptions
{
    public required OutputOptions Output {  get; set; }

    public sealed class OutputOptions
    {
        public required ConsoleOptions Console { get; set; }

        public sealed class ConsoleOptions
        {
            public required string IndentChars { get; set; }
        }

        public required JsonOptions Json { get; set; }

        public sealed class JsonOptions
        {
            public required string Encoding { get; set; }
        }

        public required CsvOptions Csv { get; set; }

        public sealed class CsvOptions
        {
            public required string Encoding { get; set; }
            public required char Delimiter { get; set; }
        }

        public required XmlOptions Xml { get; set; }

        public sealed class XmlOptions
        {
            public required string Encoding { get; set; }
            public required string IndentChars { get; set; }
        }
    }
}
