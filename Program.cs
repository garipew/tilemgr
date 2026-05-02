using System;
using System.Text.Json;
using System.Net.WebSockets;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

using Pagemgr;
using Tilemgr;
using Handler;
using Data;

///////////////////////////////
///	Server setup
///////////////////////////////

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContextFactory<ProjectContext>(options =>
	{
		options.UseSqlite("Data Source=projects.db");
	});
builder.Services.AddSingleton<PageManager>();

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

///////////////////////////////
///	Endpoints
///////////////////////////////

app.UseDefaultFiles();
app.UseStaticFiles();

var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
app.UseStaticFiles(new StaticFileOptions
		{
		FileProvider = new PhysicalFileProvider(uploadsPath),
		RequestPath = "/uploads"
		});
app.UseWebSockets();

app.MapGet("/projects", () => {
		return Results.File("projects.html", "text/html");
});

app.MapGet("/projects/list", (HttpContext c, CancellationToken cToken, PageManager mgr) => {
		using var ctx = new ProjectContext();
		List<Project> projs = ctx.Projects.AsEnumerable().ToList();
		var views = new List<ProjectView>();
		foreach(var page in mgr.Pages) {
			if(page.Data == null) {
				continue;
			}
			projs.Add(page.Data);
		}

		foreach(var p in projs) {
			views.Add(p.GetView());
		}
		var json = JsonSerializer.Serialize(views);
		return Results.Content(json, "application/json");
		});

app.MapGet("/projects/new", () => {
		return Results.File("new_project.html", "text/html");
});

app.MapPost("/projects/new", async (HttpRequest request, PageManager mgr) =>
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

		var palette = new Palette();
		palette.ImgPath = palette_path;
		palette.TileWid = int.Parse(form["t_wid"].ToString());
		palette.TileHei = int.Parse(form["t_hei"].ToString());

		root = Path.Combine("uploads", "canvas");
		var canvas = new Canvas();
		canvas.Name = Path.Combine(root, $"{name}_canvas.bin");
		canvas.DrawableLayer = new byte[int.Parse(form["wid"].ToString()), int.Parse(form["hei"].ToString())];

		var p = new Project();
		p.canvas = canvas;
		p.ProjectName = name;
		p.palette = palette;
		p.CreationDate = DateTime.Now;
		p.Hash = Project.ComputeHash(p);

		var page = new Page(p, p.Hash);
		if(!mgr.TryAdd(p.Hash, page)) {
			// TODO(garipew): Solve hash conflict
			return Results.Content("Conflict", "text/html");
		}

		return Results.Redirect($"/projects/{p.Hash}/");
	});

app.MapGet("/projects/{hash}/", (string hash, PageManager mgr) => {
		var page = mgr.GetOrCreate(hash);
		if(page.Data == null)
		{
			return Results.NotFound();
		}
		return Results.File("editor.html", "text/html");
		}
);

app.MapGet("/projects/{hash}/ws", async (string hash, HttpContext c, CancellationToken cToken, PageManager mgr) => await ProjectHandler.Handle(hash, c, cToken, mgr));

app.MapGet("/projects/{hash}/export", (string hash, HttpContext c, PageManager mgr) =>
	{
		Page? page = null;
		mgr.TryGet(hash, out page);
		Project? proj = null;
		if(page != null) {
			proj = page.Data;
		}

		if(proj == null) {
			using var ctx = new ProjectContext();
			proj = ctx.Projects.Where(p => p.Hash == hash).FirstOrDefault();
		}

		if(proj == null)
		{
			return Results.NotFound($"Project {hash} does not exist.");
		}

		var stream = File.OpenRead(proj.canvas.Name);
		return Results.File(proj.canvas.compress(),
				"application/octet-stream",
				fileDownloadName: $"{proj.ProjectName}_canvas.bin");
	});

app.Run();
