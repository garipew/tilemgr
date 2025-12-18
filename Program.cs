using System;
using Tilemgr;

if(args.Length < 1)
{
	System.Console.WriteLine("Missing argument");
	return;
}
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var project = new Project(10, 10);
project.ImportPalette(args[0], 16, 16);
project.report();

app.MapGet("/", () => "Hello World!");

app.Run();
