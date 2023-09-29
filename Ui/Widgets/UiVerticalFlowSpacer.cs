using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiVerticalFlowSpacer : UiWidget {
	public override void HandleInput(in InputEvent e, in SKRect size, ref EventState isHandled, IRootContext ctx, UiInputEventPhase phase) { }

	public override void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx) { }

	public float Height { get; set; }

	public override SKRect CalculateSize() => new(0, 0, 0, Height);
}
