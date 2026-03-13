const Connection = (function () {
	const ws = new WebSocket(location.pathname + "ws");
	ws.addEventListener("message", (event) => {
		Parser.parse(event.data);
	})

	function send_update(e) {
		const rect = viewport.getBoundingClientRect();
		mouse.x = Math.floor((e.clientX - rect.left) / (wid * scale.x));
		mouse.y = Math.floor((e.clientY - rect.top) / (hei * scale.y));
		if(!erase) {
			ws.send(JSON.stringify({x: mouse.x, y: mouse.y, tile: tile_selected}));
		} else {
			ws.send(JSON.stringify({x: mouse.x, y: mouse.y, tile: 0}));
		}
	}

	return {
		send_update
	};
})();

const Parser = (function () {
	let stage = 0;

	function decompress(compressed) {
		let prologue = compressed.indexOf("\n".charCodeAt(0));
		prologue++;
		const header = new TextDecoder("latin1").decode(compressed.slice(0, prologue));
		const [width,height] = header.split(" ").map(Number);
		const decompressed = Array.from({ length: height }, () => new Uint8Array(width));
		let repeats = 0;
		let element = 0;
		let wid = 0;
		let hei = 0;
		for (let i = prologue; i + 1 < compressed.length; i += 2) {
			repeats = compressed[i];
			element = compressed[i + 1];
			for (let j = 0; j < repeats; j++) {
				decompressed[hei][wid] = element;
				wid += 1;
				if (wid >= width) {
					wid = 0;
					hei += 1;
				}
				if (hei >= height) {
					return decompressed;
				}
			}
		}
		return decompressed;
	}

	function parse(msg) {
		var update = JSON.parse(msg)
		switch(stage){
		case 0:
			let compressed = new Uint8Array(update.compressed);
			Canvas.restore(update, decompress(compressed));
			stage++
			break
		case 1:
			Canvas.load_atlas(update);
			stage++
			break
		default:
			Canvas.update(ctx, tilemap, update.x, update.y, update.tile);
			Canvas.clear(selector_ctx);
			Canvas.draw_selector(selector_ctx);
		}
	}

	return {
		parse
	};
})();

const Canvas = (function () {
	const BACKGROUND = "#101010";
	const atlas = new Image();
	let frames = null

	function update(ctx, map, x, y, tile) {
		map[y][x] = tile
		clear(ctx);
		draw(ctx, map);
	}

	function load_atlas(msg) {
		frames = msg.frames.slice()
		atlas.src = "/" + msg.ImgPath
		const sidebar = document.getElementById("palette-container")
		palette.width = sidebar.clientWidth
		palette.height = sidebar.clientHeight
		atlas.onload = () => {
			clear(ctx);
			draw(ctx, tilemap);
			draw_selector(selector_ctx)
		}
	}

	function restore(canvas, decompressed) {
		cols = canvas.Wid
		rows = canvas.Hei
		wid = canvas.TileWid
		hei = canvas.TileHei
		tilemap = decompressed;
		if(wid <= WID_MIN || hei <= HEI_MIN) {
			scale.x = WID_MIN/wid
			scale.y = HEI_MIN/hei
		}
		viewport.width = wid * scale.x * cols;
		viewport.height = hei * scale.y * rows;
	}

	function clear(ctx) {
		ctx.fillStyle = BACKGROUND;
		ctx.fillRect(0, 0, ctx.canvas.width, ctx.canvas.height);
	}

	function draw(ctx, map) {
		if(map == null) {
			return
		}
		map.forEach( (row, i) => {
			row.forEach( (col, j) => {
				if(col == 0 || col > frames.length) {
					return
				}
				let scaled = {x: wid * scale.x, y: hei * scale.y}
				ctx.drawImage(
					atlas,
					frames[col-1].x, frames[col-1].y, wid, hei,
					j * scaled.x, i * scaled.y, scaled.x, scaled.y
				)
			})
		})
	}

	function draw_selector(ctx) {
		if(frames == null)
		{
			return
		}
		const tileW = wid * scale.x / 2
		const tileH = hei * scale.y / 2
		const tilesPerRow = Math.floor(ctx.canvas.width / tileW);
		const rowsNeeded = Math.ceil(frames.length / tilesPerRow);
		ctx.canvas.height = rowsNeeded * tileH;
		ctx.clearRect(0, 0, ctx.canvas.width, ctx.canvas.height);
		col = 0
		row = 0
		frames.forEach( (frame, i) => {
			ctx.drawImage(
				atlas,
				frame.x, frame.y, wid, hei,
				col * tileW, row * tileH, tileW, tileH
			)
			col++;
			if(col >= tilesPerRow)
			{
				row++
				col = 0
			}
		});
	}

	return {
		update,
		load_atlas,
		restore,
		clear,
		draw,
		draw_selector
	};
})();

const WID_MIN = 128
const HEI_MIN = 128
const FPS = 60
const ctx = viewport.getContext("2d")
const selector_ctx = palette.getContext("2d")

let mouse = {x: 0, y: 0, down: false}
let tile_selected = 1
let stage = 0
let wid = 16
let hei = 16
let cols = 0
let rows = 0
let scale = {x: 1, y: 1}
var tilemap = null

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
	const rect = palette.getBoundingClientRect();
	const containerRect = document.getElementById("palette-container").getBoundingClientRect();
	const scrollTop = document.getElementById("palette-container").scrollTop;
	const tileW = wid * scale.x / 2;
	const tileH = hei * scale.y / 2;
	const tilesPerRow = Math.floor(palette.width / tileW);
	
	// Calculate position relative to canvas, accounting for scroll
	const x = e.clientX - rect.left;
	const y = (e.clientY - containerRect.top) + scrollTop;

	const tileX = Math.floor(x / tileW);
	const tileY = Math.floor(y / tileH);

	const idx = tileX + tileY * tilesPerRow;
	if(e.button === 0) {
		if(idx < 0 || idx >= frames.length) {
			return;
		}
		tile_selected = idx + 1;
	}
})
