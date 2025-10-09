using System;
using System.Runtime.InteropServices;
using PgQuery.Interop; // for Native
using PgQuery;         // for ParseResult.Parser (generated)

namespace PgQuery;

/// <summary>
/// Parsing mode for <c>libpg_query</c>. Controls which grammar or context the input is parsed with.
/// </summary>
/// <remarks>
/// These values mirror the <c>PgQueryParseMode</c> enum in <c>libpg_query</c>.
/// Use <see cref="PG_QUERY_PARSE_DEFAULT"/> for standard SQL. The PL/pgSQL modes
/// are intended for parsing PL/pgSQL snippets (expressions and assignments).
/// </remarks>
public enum PgQueryParseMode : int
{
    /// <summary>Default SQL parsing mode (typical SELECT/INSERT/... statements).</summary>
    PG_QUERY_PARSE_DEFAULT = 0,

    /// <summary>Parse a type name (e.g., <c>int4</c>, <c>text</c>, qualified types).</summary>
    PG_QUERY_PARSE_TYPE_NAME,

    /// <summary>Parse a PL/pgSQL expression (no trailing semicolon).</summary>
    PG_QUERY_PARSE_PLPGSQL_EXPR,

    /// <summary>Parse the first PL/pgSQL assignment form.</summary>
    PG_QUERY_PARSE_PLPGSQL_ASSIGN1,

    /// <summary>Parse the second PL/pgSQL assignment form.</summary>
    PG_QUERY_PARSE_PLPGSQL_ASSIGN2,

    /// <summary>Parse the third PL/pgSQL assignment form.</summary>
    PG_QUERY_PARSE_PLPGSQL_ASSIGN3,
}

/// <summary>
/// Thin .NET wrapper around the Protobuf-based parsing API exposed by <c>libpg_query</c>.
/// </summary>
/// <remarks>
/// - This wrapper calls <c>pg_query_parse_protobuf_opts</c> via source-generated interop
///   (<see cref="LibraryImportAttribute"/> in <see cref="Native"/>), then deserializes the
///   returned Protobuf bytes into the generated <see cref="ParseResult"/> model.
/// - Overloads accept either a null-terminated UTF-8 buffer (no extra copy) or a managed
///   <see cref="string"/> (marshaled to a null-terminated UTF-8 buffer with
///   <see cref="Marshal.StringToCoTaskMemUTF8(string)"/>).
/// - The returned object graph is fully managed; native memory from <c>libpg_query</c>
///   is released before this method returns.
/// </remarks>
public static class Parser
{
    /// <summary>
    /// Parses a <b>null-terminated</b> UTF-8 SQL buffer with the default SQL parsing mode.
    /// </summary>
    /// <param name="sqlUtf8">
    /// A <see cref="ReadOnlySpan{T}"/> of UTF-8 bytes that <b>must include a trailing NUL (0x00)</b>.
    /// No copy is made; the span is pinned for the duration of the P/Invoke call.
    /// </param>
    /// <returns>The parsed Protobuf AST as a managed <see cref="ParseResult"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the span is not null-terminated.</exception>
    /// <exception cref="PgQueryException">Thrown when <c>libpg_query</c> reports a parse error.</exception>
    /// <remarks>
    /// Use this when you already have a null-terminated UTF-8 buffer (e.g., from interop code)
    /// and want to avoid allocating/copying.
    /// </remarks>
    public static ParseResult Parse(ReadOnlySpan<byte> sqlUtf8)
        => Parse(sqlUtf8, PgQueryParseMode.PG_QUERY_PARSE_DEFAULT);

    /// <summary>
    /// Parses a <b>null-terminated</b> UTF-8 buffer with an explicit <see cref="PgQueryParseMode"/>.
    /// </summary>
    /// <param name="sqlUtf8">
    /// A <see cref="ReadOnlySpan{T}"/> of UTF-8 bytes that <b>must include a trailing NUL (0x00)</b>.
    /// </param>
    /// <param name="mode">The parsing mode to apply (SQL, PL/pgSQL expression, type name, etc.).</param>
    /// <returns>The parsed Protobuf AST as a managed <see cref="ParseResult"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the span is not null-terminated.</exception>
    /// <exception cref="PgQueryException">Thrown when <c>libpg_query</c> reports a parse error.</exception>
    /// <remarks>
    /// The buffer is pinned only for the native call and never stored. The native result
    /// is freed regardless of success/failure.
    /// </remarks>
    public static ParseResult Parse(ReadOnlySpan<byte> sqlUtf8, PgQueryParseMode mode)
    {
        if (sqlUtf8.Length == 0 || sqlUtf8[^1] != 0) throw new ArgumentException("input not null-terminated", nameof(sqlUtf8));
        unsafe
        {
            fixed (byte* p = sqlUtf8)
            {
                return ParseCore(p, mode);
            }
        }
    }

    /// <summary>
    /// Parses a managed <see cref="string"/> as SQL using the default SQL parsing mode.
    /// </summary>
    /// <param name="sql">SQL text (managed). Must be valid for the selected mode.</param>
    /// <returns>The parsed Protobuf AST as a managed <see cref="ParseResult"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> is <c>null</c>.</exception>
    /// <exception cref="PgQueryException">Thrown when <c>libpg_query</c> reports a parse error.</exception>
    /// <remarks>
    /// The string is marshaled to a null-terminated UTF-8 buffer using
    /// <see cref="Marshal.StringToCoTaskMemUTF8(string)"/> and freed with
    /// <see cref="Marshal.FreeCoTaskMem(IntPtr)"/>.
    /// </remarks>
    public static ParseResult Parse(string sql)
        => Parse(sql, PgQueryParseMode.PG_QUERY_PARSE_DEFAULT);

    /// <summary>
    /// Parses a managed <see cref="string"/> as SQL with an explicit <see cref="PgQueryParseMode"/>.
    /// </summary>
    /// <param name="sql">SQL text (managed). Must be valid for the selected mode.</param>
    /// <param name="mode">The parsing mode to apply (SQL, PL/pgSQL expression, type name, etc.).</param>
    /// <returns>The parsed Protobuf AST as a managed <see cref="ParseResult"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> is <c>null</c>.</exception>
    /// <exception cref="PgQueryException">Thrown when <c>libpg_query</c> reports a parse error.</exception>
    /// <remarks>
    /// This overload avoids intermediate managed allocations and guarantees a null-terminated UTF-8
    /// buffer, which is required by <c>libpg_query</c>.
    /// </remarks>
    public static ParseResult Parse(string sql, PgQueryParseMode mode)
    {
        if (sql is null) throw new ArgumentNullException(nameof(sql));
        IntPtr ptr = Marshal.StringToCoTaskMemUTF8(sql);
        try
        {
            unsafe
            {
                return ParseCore((byte*)ptr, mode);
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(ptr);
        }
    }

    /// <summary>
    /// Core native call path. Invokes <c>pg_query_parse_protobuf_opts</c> and materializes the managed AST.
    /// </summary>
    /// <param name="p">Pointer to a <b>null-terminated</b> UTF-8 SQL buffer.</param>
    /// <param name="mode">The parsing mode to apply.</param>
    /// <returns>The parsed Protobuf AST as a managed <see cref="ParseResult"/>.</returns>
    /// <exception cref="PgQueryException">Thrown when <c>libpg_query</c> reports a parse error.</exception>
    /// <remarks>
    /// On success and error, native allocations inside the result struct are released via
    /// <see cref="Native.FreeProtobufParseResult(PgQuery.Interop.Native.PgQueryProtobufParseResult)"/>.
    /// </remarks>
    private static unsafe ParseResult ParseCore(byte* p, PgQueryParseMode mode)
    {
        var res = Native.ParseProtobufOpts(p, mode);
        try
        {
            if (res.error != IntPtr.Zero)
            {
                // Copy strings out of native memory BEFORE we free the result.
                var err = (Native.PgQueryError*)res.error;

                static string? Utf8(IntPtr s) => s == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(s);

                var ex = new PgQueryException(
                    message: Utf8(err->message) ?? "unknown parse error",
                    function: Utf8(err->funcname),
                    file: Utf8(err->filename),
                    line: err->lineno,
                    cursorPosition: err->cursorpos,
                    context: Utf8(err->context)
                );

                throw ex;
            }

            if (res.parse_tree.len == 0 || res.parse_tree.data == IntPtr.Zero)
                return new ParseResult();

            var bytes = new ReadOnlySpan<byte>((void*)res.parse_tree.data, checked((int)res.parse_tree.len));
            return ParseResult.Parser.ParseFrom(bytes);
        }
        finally
        {
            // Always release native allocations associated with this result.
            Native.FreeProtobufParseResult(res);
        }
    }
}

/// <summary>
/// Exception thrown when <c>libpg_query</c> reports a parsing error.
/// Contains detailed context from the native <c>PgQueryError</c>.
/// </summary>
public sealed class PgQueryException : Exception
{
    /// <summary>The PostgreSQL/pg_query error message.</summary>
    public override string Message { get; }

    /// <summary>Function name associated with the error, if provided by the parser.</summary>
    public string? Function { get; }

    /// <summary>Source file associated with the error, if provided.</summary>
    public string? File { get; }

    /// <summary>Line number in the source file, if provided (0 if unknown).</summary>
    public int Line { get; }

    /// <summary>Cursor position within the SQL text (byte offset), if provided (0 if unknown).</summary>
    public int CursorPosition { get; }

    /// <summary>Additional error context string, if available.</summary>
    public string? Context { get; }

    public PgQueryException(
        string message,
        string? function = null,
        string? file = null,
        int line = 0,
        int cursorPosition = 0,
        string? context = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Message = message;
        Function = function;
        File = file;
        Line = line;
        CursorPosition = cursorPosition;
        Context = context;
    }

    /// <summary>Returns a multi-line, human-friendly description including all fields.</summary>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"{GetType().FullName}: {Message}");
        if (!string.IsNullOrEmpty(Context)) sb.Append($"\nContext: {Context}");
        if (!string.IsNullOrEmpty(Function)) sb.Append($"\nFunction: {Function}");
        if (!string.IsNullOrEmpty(File) || Line > 0)
        {
            sb.Append("\nLocation: ");
            if (!string.IsNullOrEmpty(File)) sb.Append(File);
            if (Line > 0) sb.Append($":{Line}");
        }
        if (CursorPosition > 0) sb.Append($"\nCursor: {CursorPosition}");
        if (InnerException is not null) sb.Append($"\nInnerException: {InnerException}");
        return sb.ToString();
    }
}

