using System;
using System.Text;
using System.Text.Json;
using System.Net.WebSockets;

namespace Tilemgr;

public record UpdateMessage(int x, int y, byte tile);

public class ProjectHandler
{
	private List<WebSocket> connections;
	// TODO(garipew): O projeto deveria poder ser null, e ao tentar
	// interagir com essa rota, redirecionar ate /new caso o projeto não
	// exista. Claro, isso tem algumas implicações. Talvez a rota /projects
	// deveria listar todos os projetos, dando a opção de se conectar em um
	// deles. E cada projeto está em /projects/{hash} ou algo do tipo.
	// Ou seja, no fundo esse handler deveria ser algo completamente
	// diferente, ou, no minimo remapeado. Esse handler é para projetos
	// específicos (project ainda deve ser null, alguns links simplesmente
	// não existem) e depois preciso de um "ListProjectsHandler.cs", que
	// implica em DBs...
	// Basicamente, preciso descobrir como receber argumentos da rota
	// mapeada, /projects/{arg}.
	//
	// TLDR: 2 rotas, /projects que lista TODOS os projetos do DB e
	// /projects/{hash} que é o canal de comunicação via WebSockets. 
	// Antes de tudo (depois do upgrade), recupera o projeto ESPECIFICO no
	// DB com a hash, caso não exista ou reencaminha para /new ou 404.
	public Project project;

	public ProjectHandler(int wid, int hei)
	{
		this.project = new Project(wid, hei);
		this.connections = new List<WebSocket>();
	}

	private async Task<(byte[],int)> ReceiveAll(WebSocket ws)
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

	private async Task BroadcastMessage(byte[] msg)
	{
		foreach(var con in this.connections)
		{
			await con.SendAsync(msg,
					WebSocketMessageType.Text,
					endOfMessage: true,
					CancellationToken.None);
		}
	}

	public async Task Handle(HttpContext c, CancellationToken cToken)
	{
		if(!c.WebSockets.IsWebSocketRequest)
		{
			c.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
			return;
		}
		var ws = await c.WebSockets.AcceptWebSocketAsync();
		this.connections.Add(ws);
		while(!cToken.IsCancellationRequested)
		{
			if(ws.State == WebSocketState.Closed || ws.State == WebSocketState.CloseSent)
			{
				break;
			}
			if(ws.State == WebSocketState.CloseReceived)
			{
				await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed",
							CancellationToken.None);
				break;
			}
			(byte[] msg, int len) = await this.ReceiveAll(ws);
			if(len == 0)
			{
				continue;
			}

			UpdateMessage? update;
			try{
				update = JsonSerializer.Deserialize<UpdateMessage>(msg.AsSpan(0, len));
			} catch (JsonException e) {
				System.Console.WriteLine(e.ToString());
				continue;
			}
			if(update == null)
			{
				continue;
			}
			var confirmed_update = this.project.RegisterUpdate(update.x, update.y, update.tile);
			if(confirmed_update == null)
			{
				continue;
			}
			(int x, int y, byte tile) = confirmed_update.Value;
			var result_msg = new UpdateMessage(x, y, tile);
			var result = JsonSerializer.SerializeToUtf8Bytes(result_msg);
			await this.BroadcastMessage(result);
		}
		this.connections.Remove(ws);
	}
}
