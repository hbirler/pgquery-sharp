using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace PostgresQuery;

internal static unsafe partial class Native
{
    private const string LibName = "pg_query";

    // Postgres version information
    public const string PgMajorversion = "17";
    public const string PgVersion = "17.5";
    public const int PgVersionNum = 170005;

    // ---- Structs ----

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQueryError
    {
        public byte* message; // char*
        public byte* funcname; // char*
        public byte* filename; // char*
        public int lineno; // int
        public int cursorpos; // int
        public byte* context; // char*
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQueryProtobuf
    {
        public nuint len; // size_t
        public byte* data; // char*
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQueryScanResult : IDisposable
    {
        public PgQueryProtobuf pbuf;
        public byte* stderr_buffer; // char*
        public PgQueryError* error; // PgQueryError*
        
        public void Dispose()
        {
            pg_query_free_scan_result(this);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQueryParseResult : IDisposable
    {
        public byte* parse_tree; // char*
        public byte* stderr_buffer; // char*
        public PgQueryError* error; // PgQueryError*
        
        public void Dispose()
        {
            pg_query_free_parse_result(this);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQueryProtobufParseResult : IDisposable
    {
        public PgQueryProtobuf parse_tree;
        public byte* stderr_buffer; // char*
        public PgQueryError* error; // PgQueryError*
        
        public void Dispose()
        {
            pg_query_free_protobuf_parse_result(this);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQuerySplitStmt
    {
        public int stmt_location; // int
        public int stmt_len; // int
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQuerySplitResult : IDisposable
    {
        public PgQuerySplitStmt** stmts; // PgQuerySplitStmt**
        public int n_stmts; // int
        public byte* stderr_buffer; // char*
        public PgQueryError* error; // PgQueryError*

        public void Dispose()
        {
            pg_query_free_split_result(this);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQueryDeparseResult : IDisposable
    {
        public byte* query; // char*
        public PgQueryError* error; // PgQueryError*

        public void Dispose()
        {
            pg_query_free_deparse_result(this);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PostgresDeparseComment
    {
        public int match_location;           // int
        public int newlines_before_comment;  // int
        public int newlines_after_comment;   // int
        public byte* str;                    // char*
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQueryDeparseCommentsResult : IDisposable
    {
        public PostgresDeparseComment** comments; // PostgresDeparseComment**
        public nuint comment_count; // size_t
        public PgQueryError* error; // PgQueryError*

        public void Dispose()
        {
            pg_query_free_deparse_comments_result(this);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQueryPlpgsqlParseResult : IDisposable
    {
        public byte* plpgsql_funcs; // char*
        public PgQueryError* error; // PgQueryError*

        public void Dispose()
        {
            pg_query_free_plpgsql_parse_result(this);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQueryFingerprintResult : IDisposable
    {
        public ulong fingerprint; // uint64_t
        public byte* fingerprint_str; // char*
        public byte* stderr_buffer; // char*
        public PgQueryError* error; // PgQueryError*

        public void Dispose()
        {
            pg_query_free_fingerprint_result(this);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQueryNormalizeResult : IDisposable
    {
        public byte* normalized_query; // char*
        public PgQueryError* error; // PgQueryError*

        public void Dispose()
        {
            pg_query_free_normalize_result(this);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PgQuerySummaryParseResult : IDisposable
    {
        public PgQueryProtobuf summary;
        public byte* stderr_buffer; // char*
        public PgQueryError* error; // PgQueryError*

        public void Dispose()
        {
            pg_query_free_summary_parse_result(this);
        }
    }

    public enum PgQueryParseMode : int
    {
        PgQueryParseDefault = 0,
        PgQueryParseTypeName,
        PgQueryParsePlpgsqlExpr,
        PgQueryParsePlpgsqlAssign1,
        PgQueryParsePlpgsqlAssign2,
        PgQueryParsePlpgsqlAssign3
    }

    public static class PgQueryParserOptionBits
    {
        public const int PgQueryParseModeBits = 4;
        public const int PgQueryParseModeBitmask = ((1 << PgQueryParseModeBits) - 1);

        public const int PgQueryDisableBackslashQuote = 16;
        public const int PgQueryDisableStandardConformingStrings = 32;
        public const int PgQueryDisableEscapeStringWarning = 64;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PostgresDeparseOpts
    {
        public PostgresDeparseComment** comments; // PostgresDeparseComment**
        public nuint comment_count; // size_t

        // Pretty print options
        public byte pretty_print; // bool (use 0/1)
        public int indent_size; // int
        public int max_line_length; // int
        public byte trailing_newline; // bool (use 0/1)
        public byte commas_start_of_line; // bool (use 0/1)
    }

    // ---- Functions ----

    [LibraryImport(LibName, EntryPoint = "pg_query_normalize")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryNormalizeResult pg_query_normalize(byte* input);

    [LibraryImport(LibName, EntryPoint = "pg_query_normalize_utility")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryNormalizeResult pg_query_normalize_utility(byte* input);

    [LibraryImport(LibName, EntryPoint = "pg_query_scan")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryScanResult pg_query_scan(byte* input);

    [LibraryImport(LibName, EntryPoint = "pg_query_parse")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryParseResult pg_query_parse(byte* input);

    [LibraryImport(LibName, EntryPoint = "pg_query_parse_opts")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryParseResult pg_query_parse_opts(byte* input, int parserOptions);

    [LibraryImport(LibName, EntryPoint = "pg_query_parse_protobuf")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryProtobufParseResult pg_query_parse_protobuf(byte* input);

    [LibraryImport(LibName, EntryPoint = "pg_query_parse_protobuf_opts")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryProtobufParseResult pg_query_parse_protobuf_opts(byte* input, int parserOptions);

    [LibraryImport(LibName, EntryPoint = "pg_query_parse_plpgsql")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryPlpgsqlParseResult pg_query_parse_plpgsql(byte* input);

    [LibraryImport(LibName, EntryPoint = "pg_query_fingerprint")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryFingerprintResult pg_query_fingerprint(byte* input);

    [LibraryImport(LibName, EntryPoint = "pg_query_fingerprint_opts")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryFingerprintResult pg_query_fingerprint_opts(byte* input, int parserOptions);

    // Split
    [LibraryImport(LibName, EntryPoint = "pg_query_split_with_scanner")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQuerySplitResult pg_query_split_with_scanner(byte* input);

    [LibraryImport(LibName, EntryPoint = "pg_query_split_with_parser")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQuerySplitResult pg_query_split_with_parser(byte* input);

    // Deparse from protobuf
    [LibraryImport(LibName, EntryPoint = "pg_query_deparse_protobuf")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryDeparseResult pg_query_deparse_protobuf(PgQueryProtobuf parseTree);

    // NOTE: This takes PostgresDeparseOpts BY VALUE in C. Ensure PostgresDeparseOpts is defined EXACTLY per postgres_deparse.h
    [LibraryImport(LibName, EntryPoint = "pg_query_deparse_protobuf_opts")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryDeparseResult pg_query_deparse_protobuf_opts(PgQueryProtobuf parseTree,
        PostgresDeparseOpts opts);

    [LibraryImport(LibName, EntryPoint = "pg_query_deparse_comments_for_query")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQueryDeparseCommentsResult pg_query_deparse_comments_for_query(byte* query);

    [LibraryImport(LibName, EntryPoint = "pg_query_summary")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial PgQuerySummaryParseResult pg_query_summary(byte* input, int parserOptions, int truncateLimit);

    // Free functions
    [LibraryImport(LibName, EntryPoint = "pg_query_free_normalize_result")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pg_query_free_normalize_result(PgQueryNormalizeResult result);

    [LibraryImport(LibName, EntryPoint = "pg_query_free_scan_result")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pg_query_free_scan_result(PgQueryScanResult result);

    [LibraryImport(LibName, EntryPoint = "pg_query_free_parse_result")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pg_query_free_parse_result(PgQueryParseResult result);

    [LibraryImport(LibName, EntryPoint = "pg_query_free_split_result")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pg_query_free_split_result(PgQuerySplitResult result);

    [LibraryImport(LibName, EntryPoint = "pg_query_free_deparse_result")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pg_query_free_deparse_result(PgQueryDeparseResult result);

    [LibraryImport(LibName, EntryPoint = "pg_query_free_deparse_comments_result")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pg_query_free_deparse_comments_result(PgQueryDeparseCommentsResult result);

    [LibraryImport(LibName, EntryPoint = "pg_query_free_protobuf_parse_result")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pg_query_free_protobuf_parse_result(PgQueryProtobufParseResult result);

    [LibraryImport(LibName, EntryPoint = "pg_query_free_plpgsql_parse_result")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pg_query_free_plpgsql_parse_result(PgQueryPlpgsqlParseResult result);

    [LibraryImport(LibName, EntryPoint = "pg_query_free_fingerprint_result")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pg_query_free_fingerprint_result(PgQueryFingerprintResult result);

    [LibraryImport(LibName, EntryPoint = "pg_query_free_summary_parse_result")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pg_query_free_summary_parse_result(PgQuerySummaryParseResult result);

    // Optional cleanup
    [LibraryImport(LibName, EntryPoint = "pg_query_exit")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pg_query_exit();

    // Deprecated
    [LibraryImport(LibName, EntryPoint = "pg_query_init")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pg_query_init();
}
