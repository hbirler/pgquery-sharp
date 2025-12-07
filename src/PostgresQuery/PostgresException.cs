
namespace PostgresQuery;

/// <summary>
/// Exception thrown when <c>libpg_query</c> reports a parsing error.
/// Contains detailed context from the native <c>PgQueryError</c>.
/// </summary>
public sealed class PostgresException : Exception
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

    public PostgresException(
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