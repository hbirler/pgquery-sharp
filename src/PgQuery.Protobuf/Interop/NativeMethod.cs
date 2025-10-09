using System;
using System.Runtime.InteropServices;

namespace PgQuery.Interop;

internal static unsafe partial class Native
{
    private const string LibName = "pg_query"; // resolves per-OS

    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQueryError
    {
        public IntPtr message;     // char*
        public IntPtr funcname;    // char*
        public IntPtr filename;    // char*
        public int lineno;
        public int cursorpos;
        public IntPtr context;     // char*
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQueryProtobuf
    {
        public uint len;           // unsigned int
        public IntPtr data;        // char*
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQueryProtobufParseResult
    {
        public PgQueryProtobuf parse_tree; // serialized protobuf bytes
        public IntPtr stderr_buffer;       // char* (optional)
        public IntPtr error;               // PgQueryError*
    }

    [LibraryImport(LibName, EntryPoint = "pg_query_parse_protobuf_opts")]
    internal static partial PgQueryProtobufParseResult ParseProtobufOpts(byte* input, PgQueryParseMode mode);

    [LibraryImport(LibName, EntryPoint = "pg_query_free_protobuf_parse_result")]
    internal static partial void FreeProtobufParseResult(PgQueryProtobufParseResult result);

    [LibraryImport(LibName, EntryPoint = "pg_query_exit")]
    internal static partial void Exit();
}
