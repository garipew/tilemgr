namespace Tilemgr;

public class Context
{
	public string lookup;
	public int? TileWid;
	public int? TileHei;

	public Context(string l)
	{
		lookup = l;
	}

	public Context(string l, int? w, int? h)
	{
		lookup = l;
		TileWid = w;
		TileHei = h;
	}
}

public interface ILoadable<TSelf>
	where TSelf : ILoadable<TSelf>
{
	static abstract TSelf? Load(Context c);
	static abstract Context Save(TSelf obj);
}
