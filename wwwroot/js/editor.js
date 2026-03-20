/*
 * TODO(garipew): Add viewport
 * 			- viewport should contain: cols, rows, x and y
 * 			- should handle navigation
 */
class Canvas {
	static BACKGROUND = "#101010";
	static atlas = new Image();
	static frames = null;
	static WID_MIN = 128;
	static HEI_MIN = 128;
	static actual_wid;
	static actual_hei;
	ctx;
	cols;
	rows;
	map;
	view_wid;
	view_hei;
	view;

	constructor(ctx) {
		this.ctx = ctx;
	}

	restore(canvas, decompressed) {
		this.cols = canvas.Wid
		this.rows = canvas.Hei

		Canvas.actual_wid = canvas.TileWid;
		Canvas.actual_hei = canvas.TileHei;

		this.view_wid = Canvas.WID_MIN;
		this.view_hei = Canvas.HEI_MIN;

		this.map = decompressed;

		this.ctx.canvas.width = this.view_wid * this.cols;
		this.ctx.canvas.height = this.view_hei * this.rows;
	}

	gen_selectormap(tilecount) {
		this.view_wid = Canvas.WID_MIN / 2;
		this.view_hei = Canvas.HEI_MIN / 2;

		const cols = Math.floor(this.ctx.canvas.width / this.view_wid);
		this.map = [];
		let tmp = [];
		for(let i = 1; i < tilecount; i++) {
			tmp.push(i);
			if(tmp.length == cols) {
				this.map.push(tmp);
				tmp = [];
			}
		}

		if(tmp.length > 0) {
			this.map.push(tmp);
		}
		this.cols = cols;
		this.rows = this.map.length;

		this.ctx.canvas.width = this.cols * this.view_wid;
		this.ctx.canvas.height = this.rows * this.view_hei;
	}

	static load_atlas(msg) {
		Canvas.frames = msg.frames.slice()
		Canvas.atlas.src = "/" + msg.ImgPath
	}

	update(x, y, tile) {
		if(x === undefined || y === undefined || tile === undefined) {
			return;
		}
		if(x < 0 || x >= this.cols) {
			return;
		}
		if(y < 0 || y >= this.rows) {
			return;
		}
		if(tile < 0 || tile >= Canvas.frames.length) {
			return;
		}
		this.map[y][x] = tile;
	}

	clear() {
		this.ctx.fillStyle = Canvas.BACKGROUND;
		this.ctx.fillRect(0, 0, this.ctx.canvas.width, this.ctx.canvas.height);
	}

	draw() {
		if(this.map == null) {
			return;
		}
		this.map.forEach( (row, i) => {
			row.forEach( (col, j) => {
				if(col == 0 || col > Canvas.frames.length) {
					return;
				}
				this.ctx.drawImage(
					Canvas.atlas,
					Canvas.frames[col-1].x, Canvas.frames[col-1].y, Canvas.actual_wid, Canvas.actual_hei,
					j * this.view_wid, i * this.view_hei, this.view_wid, this.view_hei
				);
			});
		});
	}
}

const tilemap_canvas = new Canvas(viewport.getContext("2d"));
const palette_canvas = new Canvas(palette.getContext("2d"));

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
			tilemap_canvas.restore(update, decompress(compressed));
			stage++;
			break
		case 1:
			Canvas.load_atlas(update);
			Canvas.atlas.onload = () => {
				palette_canvas.gen_selectormap(Canvas.frames.length);
				tilemap_canvas.clear();
				tilemap_canvas.draw();
				palette_canvas.clear();
				palette_canvas.draw();
			}
			stage++;
			break;
		default:
			tilemap_canvas.update(update.x, update.y, update.tile);
			tilemap_canvas.clear();
			tilemap_canvas.draw();
			palette_canvas.clear();
			palette_canvas.draw();
		}
	}

	return {
		parse
	};
})();

const Connection = (function () {
	const ws = new WebSocket(location.pathname + "ws");
	ws.addEventListener("message", (event) => {
		Parser.parse(event.data);
	})

	function send_update(e) {
		const rect = viewport.getBoundingClientRect();
		mouse.x = Math.floor((e.clientX - rect.left) / tilemap_canvas.view_wid);
		mouse.y = Math.floor((e.clientY - rect.top) / tilemap_canvas.view_hei);
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

let mouse = {x: 0, y: 0, down: false}
let tile_selected = 1
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

// TODO(garipew): Not working. No tile other than the first is being selected.
palette.addEventListener("mousedown", (e) => {
	const rect = palette.getBoundingClientRect();
	const containerRect = document.getElementById("palette-container").getBoundingClientRect();
	const scrollTop = document.getElementById("palette-container").scrollTop;
	const tileW = palette_canvas.view_wid;
	const tileH = palette_canvas.view_hei;
	const tilesPerRow = palette_canvas.cols;
	
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
