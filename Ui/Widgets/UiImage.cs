using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiImage : UiWidget {
	public override void HandleInput(in InputEvent input, in SKRect size, ref EventState isHandled, IRootContext ctx, UiInputEventPhase phase) { }

	public SKImage Image { get; set; } = null!;
	public SKColor MultiplyColor { get; set; } = SKColors.White;
	public float ImageScaling { get; set; } = 1;
	public HorizontalAnchor HAnchor { get; set; } = HorizontalAnchor.Middle;
	public VerticalAnchor VAnchor { get; set; } = VerticalAnchor.Middle;
	public SKPoint AnchorPoint { get; set; }

	public override SKRect CalculateSize() {
		var width = Image.Width * ImageScaling;
		var height = Image.Height * ImageScaling;

		var xAbs = AnchorPoint.X - width * HAnchor switch {
			HorizontalAnchor.Left => 0,
			HorizontalAnchor.Middle => 0.5f,
			HorizontalAnchor.Right => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(HAnchor), HAnchor, null)
		};
		var yAbs = AnchorPoint.Y - height * VAnchor switch {
			VerticalAnchor.Top => 0,
			VerticalAnchor.Middle => 0.5f,
			VerticalAnchor.Bottom => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(VAnchor), VAnchor, null)
		};

		return new SKRect(xAbs, yAbs, xAbs + width, yAbs + height);
	}

	public override void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx) {
		Paint.FilterQuality = SKFilterQuality.High;

		using var colorFilter = SKColorFilter.CreateBlendMode(MultiplyColor, SKBlendMode.Modulate);
		Paint.ColorFilter = colorFilter;
		uiCanvas.DrawImage(Image, size, Paint);
		Paint.ColorFilter = null;
	}
}
