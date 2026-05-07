class Palette extends Canvas {
	tile_selected;

	constructor(ctx) {
		super(ctx);
		this.tile_selected = 1;
	}

	draw() {
		super.draw();

		const selected = {
			x: (this.tile_selected - 1) % this.cols,
			y: Math.floor((this.tile_selected - 1) / this.cols)
		};
		const y_pos = (selected.y - this.view.y) * Canvas.hei;
		const start_row = Math.floor(this.view.y);
		const end_row = Math.min(Math.ceil(this.view.y + this.view.rows), this.rows);
		if(selected.y < start_row || selected.y > end_row) {
			return;
		}
		const x_pos = (selected.x - this.view.x) * Canvas.wid;

		this.ctx.strokeStyle = "#ff0000";
		this.ctx.strokeRect(x_pos, y_pos, Canvas.wid, Canvas.hei);
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
}
