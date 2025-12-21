using System;
using System.Net.WebSockets;
using Tilemgr;

if(args.Length < 1)
{
	System.Console.WriteLine("Missing argument");
	return;
}
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

var projectHandler = new ProjectHandler(10, 10);
projectHandler.project.ImportPalette(args[0], 16, 16);
projectHandler.project.report();

app.Map("/projects", projectHandler.Handle);

// TODO(garipew): Rota /new que é responsável por criar um novo projeto e
// então redirecionar até ele.

// TODO(garipew): Rota / que apresenta opções "Browse Projects" e
// "Create new".
// Como cada opção tem sua rota específica, talvez não tenha necessidade de
// uma rota / aqui?
app.Run();
