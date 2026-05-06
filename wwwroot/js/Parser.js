class Parser {
	static stage = 0;

	static decompress(compressed) {
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

	static parse(msg) {
		var update = JSON.parse(msg)
		switch(stage){
		case 0:
			let compressed = new Uint8Array(update.compressed);
			tilemap_canvas.restore(update, Parser.decompress(compressed));
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
}
