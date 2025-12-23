using System;
using System.Net.WebSockets;

using Pagemgr;
using Tilemgr;
using Handler;

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
