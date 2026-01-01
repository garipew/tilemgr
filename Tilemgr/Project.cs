using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace Tilemgr;

public record ProjectView(int TileWid, int TileHei, int Wid, int Hei, string name, string path, DateTime CreationDate);

public class Project : ILoadable<Project>
{
	public Canvas canvas;
	public Palette palette;
	private readonly DateTime CreationDate;
	public readonly string ProjectName;

	public Project(Canvas c, string name, Palette p, DateTime d = default){
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
			if(canvas == null || palette == null)
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

	public static List<ProjectView> Load()
	{
		using var conn = new SqliteConnection("Data Source=data.db");
		conn.Open();
		
		using var cmd = conn.CreateCommand();
		cmd.CommandText = "SELECT CanvasPath,PalettePath,TileWid,TileHei,CreationDate,ProjectName FROM Projects";

		List<ProjectView> projects = new();
		try
		{
			using var reader = cmd.ExecuteReader();
			while(reader.Read())
			{
				if(reader.IsDBNull(0) ||
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
				if(canvas == null || palette == null)
				{
					continue;
				}
				var date = DateTime.Parse(reader.GetString(4));
				var p = new Project(canvas, reader.GetString(5), palette, date);
				projects.Add(p.GetView());
			}
		} catch(Exception e){
			System.Console.WriteLine(e.ToString());
		}
		return projects;
	}

	public ProjectView GetView()
	{
		var t_wid = palette.TileWid;
		var t_hei = palette.TileHei;
		var view = new ProjectView(t_wid, t_hei, canvas.GetWidth(), canvas.GetHeight(), ProjectName, "/projects/" + this.Hash(), CreationDate);
		return view;
	}

	private string HexToString(byte[] input)
	{
		StringBuilder output = new StringBuilder(input.Length);
		for (int i = 0; i < input.Length; i++)
		{
			output.Append(input[i].ToString("x2"));
		}
		return output.ToString();
	}

	public string Hash()
	{
		var date_bytes = Encoding.UTF8.GetBytes(this.CreationDate.ToString());
		var name_bytes = Encoding.UTF8.GetBytes(this.ProjectName);
		var all_bytes = date_bytes.Concat(name_bytes).ToArray();

		var hash = MD5.Create().ComputeHash(all_bytes);
		return HexToString(hash);
	}

	private static void Create(Project p, string hash, Context canvas, Context palette, SqliteConnection conn)
	{
		// TODO(garipew): To share tilesheets across projects, 
		// remove TileWid, TileHei and PalettePath
		// from here, substitute it to palette_id.
		using var insert = conn.CreateCommand();
		insert.CommandText = @"INSERT INTO Projects
			(Hash, CanvasPath, PalettePath, TileWid, TileHei, CreationDate, ProjectName)
			VALUES ($hash, $c_path, $p_path, $wid, $hei, $date, $name)";
		insert.Parameters.AddWithValue("$hash", hash);
		insert.Parameters.AddWithValue("$c_path", canvas.lookup);
		insert.Parameters.AddWithValue("$p_path", palette.lookup);
		insert.Parameters.AddWithValue("$wid", palette.TileWid ?? -1);
		insert.Parameters.AddWithValue("$hei", palette.TileHei ?? -1);
		insert.Parameters.AddWithValue("$date", p.CreationDate);
		insert.Parameters.AddWithValue("$name", p.ProjectName);
		insert.ExecuteNonQuery();
	}

	private static void Update(Project p, string hash, Context canvas, Context palette, SqliteConnection conn)
	{
		// TODO(garipew): To share tilesheets across projects, 
		// remove TileWid, TileHei and PalettePath
		// from here, substitute it to palette_id.
		using var update = conn.CreateCommand();
		update.CommandText = @"
			UPDATE Projects
			SET CreationDate = $date,
			ProjectName = $name,
			CanvasPath = $c_path,
			PalettePath = $p_path,
			TileWid = $wid,
			TileHei = $hei
			WHERE Hash = $hash";
		update.Parameters.AddWithValue("$date", p.CreationDate);
		update.Parameters.AddWithValue("$name", p.ProjectName);
		update.Parameters.AddWithValue("$p_path", palette.lookup);
		update.Parameters.AddWithValue("$wid", p.palette.TileWid);
		update.Parameters.AddWithValue("$hei", p.palette.TileHei);
		update.Parameters.AddWithValue("$c_path", canvas.lookup);
		update.Parameters.AddWithValue("$hash", hash);
		update.ExecuteNonQuery();
	}

	public static bool QueryPerfectMatch(Project p, Context c, SqliteConnection conn)
	{
		using var perfect = conn.CreateCommand();
		perfect.CommandText = @"SELECT * FROM Projects
			WHERE Hash = $lookup AND
			CreationDate = $date AND
			ProjectName = $name";
		perfect.Parameters.AddWithValue("$lookup", c.lookup);
		perfect.Parameters.AddWithValue("$date", p.CreationDate);
		perfect.Parameters.AddWithValue("$name", p.ProjectName);

		using var reader = perfect.ExecuteReader();
		return reader.Read();
	}

	public static Context Save(Project p)
	{
		string hash = p.Hash();
		var c_context = Canvas.Save(p.canvas);
		// TODO(garipew): To share palettes across projects,
		// update this to a claim.
		// Only include the palette id on "Palettes" table.
		Context p_context = Palette.Save(p.palette);
		var c = new Context(hash);

		using var conn = new SqliteConnection("Data Source=data.db");
		conn.Open();

		for(int tries = 0; tries < 2; tries++)
		{
			if(QueryPerfectMatch(p, c, conn))
			{
				Update(p, c.lookup, c_context, p_context, conn);
				return c;
			}
			using var partial = conn.CreateCommand();
			partial.CommandText = @"SELECT ProjectName,CreationDate
				FROM Projects
				WHERE Hash = $lookup";
			partial.Parameters.AddWithValue("$lookup", c.lookup);

			using var reader = partial.ExecuteReader();
			if(!reader.Read())
			{
				// No match.
				break;
			}
			c.lookup += p.ProjectName;
		}

		Create(p, c.lookup, c_context, p_context, conn);
		return c;
	}

	public void ImportPalette(string filename, int TileWid, int TileHei)
	{
		this.palette = new Palette(filename, TileWid, TileHei);
	}

	public int countPalette()
	{
		if(this.palette.frames == null)
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
		var frames = this.palette.frames;
		if(frames == null)
		{
			return null;
		}
		if(tile < 0 || tile > (byte)frames.Length)
		{
			return (x, y, this.canvas.GetTile(x, y));
		}
		return this.canvas.UpdateTile(x, y, tile);
	}

	public void report()
	{
		System.Console.WriteLine($"Canvas dimensions: {this.canvas.GetWidth()}, {this.canvas.GetHeight()}");
		if(this.palette.frames == null)
		{
			return;
		}
		System.Console.WriteLine($"Palette tiles: {this.palette.frames.Length}");
	}
}
