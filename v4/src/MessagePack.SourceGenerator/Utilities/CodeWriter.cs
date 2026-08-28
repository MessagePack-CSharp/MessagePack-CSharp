using System.Text;

namespace MessagePack.SourceGenerator;

/// <summary>
/// Minimal indentation-owning writer for the emitters: Line() prefixes the current
/// depth, Block()/OpenScope() write braces and track depth via disposal, so emission
/// code never threads indent strings by hand. Deliberately nothing more — no wrapping,
/// no token awareness; the emitters stay plain string writes.
/// </summary>
sealed class CodeWriter
{
    const string IndentUnit = "    ";

    // Reuse: one cached instance per thread, rented at the top of an emit and returned
    // by ToStringAndReturn. Serial execution of RegisterSourceOutput callbacks is an
    // implementation detail, and the SAME generator instance can run concurrently for
    // different projects in one IDE process (AnalyzerFileReference shares generator
    // instances across drivers) — so a plain static would corrupt; ThreadStatic is
    // correct by construction. Clear() keeps the chunk arrays, which is the point.
    [ThreadStatic]
    static CodeWriter? cached;

    // an emitter gone pathological (a multi-thousand-member type) should not pin its
    // buffer on the thread forever
    const int MaxCachedCapacity = 256 * 1024;

    static readonly char[] NewlineChars = ['\r', '\n'];

    readonly StringBuilder builder = new StringBuilder();
    int depth;

    /// <summary>Takes the thread's cached instance (reset, capacity retained) or a fresh one.</summary>
    public static CodeWriter Rent()
    {
        var writer = cached;
        if (writer is null)
        {
            return new CodeWriter();
        }
        cached = null; // a nested Rent before return gets a fresh instance instead of aliasing
        writer.builder.Clear();
        writer.depth = 0;
        return writer;
    }

    /// <summary>Renders the accumulated text and puts the instance back into the thread cache.</summary>
    public string ToStringAndReturn()
    {
        var result = builder.ToString();
        if (builder.Capacity <= MaxCachedCapacity)
        {
            cached = this;
        }
        return result;
    }

    /// <summary>Writes one line at the current depth; an empty call writes a blank line (no trailing spaces).</summary>
    public CodeWriter Line(string text = "")
    {
        if (text.IndexOfAny(NewlineChars) >= 0)
        {
            // the writer OWNS every newline ('\n' only): a multi-line literal sneaking in
            // here is how \r\n and \n get mixed in generated output (string literals take
            // the line endings of the source file they sit in, which git rewrites per OS)
            throw new ArgumentException($"Line text must be a single line: \"{text}\"", nameof(text));
        }
        if (text.Length > 0)
        {
            for (int i = 0; i < depth; i++)
            {
                builder.Append(IndentUnit);
            }
            builder.Append(text);
        }
        builder.Append('\n');
        return this;
    }

    public CodeWriter Indent()
    {
        depth++;
        return this;
    }

    public CodeWriter Unindent()
    {
        depth--;
        return this;
    }

    /// <summary>Header line + opening brace; disposal writes the closing brace.</summary>
    public BlockScope Block(string header)
    {
        Line(header);
        return OpenScope();
    }

    /// <summary>Bare opening brace; disposal closes with "}" plus <paramref name="closeSuffix"/> (e.g. ";" for object initializers).</summary>
    public BlockScope OpenScope(string closeSuffix = "")
    {
        Line("{");
        depth++;
        return new BlockScope(this, closeSuffix);
    }

    void CloseScope(string suffix)
    {
        depth--;
        Line("}" + suffix);
    }

    public override string ToString() => builder.ToString();

    public readonly struct BlockScope : IDisposable
    {
        readonly CodeWriter writer;
        readonly string suffix;

        internal BlockScope(CodeWriter writer, string suffix)
        {
            this.writer = writer;
            this.suffix = suffix;
        }

        public void Dispose() => writer.CloseScope(suffix);
    }
}
