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
