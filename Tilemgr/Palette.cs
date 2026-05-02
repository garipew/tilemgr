using System.IO;
using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tilemgr;

public record PaletteView(string ImgPath, List<FrameView> frames);

[Owned]
public class Palette
{
	public string ImgPath { get; set; }
	public int TileWid { get; set; }
	public int TileHei { get; set; }

	private bool loaded;
	private Frame[]? _frames;
	[NotMapped]
	public Frame[]? frames {
		get
		{
			if(!loaded) {
				_frames = load_frames(this.ImgPath);
				loaded = true;
			}
			return _frames;
		}
	}


	public PaletteView GetView()
	{
		List<FrameView> list = new();
		foreach(var frame in frames ?? Array.Empty<Frame>())
		{
			list.Add(frame.GetView());
		}
		return new PaletteView(ImgPath, list);
	}

	private (int wid, int hei)? get_png_dimensions(string ImgPath)
	{
		int img_wid, img_hei;
		byte[] signature = {137, 80, 78, 71, 13, 10, 26, 10};
		byte[] ihdr = {0x49, 0x48, 0x44, 0x52};
		using(var img_stream = new FileStream(ImgPath, FileMode.Open, FileAccess.Read))
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

	private Frame[]? load_frames(string ImgPath)
	{
		var dimensions = get_png_dimensions(ImgPath);
		if(dimensions == null)
		{
			return null;
		}
		(int img_wid, int img_hei) = dimensions.Value;
		int count = (img_wid / TileWid) * (img_hei / TileHei);
		Frame[] frames = new Frame[count];
		int x = 0;
		int y = 0;
		for(int current = 0; current < count && y < img_hei; current++)
		{
			frames[current] = new Frame(x, y);
			x += TileWid;
			if(x >= img_wid)
			{
				x = 0;
				y += TileHei;
			}
		}
		return frames;
	}
}
