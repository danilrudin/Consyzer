using System.Text;

namespace Consyzer.Output.Builders;

internal sealed class IndentedTextBuilder(
    string indentedChars
)
{
    private int _level;
    private readonly StringBuilder _sb = new();

    public IndentedTextBuilder PushIndent()
    {
        ++_level;
        return this;
    }

    public IndentedTextBuilder PopIndent()
    {
        if (_level > 0) --_level;
        return this;
    }

    public IndentedTextBuilder Title(string title)
    {
        _sb.AppendLine(title);
        return this;
    }

    public IndentedTextBuilder Line(string line)
    {
        _sb.AppendLine($"{IndentString()}{line}");
        return this;
    }

    public IndentedTextBuilder Line(string line, object? value)
    {
        _sb.AppendLine($"{IndentString()}{line}: {value}");
        return this;
    }

    public IndentedTextBuilder IndexedItems<T>(IEnumerable<T> items, Func<T, string> formatter)
    {
        var indent = IndentString();

        foreach (var (item, index) in items.Select((x, i) => (x, i)))
        {
            _sb.AppendLine($"{indent}[{index}] {formatter(item)}");
        }

        return this;
    }

    public IndentedTextBuilder IndexedSection<T>(IEnumerable<T> items, Action<IndentedTextBuilder, T> renderer)
    {
        var indent = IndentString();

        foreach (var (item, index) in items.Select((x, i) => (x, i)))
        {
            _sb.AppendLine($"{indent}[{index}]");
            PushIndent();
            renderer(this, item);
            PopIndent();
        }

        return this;
    }

    public string Build() => _sb.ToString();

    private string IndentString() => string.Concat(Enumerable.Repeat(indentedChars, _level));
}
