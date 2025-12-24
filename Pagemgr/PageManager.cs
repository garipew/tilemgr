using System.Net.WebSockets;
using System.Collections.Concurrent;
using Tilemgr;

namespace Pagemgr;

public class PageManager<T> where T : ILoadable<T>
{
	private readonly ConcurrentDictionary<string, Page<T>> _pages = new();

	public Page<T> GetOrCreate(string hash)
	{
		T? obj = T.Load(new Context(hash));
		return _pages.GetOrAdd(hash, new Page<T>(obj));
	}
}
