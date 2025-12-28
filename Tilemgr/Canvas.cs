using System;
using System.IO;

namespace Tilemgr;

public class Canvas : ILoadable<Canvas>
{
	public byte[,] DrawableLayer;
	private static readonly byte _prologue = 8;
	public readonly string Name;

	public Canvas(Int32 wid, Int32 hei, string name)
	{
		this.DrawableLayer = new byte[hei,wid];
		this.Name = name;
	}

	public Canvas(byte[,] drawable, string name)
	{
		this.DrawableLayer = drawable;
		this.Name = name;
	}


	public bool IsEqual(Object? obj)
	{
		if(obj == null || !(obj is Canvas))
		{
			return false;
		}
		Canvas b = (Canvas)obj;
		var wid = this.DrawableLayer.GetLength(1);
		var hei = this.DrawableLayer.GetLength(0);
		if(b.DrawableLayer.GetLength(0) != hei || b.DrawableLayer.GetLength(1) != wid)
		{
			return false;
		}
		for(int i = 0; i < hei; i++)
		{
			for(int j = 0; j < wid; j++)
			{
				if(this.DrawableLayer[i, j] != b.DrawableLayer[i, j])
				{
					return false;
				}
			}
		}
		return true;
	}

	public static Canvas? Load(Context c)
	{
		if(!File.Exists(c.lookup))
		{
			return null;
		}
		byte[] compressed = File.ReadAllBytes(c.lookup);
		return new Canvas(decompress(compressed), c.lookup);
	}

	public static Context Save(Canvas obj)
	{
		var compressed = Canvas.compress(obj.DrawableLayer);
		File.WriteAllBytes(obj.Name, compressed);
		return new Context(obj.Name);
	}

	public Int32 GetHeight()
	{
		return this.DrawableLayer.GetLength(0);
	}

	public byte GetTile(Int32 x, Int32 y)
	{
		return this.DrawableLayer[y, x];
	}

	public Int32 GetWidth()
	{
		return this.DrawableLayer.GetLength(1);
	}

	public (Int32 x, Int32 y, byte tile) UpdateTile(Int32 x, Int32 y, byte tile)
	{
		this.DrawableLayer[y,x] = tile;
		return (x, y, this.DrawableLayer[y,x]);
	}

	public static byte[,] decompress(byte[] compressed)
	{
		byte[,] decompressed = new byte[BitConverter.ToInt32(compressed, 4), BitConverter.ToInt32(compressed, 0)];
		byte repeats = 0;
		byte element  = 0;
		Int32 wid = 0;
		Int32 hei = 0;
		for(var i = _prologue; i+1 < compressed.Length; i+=2)
		{
			repeats = compressed[i];
			element = compressed[i+1];
			for(byte j = 0; j < repeats; j++)
			{
				decompressed[hei, wid] = element;
				wid += 1;
				if(wid >= decompressed.GetLength(1))
				{
					wid = 0;
					hei += 1;
				}
				if(hei >= decompressed.GetLength(0))
				{
					return decompressed;
				}
			}
		}
		return decompressed;
	}

	public static byte[] compress(byte[,] decompressed)
	{
		byte count = 0;
		byte last = decompressed[0,0];

		Int32 hei = decompressed.GetLength(0);
		Int32 wid = decompressed.GetLength(1);

		byte[] wid_bytes = BitConverter.GetBytes(wid);
		byte[] hei_bytes = BitConverter.GetBytes(hei);
		if(!BitConverter.IsLittleEndian)
		{
			Array.Reverse(wid_bytes);
			Array.Reverse(hei_bytes);
		}

		var prologue = wid_bytes.Length + hei_bytes.Length;
		var max_size = (2 * hei * wid) + prologue;
		byte[] compressed = new byte[max_size];

		Buffer.BlockCopy(wid_bytes, 0, compressed, 0, wid_bytes.Length);
		Buffer.BlockCopy(hei_bytes, 0, compressed, wid_bytes.Length, hei_bytes.Length);

		var current = prologue;
		foreach(byte tile in decompressed)
		{
			if(last == tile && count < 0xff)
			{
				count++;
				continue;
			}
			compressed[current++] = count;
			compressed[current++] = last;
			last = tile;
			count = 1;

		}
		compressed[current++] = count;
		compressed[current++] = last;

		return compressed;
	}

	public byte[] compress()
	{
		return compress(this.DrawableLayer);
	}

	public void Export(string filename)
	{
		byte[] compressed = compress(this.DrawableLayer);
		using(var f = File.Create(filename))
		{
			using(var writer = new BinaryWriter(f))
			{
				writer.Write(compressed, 0, compressed.Length);
			}
		}
	}
}
