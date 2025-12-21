namespace Tilemgr;

public class Project
{
	private Canvas canvas;
	private Palette? palette;

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
