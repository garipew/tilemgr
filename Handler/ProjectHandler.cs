using System;
using System.Text;
using System.Text.Json;
using System.Net.WebSockets;
using Tilemgr;
using Pagemgr;

namespace Handler;

public record UpdateMessage(int x, int y, byte tile);

public static class ProjectHandler
{
	private static async Task<(byte[],int)> ReceiveAll(WebSocket ws)
	{
		int size = 256;
		byte[] msg = new byte[size];
		byte[] chunk = new byte[8];
		int current = 0;
		WebSocketReceiveResult result;
		do{
			result = await ws.ReceiveAsync(chunk, CancellationToken.None);
			if(result.MessageType == WebSocketMessageType.Close)
			{
				await ws.CloseAsync(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
						result.CloseStatusDescription,
						CancellationToken.None);
				break;
			}
			if(current + result.Count >= size)
			{
				size *= 2;
				Array.Resize(ref msg, size);
			}
			Array.Copy(chunk, 0, msg, current, result.Count);
			current += result.Count;
		} while(!result.EndOfMessage);
		return (msg,current);
	}

	private static async Task BroadcastMessage(byte[] msg, IEnumerable<Client> clients)
	{
		foreach(var client in clients)
		{
			await client.ws.SendAsync(msg,
					WebSocketMessageType.Text,
					endOfMessage: true,
					CancellationToken.None);
		}
	}
	public static List<ProjectView> Handle(HttpContext c, CancellationToken cToken, PageManager<Project> mgr)
	{
		var projects = Project.Load();
		return projects;
	}

	public static async Task Handle(string hash, HttpContext c, CancellationToken cToken, PageManager<Project> mgr)
	{
		var page = mgr.GetOrCreate(hash);
		if(page.Data == null)
		{
			c.Response.StatusCode = StatusCodes.Status404NotFound;
			return;
		}
		var project = page.Data;
		if(!c.WebSockets.IsWebSocketRequest)
		{
			c.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
			return;
		}

		var client = new Client(await c.WebSockets.AcceptWebSocketAsync());
		page.Connect(client);
		var greetings = JsonSerializer.SerializeToUtf8Bytes(project.GetView(compress: true));
		await client.ws.SendAsync(greetings,
				WebSocketMessageType.Text,
				endOfMessage: true,
				CancellationToken.None);
		var sheet_info = JsonSerializer.SerializeToUtf8Bytes(project.palette.GetView());
		await client.ws.SendAsync(sheet_info,
				WebSocketMessageType.Text,
				endOfMessage: true,
				CancellationToken.None);
		while(!cToken.IsCancellationRequested)
		{
			if(client.ws.State == WebSocketState.Closed || client.ws.State == WebSocketState.CloseSent)
			{
				break;
			}
			if(client.ws.State == WebSocketState.CloseReceived)
			{
				await client.ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed",
						CancellationToken.None);
				break;
			}
			(byte[] msg, int len) = await ReceiveAll(client.ws);
			if(len == 0)
			{
				continue;
			}

			UpdateMessage? update;
			try{
				update = JsonSerializer.Deserialize<UpdateMessage>(msg.AsSpan(0, len));
			} catch (JsonException e) {
				System.Console.WriteLine($"Make sure that tile <= 255.");
				System.Console.WriteLine(e.ToString());
				continue;
			}
			if(update == null)
			{
				continue;
			}
			var confirmed_update = project.RegisterUpdate(update.x, update.y, update.tile);
			if(confirmed_update == null)
			{
				continue;
			}
			(int x, int y, byte tile) = confirmed_update.Value;
			var result_msg = new UpdateMessage(x, y, tile);
			var result = JsonSerializer.SerializeToUtf8Bytes(result_msg);
			await BroadcastMessage(result, page.Clients);
		}
		page.Disconnect(client);
		if(page.IsEmpty)
		{
			Project.Save(project);
			mgr.TryRemove(hash, out page);
		}
	}
}
