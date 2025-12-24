using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Tilemgr;

public class Project : ILoadable<Project>
{
	private Canvas canvas;
	private Palette? palette;

	public static Project? Load(Context c)
	{
		using var conn = new SqliteConnection("Data Source=data.db");
		conn.Open();

		using var cmd = conn.CreateCommand();
		cmd.CommandText = "SELECT c_path,p_path,t_wid,t_hei FROM projects WHERE hash = $lookup";
		cmd.Parameters.AddWithValue("$lookup", c.lookup);

		Project? p = null;
		try
		{
			using var reader = cmd.ExecuteReader();
			if(!reader.Read() || reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3))
			{
				return null;
			}

			var canvas = Canvas.Load(new Context(reader.GetString(0)));
			var palette = Palette.Load(new Context(reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3)));
			if(canvas == null)
			{
				return null;
			}
			p = new Project(canvas, palette);
		} catch(Exception e){
			System.Console.WriteLine(e.ToString());
		}
		return p;
	}

	public static List<Project> Load()
	{
		using var conn = new SqliteConnection("Data Source=data.db");
		conn.Open();
		
		using var cmd = conn.CreateCommand();
		cmd.CommandText = "SELECT c_path,p_path,t_wid,t_hei FROM projects";

		List<Project> projects = new();
		try
		{
			using var reader = cmd.ExecuteReader();
			while(reader.Read())
			{
				if(reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3))
				{
					continue;
				}

				var canvas = Canvas.Load(new Context(reader.GetString(0)));
				var palette = Palette.Load(new Context(reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3)));
				if(canvas == null)
				{
					continue;
				}
				projects.Add(new Project(canvas, palette));
			}
		} catch(Exception e){
			System.Console.WriteLine(e.ToString());
		}
		return projects;
	}

	public static void Save(Project p)
	{
		// TODO(garipew): This UPDATES a DB entry, this does NOT create
		// an entry.
		// This is where the hashing happens, every imutable property
		// of Project should be used (perhaps include more imutables
		// like 'CreationDate' and whatnot).
	}

	public Project(Canvas c, Palette? p){
		this.canvas = c;
		this.palette = p;
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
