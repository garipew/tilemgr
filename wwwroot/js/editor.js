class Viewport {
	static MIN_COLS = 5;
	static MIN_ROWS = 5;
	static MAX_COLS = 20;
	static MAX_ROWS = 20;
	cols;
	rows;
	x;
	y;

	constructor(cols, rows) {
		this.cols = cols;
		this.rows = rows;

		this.x = 0;
		this.y = 0;
	}

	zoom(ratio) {
		this.cols += ratio;
		if(this.cols < Viewport.MIN_COLS) {
			this.cols = Viewport.MIN_COLS;
		} else if(this.cols > Viewport.MAX_COLS) {
			this.cols = Viewport.MAX_COLS;
		}
		this.rows += ratio;
		if(this.rows < Viewport.MIN_ROWS) {
			this.rows = Viewport.MIN_ROWS;
		} else if(this.rows > Viewport.MAX_ROWS) {
			this.rows = Viewport.MAX_ROWS;
		}
	}
}

class Canvas {
	static BACKGROUND = "#101010";
	static atlas = new Image();
	static frames = null;
	static wid;
	static hei;
	ctx;
	cols;
	rows;
	map;
	view;

	constructor(ctx) {
		this.ctx = ctx;
	}

	scroll_vertical(value) {
		this.view.y += value;
		if(this.view.y < 0) {
			this.view.y = 0;
		}
		if(this.view.y + this.view.rows > this.rows) {
			this.view.y = this.rows - this.view.rows;
		}
		this.clear();
		this.draw();
	}

	scroll_horizontal(value) {
		this.view.x += value;
		if(this.view.x < 0) {
			this.view.x = 0;
		}
		if(this.view.x + this.view.cols > this.cols) {
			this.view.x = this.cols - this.view.cols;
		}
		this.clear();
		this.draw();
	}

	zoom(ratio) {
		this.view.zoom(ratio);
		this.ctx.canvas.width = Canvas.wid * this.view.cols;
		this.ctx.canvas.height = Canvas.hei * this.view.rows;
		this.clear();
		this.draw();
		console.log(this.view.cols + " " + this.view.rows);
	}

	restore(canvas, decompressed) {
		this.cols = canvas.Wid
		this.rows = canvas.Hei

		Canvas.wid = canvas.TileWid;
		Canvas.hei = canvas.TileHei;

		this.view = new Viewport(20, 20);

		this.map = decompressed;

		this.ctx.canvas.width = Canvas.wid * this.view.cols;
		this.ctx.canvas.height = Canvas.hei * this.view.rows;
	}

	gen_selectormap(tilecount) {
		this.view = new Viewport(4, 10);

		const cols = 4;
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
			for(let i = tmp.length; i < cols; i++){
				tmp.push(0);
			}
			this.map.push(tmp);
		}
		this.cols = cols;
		this.rows = this.map.length;

		this.ctx.canvas.width = Canvas.wid * this.view.cols;
		this.ctx.canvas.height = Canvas.hei * this.view.rows;
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
		const start_row = Math.floor(this.view.y);
		const end_row = Math.min(Math.ceil(this.view.y + this.view.rows), this.rows);
		const start_col = Math.floor(this.view.x);
		const end_col = Math.min(Math.ceil(this.view.x + this.view.cols), this.cols);
		for(let i = start_row; i < end_row; i++) {
			const y_pos = (i - this.view.y) * Canvas.hei;
			for(let j = start_col; j < end_col; j++) {
				const x_pos = (j - this.view.x) * Canvas.wid;
				const tile = this.map[i][j];
				if(tile == 0 || tile >= Canvas.frames.length) {
					continue;
				}
				this.ctx.drawImage(
					Canvas.atlas,
					Canvas.frames[tile-1].x, Canvas.frames[tile-1].y, Canvas.wid, Canvas.hei,
					x_pos, y_pos, Canvas.wid, Canvas.hei
				);
			}
		}
	}

	get_tile_pos(screen_x, screen_y) {
		const rect = this.ctx.canvas.getBoundingClientRect();
		const tileW = rect.width / this.view.cols;
		const tileH = rect.height / this.view.rows;

		return {
			x: Math.floor((screen_x - rect.left) / tileW) + Math.floor(this.view.x),
			y: Math.floor((screen_y - rect.top) / tileH) + Math.floor(this.view.y)
		}
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
		const tile = tilemap_canvas.get_tile_pos(e.clientX, e.clientY);
		let msg = {x: tile.x, y: tile.y, tile: tile_selected};
		if(erase) {
			msg.tile = 0;
		}
		msg = JSON.stringify(msg);
		ws.send(msg);
	}

	return {
		send_update
	};
})();

let mouse = {down: false}
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

palette.addEventListener("mousedown", (e) => {
	const tile = palette_canvas.get_tile_pos(e.clientX, e.clientY);

	const idx = tile.x + tile.y * palette_canvas.view.cols;
	if(e.button === 0) {
		if(idx < 0 || idx >= Canvas.frames.length) {
			return;
		}
		tile_selected = idx + 1;
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
