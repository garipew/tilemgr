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
app.MapGet("/projects/{hash}", async (string hash, HttpContext c, CancellationToken cToken, PageManager<Project> mgr) => await ProjectHandler.Handle(hash, c, cToken, mgr));

app.MapGet("/new", () => {
		var html = @"<!DOCTYPE html>
		<html>
		<head>
		    <style>
			body { font-family: sans-serif; padding: 20px; }
			form { max-width: 400px; }
			input, button {
			    width: 100%;
			    padding: 10px;
			    margin: 5px 0;
			    box-sizing: border-box;
			}
			button { background: #007bff; color: white; border: none; cursor: pointer; }
			button:hover { background: #0056b3; }
		    </style>
		</head>
		<body>
		    <h2>Project Creation</h2>
		    <form action=""/new"" method=""post"">
			<input name=""name"" placeholder=""Project name"" value=""Test Project"" required>
			<input name=""wid"" type=""number"" placeholder=""Width"" value=""10"" required>
			<input name=""hei"" type=""number"" placeholder=""Height"" value=""10"" required>
			<button type=""submit"">Create Project</button>
		    </form>
		    <p><small>This form sends query parameters (name, wid, hei) via POST</small></p>
		</body>
		</html>
		";
		return Results.Content(html, "text/html");});

app.MapPost("/new", async (HttpRequest request, PageManager<Project> mgr) =>
	{
		var form = await request.ReadFormAsync();
		var name = form["name"].ToString();
		var wid = int.Parse(form["wid"].ToString());
		var hei = int.Parse(form["hei"].ToString());

		var canvas = new Canvas(wid, hei, name + ".canvas");
		var p = new Project(canvas, name);
		var p_context = Project.Save(p);
		var hash = p_context.lookup;

		return Results.Created($"/projects/{hash}", p.GetView());
	});

// TODO(garipew): Route to upload tilesheet, responsible for creating a palette
// to an existing project. So, maybe
// /projects/{hash}/new_palette ?
// or even just
// /projects/{hash} also handles POSTs.

app.Run();
