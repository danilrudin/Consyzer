namespace Consyzer.Output.Builders;

internal sealed class CsvTableBuilder(char delimiter)
{
    private readonly System.Text.StringBuilder _builder = new();

    public CsvTableBuilder Header(IEnumerable<string> fields)
        => AppendRow(fields);

    public CsvTableBuilder Record(IEnumerable<string> fields)
        => AppendRow(fields);

    public string Build() => _builder.ToString();

    private CsvTableBuilder AppendRow(IEnumerable<string> fields)
    {
        _builder.AppendJoin(delimiter, fields).AppendLine();
        return this;
    }
}
