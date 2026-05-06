const tilemap_canvas = new Canvas(viewport.getContext("2d"));
const palette_canvas = new Palette(palette.getContext("2d"));

let mouse = {down: false}
let stage = 0

let erase = false
viewport.addEventListener("contextmenu", (e) => {
	e.preventDefault();
});

viewport.addEventListener("mousedown", (e) => {
	mouse.down = true
	erase = true
	if(e.button == 0) {
		erase = false
	}
	Connection.send_update(e)
})

viewport.addEventListener("mouseup", (e) => {
	mouse.down = false
})

viewport.addEventListener("mousemove", (e) => {
	if(!mouse.down) {
		return
	}
	Connection.send_update(e)
})

viewport.addEventListener("mouseleave", (e) => {
	mouse.down = false
})

palette.addEventListener("mousedown", (e) => {
	const tile = palette_canvas.get_tile_pos(e.clientX, e.clientY);

	const idx = tile.x + tile.y * palette_canvas.view.cols;
	if(e.button === 0) {
		if(idx < 0 || idx >= Canvas.frames.length) {
			return;
		}
		palette_canvas.tile_selected = idx + 1;
	}
})

palette.addEventListener("wheel", (e) => {
	let scroll = -0.5;
	if(e.deltaY > 0) {
		scroll = 0.5;
	}
	palette_canvas.scroll_vertical(scroll);
})

viewport.addEventListener("wheel", (e) => {
	let ratio = -0.5;
	if(e.deltaY > 0) {
		ratio = 0.5;
	}
	tilemap_canvas.zoom(ratio);
})

Connection.ws.addEventListener("message", (event) => {
	Parser.parse(event.data);
})
