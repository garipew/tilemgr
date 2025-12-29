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
		<a href=/projects/new> New Project </a>";
		return Results.Content(html, "text/html");});

app.MapGet("/projects", (HttpContext c, CancellationToken cToken, PageManager<Project> mgr) => ProjectHandler.Handle(c, cToken, mgr));

app.MapGet("/projects/new", () => {
		var html = @"<!DOCTYPE html>
		<html>
		<head>
		</head>
		<body>
		    <h2>Project Creation</h2>
		    <form action=""/projects/new"" method=""post"" enctype=""multipart/form-data"">
			<input name=""name"" placeholder=""Project name"" value=""Default"" required>
			<br><br>
			<input name=""t_wid"" type=""number"" placeholder=""Tile Width (px)"" value=""16"" required>
			<br><br>
			<input name=""t_hei"" type=""number"" placeholder=""Tile Heigth (px)"" value=""16"" required>
			<br><br>
			<input name=""wid"" type=""number"" placeholder=""Columns (tiles)"" value=""10"" required>
			<br><br>
			<input name=""hei"" type=""number"" placeholder=""Rows (tiles)"" value=""10"" required>
			<br><br>
			<input name=""image"" type=""file"" accept=""image/png"" required>
			<br><br>
			<button type=""submit"">Create Project</button>
			<br><br>
		    </form>
		    <p><small>This form sends query parameters (name, wid, hei) via POST</small></p>
		</body>
		</html>
		";
		return Results.Content(html, "text/html");});

app.MapPost("/projects/new", async (HttpRequest request, PageManager<Project> mgr) =>
	{
		var form = await request.ReadFormAsync();

		var name = form["name"].ToString();
		var image = form.Files["image"];
		if(image == null || image.Length == 0)
		{
			return Results.BadRequest("Tilesheet required.");
		}
		using var stream = new FileStream($"uploads/{name}_atlas.png", FileMode.Create);
		await image.CopyToAsync(stream);

		var t_wid = int.Parse(form["t_wid"].ToString());
		var t_hei = int.Parse(form["t_hei"].ToString());
		var palette = new Palette($"uploads/{name}_atlas.png", t_wid, t_hei);

		var wid = int.Parse(form["wid"].ToString());
		var hei = int.Parse(form["hei"].ToString());
		var canvas = new Canvas($"uploads/{name}_canvas.bin", wid, hei);

		var p = new Project(canvas, name, palette);
		var p_context = Project.Save(p);
		var hash = p_context.lookup;

		return Results.Created($"/projects/{hash}", p.GetView());
	});

app.MapGet("/projects/{hash}", async (string hash, HttpContext c, CancellationToken cToken, PageManager<Project> mgr) => await ProjectHandler.Handle(hash, c, cToken, mgr));

// TODO(garipew): To share palettes across projects,
// add /palettes and /palettes/new endpoint.

app.Run();
