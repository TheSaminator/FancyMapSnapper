using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public abstract class UiWidget {
	public bool IsHovered { get; private set; }

	public abstract void HandleInput(in InputEvent input, in SKRect size, ref EventState isHandled, IRootContext ctx, UiInputEventPhase phase);

	public virtual void Tick(in SKRect size) {
		IsHovered = size.IsImpactedBy(FmsApp.Instance.MousePosition);
	}

	public abstract void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx);

	public abstract SKRect CalculateSize();

	public static SKPaint CreatePaint() => new() {
		IsAntialias = true,
		SubpixelText = true,
		LcdRenderText = true
	};

	protected readonly SKPaint Paint = CreatePaint();
}
