using PostgresQuery;

var sql = "SELECT 1 AS x";
// Preferred API: ReadOnlySpan<byte>dot
var ast = Parser.Parse(sql);

// Print a couple of fields from the protobuf model
Console.WriteLine($"Postgres version: {ast.Version}");
Console.WriteLine($"Statement count : {ast.Stmts.Count}");

// Show the first statement node type (if present)
if (ast.Stmts.Count > 0)
{
    var node = ast.Stmts[0].Stmt;
    Console.WriteLine($"First node kind : {node.NodeCase}");
}