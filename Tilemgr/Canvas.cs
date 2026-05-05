using System;
using System.IO;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tilemgr;

[Owned]
public class Canvas
{
	public byte[] Compressed { get; set; }
	[NotMapped]
	public byte[,] DrawableLayer { get; set; }

	public int GetLength(byte[] compressed)
	{
		int count;
		for(count = Array.IndexOf(compressed, (byte)'\n')+1; count < compressed.Length; count+=2)
		{
			if(compressed[count] == 0)
			{
				break;
			}
			count+=2;
		}
		return count;
	}

	public void Export(string filename)
	{
		byte[] compressed = compress(this.DrawableLayer);
		using(var f = File.Create(filename))
		{
			using(var writer = new BinaryWriter(f))
			{
				writer.Write(compressed, 0, this.GetLength(compressed));
			}
		}
	}

	public int GetHeight()
	{
		if(this.DrawableLayer == null) {
			this.DrawableLayer = Canvas.decompress(this.Compressed);
		}
		return this.DrawableLayer.GetLength(0);
	}

	public byte GetTile(int x, int y)
	{
		if(this.DrawableLayer == null) {
			this.DrawableLayer = Canvas.decompress(this.Compressed);
		}
		return this.DrawableLayer[y, x];
	}

	public int GetWidth()
	{
		if(this.DrawableLayer == null) {
			this.DrawableLayer = Canvas.decompress(this.Compressed);
		}
		return this.DrawableLayer.GetLength(1);
	}

	public (int x, int y, byte tile) UpdateTile(int x, int y, byte tile)
	{
		this.DrawableLayer[y,x] = tile;
		return (x, y, this.DrawableLayer[y,x]);
	}

	public static byte[,] decompress(byte[] compressed)
	{
		int header_length = Array.IndexOf(compressed, (byte)'\n');
		string header = Encoding.UTF8.GetString(compressed, 0, header_length);
		string[] fields = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		int wid_total = int.Parse(fields[0]);
		int hei_total = int.Parse(fields[1]);
		byte[,] decompressed = new byte[hei_total, wid_total];
		byte repeats = 0;
		byte element  = 0;
		int wid = 0;
		int hei = 0;
		for(var i = header_length+1; i+1 < compressed.Length; i+=2)
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

		int hei = decompressed.GetLength(0);
		int wid = decompressed.GetLength(1);

		string header = $"{wid} {hei}\n";
		var headerBytes = Encoding.UTF8.GetBytes(header);

		var prologue = headerBytes.Length;
		var max_size = (2 * hei * wid) + prologue;
		byte[] compressed = new byte[max_size];

		Buffer.BlockCopy(headerBytes, 0, compressed, 0, headerBytes.Length);

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
		if(this.DrawableLayer == null) {
			this.DrawableLayer = Canvas.decompress(this.Compressed);
		}
		return compress(this.DrawableLayer);
	}
}
