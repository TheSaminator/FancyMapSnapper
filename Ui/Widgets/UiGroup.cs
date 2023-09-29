using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiGroup : UiContainer {
	public override void HandleInput(in InputEvent input, in SKRect size, ref EventState isHandled, IRootContext ctx, UiInputEventPhase phase) {
		var mySize = CalculateSize();
		for (var i = 0; i < NumChildren; i++) {
			var child = ChildAt(i);
			child.HandleInput(in input, size.CalculateGroupChild(in mySize, child), ref isHandled, ctx, phase);
		}
	}

	public override void Tick(in SKRect size) {
		var mySize = CalculateSize();
		for (var i = 0; i < NumChildren; i++) {
			var child = ChildAt(i);
			child.Tick(size.CalculateGroupChild(in mySize, child));
		}
	}

	public override void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx) {
		var mySize = CalculateSize();
		for (var i = 0; i < NumChildren; i++) {
			var child = ChildAt(i);
			child.Render(uiCanvas, size.CalculateGroupChild(in mySize, child), ctx);
		}
	}

	public override SKRect CalculateSize() {
		var rect = new SKRect();
		for (var i = 0; i < NumChildren; i++) {
			var childSize = ChildAt(i).CalculateSize();
			if (i == 0)
				rect = childSize;
			else {
				if (rect.Top > childSize.Top) rect.Top = childSize.Top;
				if (rect.Left > childSize.Left) rect.Left = childSize.Left;
				if (rect.Right < childSize.Right) rect.Right = childSize.Right;
				if (rect.Bottom < childSize.Bottom) rect.Bottom = childSize.Bottom;
			}
		}

		return rect;
	}
}
