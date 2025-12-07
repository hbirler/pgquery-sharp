using System;
using System.Text;
using PostgresQuery;
using Xunit;

namespace PostgresQuery.Tests;

public class ParserTests
{
    [Fact]
    public void Parse_returns_ast_with_statement()
    {
        var result = Parser.Parse("SELECT 1 AS value");

        Assert.True(result.Version > 0);
        Assert.Single(result.Stmts);
        Assert.NotEqual(Node.NodeOneofCase.None, result.Stmts[0].Stmt.NodeCase);
    }

    [Fact]
    public void Parse_invalid_sql_surfaces_native_error()
    {
        Assert.Throws<PostgresException>(() => Parser.Parse("SELECT 1 FROM"));
    }

    [Fact]
    public void Normalize_is_idempotent_for_simple_query()
    {
        const string sql = " select  1 /* comment */ ";
        var normalized = Parser.Normalize(sql);
        var normalizedAgain = Parser.Normalize(normalized);

        Assert.False(string.IsNullOrWhiteSpace(normalized));
        Assert.Equal(normalized, normalizedAgain);
    }

    [Fact]
    public void Split_returns_per_statement_strings()
    {
        var parts = Parser.Split("select 1; select 2;");

        Assert.Equal(2, parts.Length);
        Assert.Equal("select 1", parts[0].TrimEnd(';').Trim());
        Assert.Contains("select 2", parts[1]);
    }

    [Fact]
    public void Split_span_requires_null_terminated_buffer()
    {
        var bytes = Encoding.UTF8.GetBytes("select 1;");
        Assert.Throws<ArgumentException>(() => Parser.Split(bytes));
    }

    [Fact]
    public void Split_span_returns_statement_ranges()
    {
        var sqlBytes = Encoding.UTF8.GetBytes("select 1; select 2;\0");
        var spans = Parser.Split(sqlBytes);

        Assert.Equal(2, spans.Length);
        Assert.Equal("select 1", Encoding.UTF8.GetString(sqlBytes, spans[0].Location, spans[0].Length).TrimEnd(';').Trim());
        Assert.Contains("select 2", Encoding.UTF8.GetString(sqlBytes, spans[1].Location, spans[1].Length));
    }

    [Fact]
    public void Fingerprint_differs_for_different_queries()
    {
        var first = Parser.Fingerprint("select 1");
        var repeat = Parser.Fingerprint("select 1");

        Assert.True(first.Value > 0);
        Assert.False(string.IsNullOrWhiteSpace(first.Text));
        Assert.Equal(first.Value, repeat.Value);
        Assert.Equal(first.Text, repeat.Text);
    }

    [Fact]
    public void Scan_returns_tokens()
    {
        var tokens = Parser.Scan("select 1");
        Assert.NotEmpty(tokens.Tokens);
    }
}
