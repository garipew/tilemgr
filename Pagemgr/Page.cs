using System.Net.WebSockets;
using System.Collections.Concurrent;

using Tilemgr;

namespace Pagemgr;

public class Page
{
	public Project? Data;
	public readonly string Hash;
	private readonly ConcurrentDictionary<Guid, Client> _clients = new();

	public IEnumerable<Client> Clients => _clients.Values;
	public bool IsEmpty => _clients.IsEmpty;

	public Page(Project? data, string hash)
	{
		Data = data;
		Hash = hash;
	}

	public bool Connect(Client c)
	{
		return _clients.TryAdd(c.Id, c);
	}

	public bool Disconnect(Client c)
	{
		Client? v;
		_clients.TryGetValue(c.Id, out v);
		if(v == c)
		{
			return _clients.TryRemove(c.Id, out v);
		}
		return false;
	}
}
