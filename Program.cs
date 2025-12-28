using System;
using System.Net.WebSockets;
using Microsoft.Data.Sqlite;

using Pagemgr;
using Tilemgr;
using Handler;

using var conn = new SqliteConnection("Data Source=data.db");
conn.Open();

using var create = conn.CreateCommand();
create.CommandText = @" CREATE TABLE IF NOT EXISTS Projects (
		Hash varchar(255),
		CanvasPath varchar(255),
		PalettePath varchar(255),
		TileWid int,
		TileHei int,
		CreationDate datetime DEFAULT CURRENT_TIMESTAMP,
		ProjectName varchar(255))";
create.ExecuteNonQuery();

if(args.Length < 1)
{
	System.Console.WriteLine("Missing argument");
	return;
}
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<PageManager<Project>>();

var app = builder.Build();

app.UseWebSockets();

app.MapGet("/", () => {
		var html = @"
		<a href=/projects> Browse Projects </a>
		<br>
		<a href=/new> New Project </a>";
		return Results.Content(html, "text/html");});

app.MapGet("/projects", (HttpContext c, CancellationToken cToken, PageManager<Project> mgr) => ProjectHandler.Handle(c, cToken, mgr));
app.MapGet("/projects/{hash}", (string hash, HttpContext c, CancellationToken cToken, PageManager<Project> mgr) => ProjectHandler.Handle(hash, c, cToken, mgr));

// TODO(garipew): Rota /new que é responsável por criar um novo projeto e
// então redirecionar até ele.

app.Run();
