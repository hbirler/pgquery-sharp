using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PostgresQuery;

/// <summary>
/// Parsing mode for <c>libpg_query</c>. Controls which grammar or context the input is parsed with.
/// </summary>
/// <remarks>
/// Mirrors the native <c>PgQueryParseMode</c> values but keeps the native details internal.
/// </remarks>
public enum ParseMode
{
    Default = 0,
    TypeName,
    PlpgsqlExpr,
    PlpgsqlAssign1,
    PlpgsqlAssign2,
    PlpgsqlAssign3,
}

/// <summary>
/// Additional parser flags that affect how string literals are handled, matching
/// libpg_query / PostgreSQL knobs. Combine with a <see cref="ParseMode"/>.
/// </summary>
[Flags]
public enum ParserFlags
{
    /// <summary>No extra flags.</summary>
    None = 0,

    /// <summary>Disable backslash escapes in ordinary string literals (treat <c>\</c> as ordinary char).</summary>
    DisableBackslashQuote = 1 << 4, // 16

    /// <summary>Disable standard-conforming strings (legacy escaping behavior).</summary>
    DisableStandardConformingStrings = 1 << 5, // 32

    /// <summary>Disable the server-side "escape string" warning.</summary>
    DisableEscapeStringWarning = 1 << 6, // 64
}

/// <summary>
/// How to split a SQL batch into individual statements.
/// </summary>
public enum SplitAlgorithm
{
    /// <summary>Split using the lightweight scanner (fast, syntax-agnostic).</summary>
    Scanner = 0,

    /// <summary>Split using the full parser (more accurate with tricky constructs).</summary>
    Parser = 1,
}

/// <summary>
/// Describes a statement's byte range within the original SQL (UTF-8) buffer.
/// </summary>
public readonly struct StatementSpan
{
    /// <summary>Byte offset of the statement's first byte.</summary>
    public int Location { get; }

    /// <summary>Length in bytes.</summary>
    public int Length { get; }

    public StatementSpan(int location, int length)
    {
        Location = location;
        Length = length;
    }

    /// <summary>Converts to a managed <see cref="Range"/> suitable for slicing the original string.</summary>
    public Range AsRange() => new(Location, Location + Length);

    public override string ToString() => $"[{Location}..{Location + Length})";
}

/// <summary>
/// Pretty-printing controls for deparsing a Protobuf parse tree back to SQL.
/// </summary>
public struct DeparseOptions()
{
    /// <summary>Pretty-print output (line breaks, indentation).</summary>
    public bool PrettyPrint { get; init; } = true;

    /// <summary>Spaces per indentation level when pretty-printing.</summary>
    public int IndentSize { get; init; } = 2;

    /// <summary>Wrapping hint for pretty-printers (0 means no limit).</summary>
    public int MaxLineLength { get; init; } = 0;

    /// <summary>Append a trailing newline to the result.</summary>
    public bool TrailingNewline { get; init; } = true;

    /// <summary>Place commas at the start of wrapped lines (as opposed to end of line).</summary>
    public bool CommasStartOfLine { get; init; } = false;
}

/// <summary>
/// A deparsed comment discovered or injected around a statement, useful when analyzing formatting.
/// Returned by <see cref="GetCommentsForQuery(string)"/>.
/// </summary>
public struct DeparseComment()
{
    /// <summary>Byte offset where the related match was found.</summary>
    public int MatchLocation { get; init; }

    /// <summary>Newline count immediately before the comment.</summary>
    public int NewlinesBefore { get; init; }

    /// <summary>Newline count immediately after the comment.</summary>
    public int NewlinesAfter { get; init; }

    /// <summary>The comment text (e.g., <c>-- hi</c> or <c>/* hi */</c>).</summary>
    public string Text { get; init; } = "";
}

/// <summary>
/// Fingerprint produced by libpg_query, useful for query deduplication.
/// </summary>
public readonly struct QueryFingerprint
{
    /// <summary>64-bit numeric fingerprint.</summary>
    public ulong Value { get; }

    /// <summary>Human-readable canonical form (implementation-defined by libpg_query).</summary>
    public string Text { get; }

    public QueryFingerprint(ulong value, string text)
    {
        Value = value;
        Text = text;
    }

    public override string ToString() => $"{Text} ({Value})";
}

/// <summary>
/// Thin, allocation-conscious .NET wrapper around <c>libpg_query</c>.
/// </summary>
public static class Parser
{
    // -------- Parse (Protobuf) --------

    /// <summary>
    /// Parse a <b>NUL-terminated</b> UTF-8 SQL buffer with explicit mode and flags.
    /// </summary>
    /// <exception cref="ArgumentException">If the span is not NUL-terminated.</exception>
    /// <exception cref="PgQueryException">When libpg_query reports a parse error.</exception>
    public static ParseResult Parse(ReadOnlySpan<byte> sqlUtf8, ParseMode mode = ParseMode.Default, ParserFlags flags = ParserFlags.None)
    {
        unsafe
        {
            return Parse(EnsureNullTerminated(sqlUtf8, nameof(sqlUtf8)), mode, flags);
        }
    }

    /// <summary>Parse managed SQL with explicit mode and flags.</summary>
    public static ParseResult Parse(string sql, ParseMode mode = ParseMode.Default, ParserFlags flags = ParserFlags.None)
    {
        using var temp = new TempUtf8(sql, nameof(sql));
        unsafe
        {
            return Parse(temp.Ptr, mode, flags);
        }
    }

    private static unsafe ParseResult Parse(byte* sqlUtf8, ParseMode mode, ParserFlags flags)
    {
        var res = Native.pg_query_parse_protobuf_opts(sqlUtf8, BuildOptionBits(mode, flags));
        CheckError(res.error);
        var bytes = new ReadOnlySpan<byte>(res.parse_tree.data, checked((int)res.parse_tree.len));
        return ParseResult.Parser.ParseFrom(bytes);
    }

    // -------- Normalize --------

    /// <summary>Normalize a managed SQL string.</summary>
    public static string Normalize(string sql)
    {
        using var temp = new TempUtf8(sql, nameof(sql));
        unsafe
        {
            using var res = Native.pg_query_normalize(temp.Ptr);
            CheckError(res.error);
            return Utf8(res.normalized_query) ?? string.Empty;
        }
    }

    /// <summary>Normalize utility commands from a managed SQL string.</summary>
    public static string NormalizeUtility(string sql)
    {
        using var temp = new TempUtf8(sql, nameof(sql));
        unsafe
        {
            using var res = Native.pg_query_normalize_utility(temp.Ptr);
            CheckError(res.error);
            return Utf8(res.normalized_query) ?? string.Empty;
        }
    }

    // -------- Fingerprint --------

    /// <summary>
    /// Compute a stable fingerprint with explicit mode/flags for a <b>NUL-terminated</b> UTF-8 SQL buffer.
    /// </summary>
    public static QueryFingerprint Fingerprint(ReadOnlySpan<byte> sqlUtf8, ParseMode mode = ParseMode.Default, ParserFlags flags = ParserFlags.None)
    {
        int opts = BuildOptionBits(mode, flags);
        unsafe
        {
            using var res = Native.pg_query_fingerprint_opts(EnsureNullTerminated(sqlUtf8, nameof(sqlUtf8)), opts);
            CheckError(res.error);
            return new QueryFingerprint(res.fingerprint, Utf8(res.fingerprint_str) ?? "");
        }
    }

    /// <summary>Compute a stable fingerprint of managed SQL with explicit mode/flags.</summary>
    public static QueryFingerprint Fingerprint(string sql, ParseMode mode = ParseMode.Default, ParserFlags flags = ParserFlags.None)
    {
        if (sql is null) throw new ArgumentNullException(nameof(sql));
        int opts = BuildOptionBits(mode, flags);
        using var temp = new TempUtf8(sql, nameof(sql));
        unsafe
        {
            using var res = Native.pg_query_fingerprint_opts(temp.Ptr, opts);
            CheckError(res.error);
            return new QueryFingerprint(res.fingerprint, Utf8(res.fingerprint_str) ?? "");
        }
    }

    // -------- Split --------

    /// <summary>
    /// Split a <b>NUL-terminated</b> UTF-8 SQL buffer into statement spans using the chosen algorithm.
    /// </summary>
    public static StatementSpan[] Split(ReadOnlySpan<byte> sqlUtf8, SplitAlgorithm algorithm = SplitAlgorithm.Scanner)
    {
        unsafe
        {
            var p = EnsureNullTerminated(sqlUtf8, nameof(sqlUtf8));
            using var res = algorithm == SplitAlgorithm.Parser
                ? Native.pg_query_split_with_parser(p)
                : Native.pg_query_split_with_scanner(p);

            CheckError(res.error);
            var result = new StatementSpan[res.n_stmts];
            for (int i = 0; i < res.n_stmts; i++)
            {
                var item = res.stmts[i];
                if (item != null)
                {
                    result[i] = new StatementSpan(item->stmt_location, item->stmt_len);
                }
            }

            return result;
        }
    }

    /// <summary>Split a managed SQL string into statement spans.</summary>
    public static string[] Split(string sql, SplitAlgorithm algorithm = SplitAlgorithm.Scanner)
    {
        using var temp = new TempUtf8(sql, nameof(sql));
        unsafe
        {
            using var res = algorithm == SplitAlgorithm.Parser
                ? Native.pg_query_split_with_parser(temp.Ptr)
                : Native.pg_query_split_with_scanner(temp.Ptr);

            CheckError(res.error);
            var result = new string[res.n_stmts];
            for (int i = 0; i < res.n_stmts; i++)
            {
                var start = temp.Ptr + res.stmts[i]->stmt_location;
                result[i] = Marshal.PtrToStringUTF8((IntPtr)start, res.stmts[i]->stmt_len) ?? "";
            }

            return result;
        }
    }

    // -------- Scan (tokens protobuf) --------

    /// <summary>
    /// Run the scanner and return the parsed protobuf model describing tokens.
    /// </summary>
    /// <remarks>
    /// Uses the generated <see cref="ScanResult"/> parser to deserialize the protobuf payload
    /// returned by <c>libpg_query</c>. Native memory is freed before returning.
    /// </remarks>
    /// <exception cref="PgQueryException">When libpg_query reports an error.</exception>
    public static ScanResult Scan(ReadOnlySpan<byte> sqlUtf8)
    {
        unsafe
        {
            return Scan(EnsureNullTerminated(sqlUtf8, nameof(sqlUtf8)));
        }
    }

    /// <summary>
    /// Run the scanner for a managed SQL string and return the parsed protobuf model.
    /// </summary>
    /// <exception cref="PgQueryException">When libpg_query reports an error.</exception>
    public static ScanResult Scan(string sql)
    {
        using var temp = new TempUtf8(sql, nameof(sql));
        unsafe
        {
            return Scan(temp.Ptr);
        }
    }

    private static unsafe ScanResult Scan(byte* sqlUtf8)
    {
        using var res = Native.pg_query_scan(sqlUtf8);
        CheckError(res.error);
        var bytes = new ReadOnlySpan<byte>(res.pbuf.data, checked((int)res.pbuf.len));
        return ScanResult.Parser.ParseFrom(bytes);
    }

    // ======== Private helpers ========

    private readonly unsafe struct TempUtf8 : IDisposable
    {
        public byte* Ptr { get; init; }

        public TempUtf8(string s, string? paramName = null)
        {
            if (s is null) throw new ArgumentNullException(paramName);
            Ptr = (byte*)Marshal.StringToCoTaskMemUTF8(s);
        }

        public void Dispose()
        {
            if (Ptr != null)
            {
                Marshal.FreeCoTaskMem((IntPtr)Ptr);
            }
        }
    }

    private static unsafe byte* EnsureNullTerminated(ReadOnlySpan<byte> sqlUtf8, string paramName)
    {
        if (sqlUtf8.Length == 0 || sqlUtf8[^1] != 0)
            throw new ArgumentException("input must be UTF-8 and NUL-terminated (last byte == 0x00)", paramName);
        fixed (byte* p = sqlUtf8)
        {
            return p;
        }
    }

    private static int BuildOptionBits(ParseMode mode, ParserFlags flags)
    {
        // Native packs mode in the low 4 bits; flags occupy higher bits.
        const int modeBitmask = (1 << 4) - 1; // 0b1111
        return ((int)mode & modeBitmask) | (int)flags;
    }

    private static unsafe string? Utf8(byte* s) => s == null ? null : Marshal.PtrToStringUTF8((IntPtr)s);

    private static unsafe void CheckError(Native.PgQueryError* err)
    {
        if (err != null) throw MakeException(err);
    }

    private static unsafe PostgresException MakeException(Native.PgQueryError* err)
        => new(
            message: Utf8(err->message) ?? "unknown error",
            function: Utf8(err->funcname),
            file: Utf8(err->filename),
            line: err->lineno,
            cursorPosition: err->cursorpos,
            context: Utf8(err->context)
        );
}


