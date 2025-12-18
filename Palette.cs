using System.IO;
using System.Buffers.Binary;

namespace Tilemgr;

public class Palette
{
	private string img_path;
	public Frame[]? frames;

	public Palette(string img_path, int tile_wid, int tile_hei)
	{
		this.img_path = img_path;
		this.frames = load_frames(img_path, tile_wid, tile_hei);
	}

	private (int wid, int hei)? get_png_dimensions(string img_path)
	{
		int img_wid, img_hei;
		byte[] signature = {137, 80, 78, 71, 13, 10, 26, 10};
		byte[] ihdr = {0x49, 0x48, 0x44, 0x52};
		using(var img_stream = new FileStream(img_path, FileMode.Open, FileAccess.Read))
		{
			using(var reader = new BinaryReader(img_stream))
			{
				byte[] buffer = reader.ReadBytes(8);
				if(!buffer.SequenceEqual(signature))
				{
					return null; 
				}
				reader.ReadBytes(4);
				byte[] ihdr_buffer = reader.ReadBytes(4);
				if(!ihdr_buffer.SequenceEqual(ihdr))
				{
					return null; 
				}
				byte[] wid_bytes = reader.ReadBytes(4);
				byte[] hei_bytes = reader.ReadBytes(4);
				img_wid = BinaryPrimitives.ReadInt32BigEndian(wid_bytes);
				img_hei = BinaryPrimitives.ReadInt32BigEndian(hei_bytes);
			}
		}
		return (img_wid, img_hei);
	}

	private Frame[]? load_frames(string img_path, int tile_wid, int tile_hei)
	{
		var dimensions = get_png_dimensions(img_path);
		if(dimensions == null)
		{
			return null;
		}
		(int img_wid, int img_hei) = dimensions.Value;
		int count = (img_wid / tile_wid) * (img_hei / tile_hei);
		Frame[] frames = new Frame[count];
		int x = 0;
		int y = 0;
		for(int current = 0; current < count && y < img_hei; current++)
		{
			frames[current] = new Frame(x, y, tile_wid, tile_hei);
			x += tile_wid;
			if(x >= img_wid)
			{
				x = 0;
				y += tile_hei;
			}
		}
		return frames;
	}
}
