class Viewport {
	static MIN_COLS = 5;
	static MIN_ROWS = 5;
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
		}
		this.rows += ratio;
		if(this.rows < Viewport.MIN_ROWS) {
			this.rows = Viewport.MIN_ROWS;
		}
	}
}
