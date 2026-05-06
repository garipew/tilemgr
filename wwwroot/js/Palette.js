class Palette extends Canvas {
	tile_selected;

	constructor(ctx) {
		super(ctx);
		this.tile_selected = 1;
	}

	draw() {
		const selected = {
			x: (this.tile_selected - 1) % this.cols,
			y: Math.floor((this.tile_selected - 1) / this.cols)
		};
		const y_pos = selected.y * Canvas.hei;
		const x_pos = selected.x * Canvas.wid;
		super.draw();
	}
}
