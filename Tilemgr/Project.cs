using System.Text;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tilemgr;

public record ProjectView(int TileWid, int TileHei, int Wid, int Hei, string name, string path, DateTime CreationDate, List<byte> compressed);

public class Project
{
	public int Id { get; set; }
	public string Hash { get; set; } = null!;
	public Canvas canvas { get; set; } = null!;
	public Palette palette { get; set; } = null!;
	public DateTime CreationDate { get; set; }
	public string ProjectName { get; set; } = null!;

	static private string HexToString(byte[] input)
	{
		StringBuilder output = new StringBuilder(input.Length);
		for (int i = 0; i < input.Length; i++)
		{
			output.Append(input[i].ToString("x2"));
		}
		return output.ToString();
	}

	static public string ComputeHash(Project p)
	{
		var date_bytes = Encoding.UTF8.GetBytes(p.CreationDate.ToString());
		var name_bytes = Encoding.UTF8.GetBytes(p.ProjectName);
		var all_bytes = date_bytes.Concat(name_bytes).ToArray();

		var hash = MD5.Create().ComputeHash(all_bytes);
		return HexToString(hash);
	}

	public ProjectView GetView(bool compress = false)
	{
		var t_wid = this.palette.TileWid;
		var t_hei = this.palette.TileHei;
		List<byte> compressed = new();
		if(compress)
		{
			var bytes = this.canvas.compress();
			compressed = bytes.AsSpan(0, this.canvas.GetLength(bytes)).ToArray().ToList();
		}
		var view = new ProjectView(
				t_wid, t_hei,
				canvas.GetWidth(), canvas.GetHeight(),
				ProjectName, "/projects/" + this.Hash,
				CreationDate, compressed);
		return view;
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
}
