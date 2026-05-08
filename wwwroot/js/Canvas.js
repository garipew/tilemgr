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
		if(this.view.cols > this.cols) {
			this.view.cols = this.cols;
		}
		if(this.view.rows > this.rows) {
			this.view.rows = this.rows;
		}
		this.ctx.canvas.width = Canvas.wid * this.view.cols;
		this.ctx.canvas.height = Canvas.hei * this.view.rows;
		this.clear();
		this.draw();
	}

	restore(canvas, decompressed) {
		this.cols = canvas.Wid
		this.rows = canvas.Hei

		Canvas.wid = canvas.TileWid;
		Canvas.hei = canvas.TileHei;

		this.view = new Viewport(Math.min(this.cols, 20), Math.min(this.rows, 20));

		this.map = decompressed;

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
		if(tile < 0 || tile > Canvas.frames.length) {
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
				if(tile == 0 || tile > Canvas.frames.length) {
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
			x: Math.floor(((screen_x - rect.left) / tileW) + this.view.x),
			y: Math.floor(((screen_y - rect.top) / tileH) + this.view.y)
		}
	}
}
