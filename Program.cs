using System;
using System.Net.WebSockets;
using Microsoft.Data.Sqlite;

using Pagemgr;
using Tilemgr;
using Handler;

using var conn = new SqliteConnection("Data Source=data.db");
conn.Open();

using var create = conn.CreateCommand();
// TODO(garipew): Update properties of columns,
// varchar -> TEXT,
// Hash should be PRIMARY,
// Name should be UNIQUE (this would also result on Paths being unique, since
// they are created using name).
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
		string output_root = "uploads";
		if(!Directory.Exists(output_root))
		{
			Directory.CreateDirectory(output_root);
		}
		var palette_path = Path.Combine(output_root, $"{name}_atlas.png");
		using var stream = new FileStream(palette_path, FileMode.Create);
		await image.CopyToAsync(stream);

		var palette = new Palette(palette_path,
				int.Parse(form["t_wid"].ToString()),
				int.Parse(form["t_hei"].ToString()));

		var canvas = new Canvas(Path.Combine(output_root, $"{name}_canvas.bin"),
				int.Parse(form["wid"].ToString()),
				int.Parse(form["hei"].ToString()));

		var p = new Project(canvas, name, palette);
		var p_context = Project.Save(p);

		return Results.Created($"/projects/{p_context.lookup}", p.GetView());
	});

app.MapGet("/projects/{hash}", async (string hash, HttpContext c, CancellationToken cToken, PageManager<Project> mgr) => await ProjectHandler.Handle(hash, c, cToken, mgr));

app.MapGet("/projects/{hash}/export", (string hash, HttpContext c, PageManager<Project> mgr) =>
	{
		Page<Project>? page = null;
		Project? p = null;
		mgr.TryGet(hash, out page);
		if(page != null)
		{
			p = page.Data; // <- This is recoverable, project could exist on db
		}

		p ??= Project.Load(new Context(hash)); // <- This is unrecoverable.
		if(p == null)
		{
			return Results.NotFound($"Project {hash} does not exist.");
		}
		Canvas.Save(p.canvas);
		var stream = File.OpenRead(p.canvas.Name);
		return Results.File(stream,
				"application/octet-stream",
				fileDownloadName: $"{p.ProjectName}_canvas.bin");
	});

// TODO(garipew): To share palettes across projects,
// add /palettes and /palettes/new endpoint.

app.Run();
