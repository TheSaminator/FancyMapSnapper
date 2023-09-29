using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiRoot : UiContainer, IRootContext {
	public UiFocusable? FocusedWidget { get; set; } = null;

	public override void HandleInput(in InputEvent input, in SKRect size, ref EventState isHandled, IRootContext ctx, UiInputEventPhase phase) {
		for (var i = NumChildren - 1; i >= 0; i--) {
			var child = ChildAt(i);
			child.HandleInput(in input, child.CalculateSize(), ref isHandled, ctx, phase);
		}
	}

	public override SKRect CalculateSize() => new(0, 0, FmsApp.Instance.ClientSize.X, FmsApp.Instance.ClientSize.Y);

	public override void Tick(in SKRect size) {
		for (var i = 0; i < NumChildren; i++) {
			var child = ChildAt(i);
			child.Tick(child.CalculateSize());
		}
	}

	public override void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx) {
		for (var i = 0; i < NumChildren; i++) {
			var child = ChildAt(i);
			child.Render(uiCanvas, child.CalculateSize(), ctx);
		}
	}

	public bool HandleInput(in InputEvent e) {
		var isHandled = new EventState();
		var size = CalculateSize();

		HandleInput(in e, size, ref isHandled, this, UiInputEventPhase.Focused);
		if (isHandled.IsHandled)
			return true;

		HandleInput(in e, size, ref isHandled, this, UiInputEventPhase.HotKeys);
		if (isHandled.IsHandled)
			return true;

		HandleInput(in e, size, ref isHandled, this, UiInputEventPhase.General);
		return isHandled.IsHandled;
	}

	public void Tick() {
		Tick(CalculateSize());
	}

	public void Render(SKCanvas uiCanvas) {
		Render(uiCanvas, CalculateSize(), this);
	}
}
