using System;
using System.Net.WebSockets;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.FileProviders;

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

if(!Directory.Exists("uploads")){
	Directory.CreateDirectory("uploads");
}

var path = Path.Combine("uploads", "palettes");
if(!Directory.Exists(path))
{
	Directory.CreateDirectory(path);
}
path = Path.Combine("uploads", "canvas");
if(!Directory.Exists(path))
{
	Directory.CreateDirectory(path);
}

app.UseDefaultFiles();
app.UseStaticFiles();

var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
app.UseStaticFiles(new StaticFileOptions
		{
		FileProvider = new PhysicalFileProvider(uploadsPath),
		RequestPath = "/uploads"
		});
app.UseWebSockets();

app.MapGet("/projects", (HttpContext c, CancellationToken cToken, PageManager<Project> mgr) => {
		var projects = ProjectHandler.Handle(c, cToken, mgr);
		var html = @"<!DOCTYPE html>
		<html lang=""en"">
		<head>
		<meta charset=""UTF-8"">
		<title>Projects</title>
		<link rel=""stylesheet"" href=""/css/project.css"">
			</head>
			<body>
			<div class=""container"">
			<h1>Projects</h1>
			";


		foreach (var p in projects)
		{
			html += @$"
				<div class=""project"">
				<div class=""header"">
				<a href=""{p.path}/"">
				<h3>{Path.GetFileName(p.name)}</h3>
				</a>
				</div>
				<div class=""details"">
				<div>Tile size: {p.TileWid} × {p.TileHei} px</div>
				<div>Project size: {p.Wid} × {p.Hei} tiles</div>
				<div>Created: {p.CreationDate}</div>
				</div>
				</div>
				";
		}

		html += @"</div>
			</body>
			</html>";
		return Results.Content(html, "text/html");
		});

app.MapGet("/projects/new", () => {
		return Results.File("new_project.html", "text/html");
});

app.MapPost("/projects/new", async (HttpRequest request, PageManager<Project> mgr) =>
	{
		var form = await request.ReadFormAsync();

		var name = form["name"].ToString();
		var image = form.Files["image"];
		if(image == null || image.Length == 0)
		{
			return Results.BadRequest("Tilesheet required.");
		}
		var root = Path.Combine("uploads", "palettes");
		var palette_path = Path.Combine(root, $"{name}_atlas.png");
		using var stream = new FileStream(palette_path, FileMode.Create);
		await image.CopyToAsync(stream);

		var palette = new Palette(palette_path,
				int.Parse(form["t_wid"].ToString()),
				int.Parse(form["t_hei"].ToString()));

		root = Path.Combine("uploads", "canvas");
		var canvas = new Canvas(Path.Combine(root, $"{name}_canvas.bin"),
				int.Parse(form["wid"].ToString()),
				int.Parse(form["hei"].ToString()));

		var p = new Project(canvas, name, palette);
		var p_context = Project.Save(p);

		return Results.Redirect($"/projects/{p_context.lookup}");
	});

app.MapGet("/projects/{hash}/", (string hash, PageManager<Project> mgr) => {
		var page = mgr.GetOrCreate(hash);
		if(page.Data == null)
		{
			return Results.NotFound();
		}
		return Results.File("editor.html", "text/html");
		}
);

app.MapGet("/projects/{hash}/ws", async (string hash, HttpContext c, CancellationToken cToken, PageManager<Project> mgr) => await ProjectHandler.Handle(hash, c, cToken, mgr));

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
