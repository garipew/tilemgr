using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace Tilemgr;

public class Project : ILoadable<Project>
{
	private Canvas canvas;
	private Palette? palette;
	private readonly DateTime CreationDate;
	private readonly string ProjectName;

	public Project(Canvas c, string name, Palette? p = null, DateTime d = default){
		this.canvas = c;
		this.ProjectName = name;
		this.palette = p;
		if(d == default)
		{
			this.CreationDate = DateTime.Now;
		}else{
			this.CreationDate = d;
		}
	}

	public static Project? Load(Context c)
	{
		using var conn = new SqliteConnection("Data Source=data.db");
		conn.Open();

		using var cmd = conn.CreateCommand();
		cmd.CommandText = "SELECT CanvasPath,PalettePath,TileWid,TileHei,CreationDate,ProjectName FROM Projects WHERE Hash = $lookup";
		cmd.Parameters.AddWithValue("$lookup", c.lookup);

		Project? p = null;
		try
		{
			using var reader = cmd.ExecuteReader();
			if(!reader.Read() ||
			reader.IsDBNull(0) ||
			reader.IsDBNull(1) ||
			reader.IsDBNull(2) ||
			reader.IsDBNull(3) ||
			reader.IsDBNull(4) ||
			reader.IsDBNull(5))
			{
				return null;
			}

			var canvas = Canvas.Load(new Context(reader.GetString(0)));
			var palette = Palette.Load(new Context(reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3)));
			if(canvas == null)
			{
				return null;
			}
			var date = DateTime.Parse(reader.GetString(4));
			p = new Project(canvas, reader.GetString(5), palette, date);
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
		// TODO(garipew): Decide on the actual layout of the table,
		// PalettePath, TileWid and TileHei can be all packed into a
		// single json "Palette" for instance. It's also possible to
		// also include the canvas dimensions in the DB, instead of
		// packing it on the binary? Though this would require changes
		// in other places.
		cmd.CommandText = "SELECT CanvasPath,PalettePath,TileWid,TileHei,CreationDate,ProjectName FROM Projects";

		List<Project> projects = new();
		try
		{
			using var reader = cmd.ExecuteReader();
			while(reader.Read())
			{
				if(!reader.Read() ||
				reader.IsDBNull(0) ||
				reader.IsDBNull(1) ||
				reader.IsDBNull(2) ||
				reader.IsDBNull(3) ||
				reader.IsDBNull(4) ||
				reader.IsDBNull(5))
				{
					continue;
				}

				var canvas = Canvas.Load(new Context(reader.GetString(0)));
				var palette = Palette.Load(new Context(reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3)));
				if(canvas == null)
				{
					continue;
				}
				var date = DateTime.Parse(reader.GetString(4));
				projects.Add(new Project(canvas, reader.GetString(5), palette, date));
			}
		} catch(Exception e){
			System.Console.WriteLine(e.ToString());
		}
		return projects;
	}

	private string HexToString(byte[] input)
	{
		StringBuilder output = new StringBuilder(input.Length);
		for (int i = 0; i < input.Length; i++)
		{
			output.Append(input[i].ToString("X2"));
		}
		return output.ToString();
	}

	public string Hash()
	{
		var date_bytes = Encoding.UTF8.GetBytes(this.CreationDate.ToString());
		var name_bytes = Encoding.UTF8.GetBytes(this.ProjectName);
		var all_bytes = date_bytes.Concat(name_bytes).ToArray();

		var hash = new MD5CryptoServiceProvider().ComputeHash(all_bytes);
		return HexToString(hash);
	}

	public static Context Save(Project p)
	{
		string hash = p.Hash();
		var c_context = Canvas.Save(p.canvas);
		var p_context = Palette.Save(p.palette);
		// TODO(garipew): Update a DB entry, possibly creating it.
		//
		// If the hash already exist, but ProjectName OR CreationDate
		// doesn't match, it's a collision. The way we load from the DB
		// has to reflect the way we handle collisions.
		var c = new Context(hash);

		using var conn = new SqliteConnection("Data Source=data.db");
		conn.Open();

		// TODO(garipew): Move this to startup, instead of repeating
		// at every Save. 
		using var create = conn.CreateCommand();
		create.CommandText = @" CREATE TABLE IF NOT EXISTS Projects (
					Hash varchar(255),
					CanvasPath varchar(255),
					PalettePath varchar(255),
					TileWid int,
					TileHei int,
					CreationDate datetime(default),
					ProjectName varchar(255))";
		create.ExecuteNonQuery();

		using var cmd = conn.CreateCommand();
		cmd.CommandText = "SELECT CreationDate,ProjectName FROM Projects WHERE Hash = $lookup";
		cmd.Parameters.AddWithValue("$lookup", c.lookup);

		SqliteDataReader reader;
		using var reader = cmd.ExecuteReader();
		if(!reader.Read())
		{
			// TODO(garipew): Entry does not exist, create
			// new.
		} else
		{
			// TODO(garipew): Entry does exist, check if
			// CreationDate and ProjectName match. If not, handle
			// collision, if so, update.
			//
			if(reader.IsDBNull(0) || reader.IsDBNull(1))
			{
				// Malformed
			}
			if(reader.GetDateTime(0).Equals(p.CreationDate) &&
				reader.GetString(0).Equals(p.ProjectName))
			{
				cmd.CommandText = @"
					UPDATE Projects
					SET CanvasPath = $c_path,
					SET PalettePath = $p_path";
				return c;
			}
		}

		return c;
	}

	public void ImportPalette(string filename, int TileWid, int TileHei)
	{
		this.palette = new Palette(filename, TileWid, TileHei);
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
