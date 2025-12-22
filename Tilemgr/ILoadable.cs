namespace Tilemgr;

public interface ILoadable<TSelf>
	where TSelf : ILoadable<TSelf>
{
	static abstract TSelf? Pull(string hash);
	static abstract void Push(TSelf obj);
}
