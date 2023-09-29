using FancyMapSnapper.Ui.Widgets;
using OpenTK.Mathematics;
using SkiaSharp;

namespace FancyMapSnapper.Ui;

public static class WidgetSizing {
	public static SKRect Inset(in this SKRect givenSize, float inset) {
		return new SKRect(
			givenSize.Left + inset,
			givenSize.Top + inset,
			givenSize.Right - inset,
			givenSize.Bottom - inset
		);
	}

	public static SKRect Outset(in this SKRect givenSize, float outset) {
		return givenSize.Inset(-outset);
	}

	public static SKRect WithOffset(this SKRect givenSize, float x, float y) {
		givenSize.Offset(x, y);
		return givenSize;
	}

	public static SKRect CalculateChild(in this SKRect givenSize, UiWidget child) {
		var childSize = child.CalculateSize();
		return new SKRect(
			childSize.Left + givenSize.Left,
			childSize.Top + givenSize.Top,
			childSize.Right + givenSize.Left,
			childSize.Bottom + givenSize.Top
		);
	}

	public static SKRect CalculateGroupChild(in this SKRect givenSize, in SKRect calculatedSize, UiWidget child) {
		var childSize = child.CalculateSize();
		return new SKRect(
			childSize.Left + givenSize.Left - calculatedSize.Left,
			childSize.Top + givenSize.Top - calculatedSize.Top,
			childSize.Right + givenSize.Left - calculatedSize.Left,
			childSize.Bottom + givenSize.Top - calculatedSize.Top
		);
	}

	public static bool IsImpactedBy(in this SKRect currentSize, in Vector2 position) {
		return currentSize.Contains(position.X, position.Y);
	}
}
