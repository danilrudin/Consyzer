namespace Consyzer.Output.Builders;

internal sealed class CsvTableBuilder(char delimiter)
{
    private readonly List<string> _lines = [];

    public CsvTableBuilder Header(IEnumerable<string> fields)
    {
        _lines.Add(string.Join(delimiter, fields));
        return this;
    }

    public CsvTableBuilder Record(IEnumerable<string> fields)
    {
        _lines.Add(string.Join(delimiter, fields));
        return this;
    }

    public CsvTableBuilder Records<T>(IEnumerable<T> items, Func<object?, string> serializer)
    {
        var props = typeof(T).GetProperties();
        Header(props.Select(p => p.Name));

        foreach (var item in items)
        {
            Record(props.Select(p => serializer(p.GetValue(item))));
        }

        return this;
    }

    public string Build() => string.Join(Environment.NewLine, _lines) + Environment.NewLine;
}
