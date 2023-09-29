using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiBox : UiWrapper {
	public ColorScheme? CustomColors { get; set; }
	public ColorScheme Colors => CustomColors ?? ColorScheme.UiTheme;
	public float Padding { get; set; } = 5;
	public float Border { get; set; } = 3;
	public float BorderRadius { get; set; } = 8;
	public float ChildInset => Padding + Border;

	private static bool ShouldCaptureInput(in InputEvent input, in SKRect size, UiInputEventPhase phase) {
		if (phase != UiInputEventPhase.General) return false;

		switch (input.Type) {
			case InputEventType.CursorMoved when size.IsImpactedBy(FmsApp.Instance.MousePosition):
			case InputEventType.MouseButton when size.IsImpactedBy(FmsApp.Instance.MousePosition):
			case InputEventType.ScrollWheel when size.IsImpactedBy(FmsApp.Instance.MousePosition):
				return true;
			case InputEventType.KeyboardKey:
			case InputEventType.TextWritten:
			case InputEventType.Unknown:
			default:
				return false;
		}
	}

	public override void HandleInput(in InputEvent input, in SKRect size, ref EventState isHandled, IRootContext ctx, UiInputEventPhase phase) {
		if (Child == null)
			return;

		var childSize = size.Inset(ChildInset);
		Child.HandleInput(in input, in childSize, ref isHandled, ctx, phase);
		if (ShouldCaptureInput(in input, in childSize, phase))
			isHandled.Handle();
	}

	public override void Tick(in SKRect size) {
		Child?.Tick(Child.CalculateSize());
	}

	public override void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx) {
		if (Child == null)
			return;

		var r = BorderRadius;
		Paint.Color = Colors[3];
		uiCanvas.DrawRoundRect(size, r, r, Paint);

		r -= Border;
		Paint.Color = Colors[1];
		uiCanvas.DrawRoundRect(size.Inset(Border), r, r, Paint);

		var childSize = size.Inset(ChildInset);
		Child.Render(uiCanvas, childSize, ctx);
	}

	public override SKRect CalculateSize() {
		return Child == null ? SKRect.Empty : Child.CalculateSize().Outset(ChildInset);
	}
}
