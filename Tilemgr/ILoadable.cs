namespace Tilemgr;

public class Context
{
	public string lookup;
	public int? tile_wid;
	public int? tile_hei;

	public Context(string l)
	{
		lookup = l;
	}

	public Context(string l, int? w, int? h)
	{
		lookup = l;
		tile_wid = w;
		tile_hei = h;
	}
}

public interface ILoadable<TSelf>
	where TSelf : ILoadable<TSelf>
{
	static abstract TSelf? Load(Context c);
	static abstract void Save(TSelf obj);
}
