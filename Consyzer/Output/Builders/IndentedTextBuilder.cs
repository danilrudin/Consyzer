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
        AppendIndent();
        _sb.AppendLine(line);
        return this;
    }

    public IndentedTextBuilder Line(string line, object? value)
    {
        AppendIndent();
        _sb.Append(line).Append(": ").Append(value).AppendLine();
        return this;
    }

    public IndentedTextBuilder IndexedItems<T>(IEnumerable<T> items, Func<T, string> formatter)
    {
        var index = 0;

        foreach (var item in items)
        {
            AppendIndent();
            _sb.Append('[')
                .Append(index)
                .Append("] ")
                .AppendLine(formatter(item));
            ++index;
        }

        return this;
    }

    public IndentedTextBuilder IndexedSection<T>(IEnumerable<T> items, Action<IndentedTextBuilder, T> renderer)
    {
        var index = 0;

        foreach (var item in items)
        {
            AppendIndent();
            _sb.Append('[').Append(index).AppendLine("]");
            PushIndent();
            renderer(this, item);
            PopIndent();
            ++index;
        }

        return this;
    }

    public string Build() => _sb.ToString();

    private void AppendIndent()
    {
        for (var i = 0; i < _level; ++i)
        {
            _sb.Append(indentedChars);
        }
    }
}
