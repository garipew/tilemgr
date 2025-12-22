using System;
using System.Net.WebSockets;

namespace Pagemgr;

public class Client
{
	public Guid Id = Guid.NewGuid();
	public WebSocket ws;

	public Client(WebSocket ws)
	{
		this.ws = ws;
	}
}
