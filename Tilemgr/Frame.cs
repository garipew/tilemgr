namespace Tilemgr;

public record FrameView(int x, int y);

public class Frame
{
	int x;
	int y;
	public Frame(int x, int y)
	{
		this.x = x;
		this.y = y;
	}

	public FrameView GetView()
	{
		return new FrameView(x, y);
	}
}
