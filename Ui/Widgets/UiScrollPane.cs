using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiScrollPane : UiContainer {
	private bool _mouseDown;
	private bool _mouseDownOnScrollBar;

	private static bool IsContentHovered(SKRect size) {
		return new SKRect(size.Left, size.Top, size.Right - ScrollWidgetWidth, size.Bottom).IsImpactedBy(FmsApp.Instance.MousePosition);
	}

	private static bool IsScrollBarHovered(SKRect size) {
		return new SKRect(size.Right - ScrollWidgetWidth, size.Top, size.Right, size.Bottom).IsImpactedBy(FmsApp.Instance.MousePosition);
	}

	public override void HandleInput(in InputEvent input, in SKRect size, ref EventState isHandled, IRootContext ctx, UiInputEventPhase phase) {
		var innerSize = CalculateInnerSize();
		var scrollShift = CalculateScrollShift(size, innerSize, out var scrollingEnabled);

		switch (input.Type) {
			case InputEventType.CursorMoved when phase == UiInputEventPhase.General: {
				MoveScrollWithMouse(input, size, ref isHandled, scrollShift, scrollingEnabled, true, ctx, phase, out var hovered, out var scrollBarHovered);
				if (hovered || scrollBarHovered || _mouseDownOnScrollBar)
					isHandled.Handle();

				break;
			}
			case InputEventType.MouseButton when phase == UiInputEventPhase.General: {
				var mouseButton = input.AsMouseButton!.Value;
				if (mouseButton.Button == MouseButton.Left)
					_mouseDown = mouseButton.ButtonAction == InputEventAction.Press;

				MoveScrollWithMouse(input, size, ref isHandled, scrollShift, scrollingEnabled, false, ctx, phase, out var hovered, out var scrollBarHovered);

				if (hovered || scrollBarHovered || _mouseDownOnScrollBar)
					isHandled.Handle();

				break;
			}
			case InputEventType.ScrollWheel when IsContentHovered(size) && phase == UiInputEventPhase.General: {
				var scrollAmount = input.AsScrollWheel!.Value.AmountScrolled;

				for (var i = 0; i < NumChildren; i++) {
					var child = ChildAt(i);
					child.HandleInput(in input, size.CalculateChild(child).WithOffset(0, scrollShift), ref isHandled, ctx, phase);
				}

				if (isHandled.IsHandled) return;

				var prevScrollPos = ScrollPos;
				ScrollPos = MathF.Min(MathF.Max(ScrollPos + scrollAmount * ScrollSpeed / (size.Height - innerSize.Height), 0), 1);

				var dy = (ScrollPos - prevScrollPos) * (size.Height - innerSize.Height);
				var newPosition = FmsApp.Instance.MousePosition;
				var oldPosition = newPosition with { Y = newPosition.Y - dy };
				var newInput = new InputEvent(new InputEventCursorMoved(oldPosition, newPosition));

				for (var i = 0; i < NumChildren; i++) {
					var child = ChildAt(i);
					child.HandleInput(in newInput, size.CalculateChild(child).WithOffset(0, scrollShift + dy), ref isHandled, ctx, phase);
				}

				isHandled.Handle();
				break;
			}
			case InputEventType.KeyboardKey:
			case InputEventType.TextWritten: {
				for (var i = 0; i < NumChildren; i++) {
					var child = ChildAt(i);
					child.HandleInput(in input, size.CalculateChild(child).WithOffset(0, scrollShift), ref isHandled, ctx, phase);
				}

				break;
			}
			case InputEventType.Unknown:
			default:
				break;
		}
	}

	private void MoveScrollWithMouse(InputEvent input, SKRect size, ref EventState isHandled, float scrollShift, bool scrollingEnabled, bool alwaysNotifyChildren, IRootContext ctx, UiInputEventPhase phase, out bool hovered, out bool scrollBarHovered) {
		hovered = IsContentHovered(size);
		if ((hovered || alwaysNotifyChildren) && !_mouseDownOnScrollBar)
			for (var i = 0; i < NumChildren; i++) {
				var child = ChildAt(i);
				child.HandleInput(in input, size.CalculateChild(child).WithOffset(0, scrollShift), ref isHandled, ctx, phase);
			}

		scrollBarHovered = IsScrollBarHovered(size);
		if (scrollBarHovered && _mouseDown)
			_mouseDownOnScrollBar = true;
		if ((!hovered && !scrollBarHovered) || !_mouseDown)
			_mouseDownOnScrollBar = false;

		if (_mouseDownOnScrollBar && scrollingEnabled)
			ScrollPos = MathF.Min(MathF.Max((FmsApp.Instance.MousePosition.Y - size.Top - ScrollWidgetWidth / 2) / (size.Height - ScrollWidgetWidth), 0), 1);
	}

	public override void Tick(in SKRect size) {
		var scrollShift = CalculateScrollShift(size, CalculateInnerSize(), out _);

		for (var i = 0; i < NumChildren; i++) {
			var child = ChildAt(i);
			child.Tick(size.CalculateChild(child).WithOffset(0, scrollShift));
		}
	}

	public override void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx) {
		var scrollShift = CalculateScrollShift(size, CalculateInnerSize(), out var scrollingEnabled);

		var barRect = new SKRect(size.Right - ScrollWidgetWidth, size.Top, size.Right, size.Bottom).Inset((ScrollWidgetWidth - ScrollBarWidth) / 2);
		Paint.Color = scrollingEnabled ? Colors[3] : Colors[2];
		uiCanvas.DrawRoundRect(barRect, ScrollBarWidth / 2, ScrollBarWidth / 2, Paint);

		if (scrollingEnabled) {
			Paint.Color = Colors[5];
			const float radius = ScrollWidgetWidth / 2;
			var centerY = size.Top + radius + (size.Height - ScrollWidgetWidth) * ScrollPos;
			uiCanvas.DrawCircle(size.Right - radius, centerY, radius, Paint);
		}

		uiCanvas.Save();
		uiCanvas.ClipRect(new SKRect(size.Left, size.Top, size.Right - ScrollWidgetWidth, size.Bottom));
		for (var i = 0; i < NumChildren; i++) {
			var child = ChildAt(i);
			child.Render(uiCanvas, size.CalculateChild(child).WithOffset(0, scrollShift), ctx);
		}

		uiCanvas.Restore();
	}

	public const float ScrollWidgetWidth = 16;
	public const float ScrollBarWidth = 8;

	public const float ScrollSpeed = 64;

	public ColorScheme? CustomColors { get; set; }
	public ColorScheme Colors => CustomColors ?? ColorScheme.UiTheme;

	public float ScrollPos { get; set; }

	public float InnerSizePadding { get; set; }

	public SKRect OuterSize { get; set; }

	private SKRect CalculateInnerSize() {
		var rect = new SKRect();
		for (var i = 0; i < NumChildren; i++) {
			var childSize = ChildAt(i).CalculateSize();
			if (i == 0) {
				rect = childSize;
				continue;
			}

			rect.Top = MathF.Min(rect.Top, childSize.Top);
			rect.Left = MathF.Min(rect.Left, childSize.Left);
			rect.Right = MathF.Max(rect.Right, childSize.Right);
			rect.Bottom = MathF.Max(rect.Bottom, childSize.Bottom);
		}

		rect.Top -= InnerSizePadding;
		rect.Bottom += InnerSizePadding;

		return rect;
	}

	private float CalculateScrollShift(SKRect outerSize, SKRect innerSize, out bool scrollingEnabled) {
		var difference = MathF.Min(0, outerSize.Height - innerSize.Height);
		scrollingEnabled = difference != 0;
		return ScrollPos * difference - innerSize.Top;
	}

	public override SKRect CalculateSize() => OuterSize with { Right = OuterSize.Right + ScrollWidgetWidth };
}
