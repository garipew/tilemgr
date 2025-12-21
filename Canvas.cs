using System;
using System.IO;

namespace Tilemgr;

public class Canvas
{
	private byte[,] DrawableLayer;
	private static byte Prologue = 4;

	public Canvas(int wid, int hei)
	{
		this.DrawableLayer = new byte[hei,wid];
	}

	public int GetHeight()
	{
		return this.DrawableLayer.GetLength(0);
	}

	public byte GetTile(int x, int y)
	{
		return this.DrawableLayer[y, x];
	}

	public int GetWidth()
	{
		return this.DrawableLayer.GetLength(1);
	}

	public (int x, int y, byte tile) UpdateTile(int x, int y, byte tile)
	{
		this.DrawableLayer[y,x] = tile;
		return (x, y, this.DrawableLayer[y,x]);
	}

	private byte[] compress()
	{
		byte count = 0;
		int last = this.DrawableLayer[0,0];

		int hei = this.DrawableLayer.GetLength(0);
		int wid = this.DrawableLayer.GetLength(1);

		int max_size = (2 * hei * wid) + Prologue;
		byte[] compressed = new byte[max_size];
		int current = Prologue;
		foreach(int tile in this.DrawableLayer)
		{
			if(last == tile && count < 0xff)
			{
				count++;
				continue;
			}
			compressed[current++] = count;
			compressed[current++] = (byte)last;
			last = tile;
			count = 1;

		}
		compressed[current++] = count;
		compressed[current++] = (byte)last;

		byte[] size = BitConverter.GetBytes(current);
		if(!BitConverter.IsLittleEndian)
		{
			Array.Reverse(size);
		}
		Buffer.BlockCopy(size, 0, compressed, 0, size.Length);
		return compressed;
	}

	public void Export(string filename)
	{
		byte[] compressed = compress();
		byte[] slice = compressed[0..4];
		if(!BitConverter.IsLittleEndian)
		{
			Array.Reverse(slice);
		}
		int size = BitConverter.ToInt32(slice, 0) - Prologue;
		using(var f = File.Create(filename))
		{
			using(var writer = new BinaryWriter(f))
			{
				writer.Write(compressed, Prologue, size);
			}
		}
	}
}
