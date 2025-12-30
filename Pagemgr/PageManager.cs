using System.Net.WebSockets;
using System.Collections.Concurrent;
using Tilemgr;

namespace Pagemgr;

public class PageManager<T> where T : ILoadable<T>
{
	private readonly ConcurrentDictionary<string, Page<T>> _pages = new();

	public bool TryGet(string key, out Page<T>? p) => _pages.TryGetValue(key, out p);
	public Page<T> GetOrCreate(string hash)
	{
		T? obj = T.Load(new Context(hash));
		return _pages.GetOrAdd(hash, new Page<T>(obj));
	}
}
