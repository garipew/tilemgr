using System.Net.WebSockets;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

using Tilemgr;
using Data;

namespace Pagemgr;

public class PageManager
{
	private IDbContextFactory<ProjectContext> _contextFactory;

	private readonly ConcurrentDictionary<string, Page> _pages = new();

	public bool TryGet(string key, out Page? p) => _pages.TryGetValue(key, out p);
	public bool TryRemove(string key, out Page? p) => _pages.TryRemove(key, out p);
	public bool TryAdd(string key, Page p) => _pages.TryAdd(key, p);

	public IEnumerable<Page> Pages => _pages.Values;

	public PageManager(IDbContextFactory<ProjectContext> factory) {
		_contextFactory = factory;
	}

	public Page GetOrCreate(string hash)
	{
		Page? p;
		if(_pages.TryGetValue(hash, out p) && p != null) {
			return p;
		}
		using var context = _contextFactory.CreateDbContext();
		Project? proj = context.Projects.Where(p => p.Hash == hash).FirstOrDefault();
		if(proj != null) {
			proj.canvas.DrawableLayer = Canvas.decompress(proj.canvas.Compressed);
		}
		return _pages.GetOrAdd(hash, new Page(proj, hash));
	}

	public bool Connect(Page p, Client c)
	{
		return p.Connect(c);
	}

	public bool Disconnect(Page p, Client c)
	{
		Page? removed;

		if(p.Disconnect(c) && p.IsEmpty && p.Data != null) {
			p.Data.canvas.Compressed = p.Data.canvas.compress();
			using var context = _contextFactory.CreateDbContext();
			var proj = context.Projects.Where(pr => pr.Hash == p.Data.Hash).FirstOrDefault();
			if(proj == null) {
				context.Projects.Add(p.Data);
			} else {
				proj.canvas = p.Data.canvas;
				proj.palette = p.Data.palette;
			}
			context.SaveChanges();
			return _pages.TryRemove(p.Hash, out removed);
		}
		return false;
	}
}
