viewport.style.cursor = "pointer";
palette.style.cursor = "pointer";
const tilemap_canvas = new Canvas(viewport.getContext("2d"));
const palette_canvas = new Palette(palette.getContext("2d"));

let mouse = {down: false, grab: null}
let stage = 0

let erase = false
let scroll = false;
viewport.addEventListener("contextmenu", (e) => {
	e.preventDefault();
});

viewport.addEventListener("mousedown", (e) => {
	mouse.down = true
	scroll = e.button == 1;
	if(scroll) {
		mouse.grab = tilemap_canvas.get_tile_pos(e.clientX, e.clientY);
		viewport.style.cursor = "grabbing";
		return;
	}
	erase = e.button == 2;
	Connection.send_update(e)
})

viewport.addEventListener("mouseup", (e) => {
	mouse.down = false
	viewport.style.cursor = "pointer";
	mouse.grab = null;
})

viewport.addEventListener("mousemove", (e) => {
	if(!mouse.down) {
		return
	}
	if(scroll) {
		const mouse_pos = tilemap_canvas.get_tile_pos(e.clientX, e.clientY);

		tilemap_canvas.view.x -= mouse_pos.x - mouse.grab.x;

		if(tilemap_canvas.view.x < 0) {
			tilemap_canvas.view.x = 0;
		} else if(tilemap_canvas.view.x + tilemap_canvas.view.cols >= tilemap_canvas.cols) {
			tilemap_canvas.view.x = tilemap_canvas.cols - tilemap_canvas.view.cols;
		}

		tilemap_canvas.view.y -= mouse_pos.y - mouse.grab.y;

		if(tilemap_canvas.view.y < 0) {
			tilemap_canvas.view.y = 0;
		} else if(tilemap_canvas.view.y + tilemap_canvas.view.rows >= tilemap_canvas.rows) {
			tilemap_canvas.view.y = tilemap_canvas.rows - tilemap_canvas.view.rows;
		}
		tilemap_canvas.clear();
		tilemap_canvas.draw();
		return;
	}
	Connection.send_update(e)
})

viewport.addEventListener("mouseleave", (e) => {
	mouse.down = false
})

viewport.addEventListener("wheel", (e) => {
	let ratio = -1;
	if(e.deltaY > 0) {
		ratio = 1;
	}
	tilemap_canvas.zoom(ratio);
})

palette.addEventListener("mousedown", (e) => {
	const tile = palette_canvas.get_tile_pos(e.clientX, e.clientY);

	const idx = tile.x + tile.y * palette_canvas.view.cols;
	if(e.button === 0) {
		if(idx < 0 || idx >= Canvas.frames.length) {
			return;
		}
		palette_canvas.tile_selected = idx + 1;
		palette_canvas.clear();
		palette_canvas.draw();
	}
})

palette.addEventListener("wheel", (e) => {
	let scroll = -0.5;
	if(e.deltaY > 0) {
		scroll = 0.5;
	}
	palette_canvas.scroll_vertical(scroll);
})

Connection.ws.addEventListener("message", (event) => {
	Parser.parse(event.data);
})
