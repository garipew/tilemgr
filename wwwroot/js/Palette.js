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
}
