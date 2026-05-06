class Connection {
	static ws = new WebSocket(location.pathname + "ws");

	static send_update(e) {
		const tile = tilemap_canvas.get_tile_pos(e.clientX, e.clientY);
		let msg = {x: tile.x, y: tile.y, tile: palette_canvas.tile_selected};
		if(erase) {
			msg.tile = 0;
		}
		msg = JSON.stringify(msg);
		Connection.ws.send(msg);
	}
}
