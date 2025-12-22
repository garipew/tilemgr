using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Tilemgr;

public class Project : ILoadable<Project>
{
	private Canvas canvas;
	private Palette? palette;

	public static Project? Pull(string hash)
	{
		using var conn = new SqliteConnection("Data Source=data.db");
		conn.Open();
		
		using var cmd = conn.CreateCommand();
		// TODO(garipew): Writing the query requires to actually
		// know the layout of the table, I have to figure it out. After
		// that, it is as simple as:
		// cmd.CommandText = "SELECT cols FROM table WHERE hash = $hash";
		// cmd.Parameters.AddWithValue("$hash", hash);

		// using var reader = cmd.ExecuteReader();
		// if(!reader.Read() || reader.IsDBNull(0) || reader.IsDBNull(n))
		// {
		// 	return null;
		// }

		return null;
	}

	public static void Pull()
	{
		// TODO(garipew): Return every single project metadata (not all
		// metadata, tilesheet is not needed here) stored on DB.
	}

	public static void Push(Project p)
	{
		// TODO(garipew): This UPDATES a DB entry, this does NOT create
		// an entry.
		// This is where the hashing happens, every imutable property
		// of Project should be used (perhaps include more imutables
		// like 'CreationDate' and whatnot).
	}

	public Project(int wid, int hei){
		this.canvas = new Canvas(wid, hei);
	}

	public void ImportPalette(string filename, int tile_wid, int tile_hei)
	{
		this.palette = new Palette(filename, tile_wid, tile_hei);
	}

	public int countPalette()
	{
		if(this.palette == null || this.palette.frames == null)
		{
			return 0;
		}
		return this.palette.frames.Length;
	}

	public (int x, int y, byte tile)? RegisterUpdate(int x, int y, byte tile)
	{
		if(y >= this.canvas.GetHeight() || y < 0)
		{
			return null;
		}
		if(x >= this.canvas.GetWidth() || x < 0)
		{
			return null;
		}
		if(this.palette == null)
		{
			return null;
		}
		var frames = this.palette.frames;
		if(frames == null)
		{
			return null;
		}
		if(tile < 0 || tile >= (byte)frames.Length)
		{
			return (x, y, this.canvas.GetTile(x, y));
		}
		return this.canvas.UpdateTile(x, y, tile);
	}

	public void report()
	{
		System.Console.WriteLine($"Canvas dimensions: {this.canvas.GetWidth()}, {this.canvas.GetHeight()}");
		if(this.palette == null || this.palette.frames == null)
		{
			return;
		}
		System.Console.WriteLine($"Palette tiles: {this.palette.frames.Length}");
	}
}
