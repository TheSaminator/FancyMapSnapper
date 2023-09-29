using FancyMapSnapper.DataStructures;
using FancyMapSnapper.Ui.Widgets;
using SkiaSharp;

namespace FancyMapSnapper.Ui;

public enum HorizontalAnchor : sbyte {
	Left = -1,
	Middle = 0,
	Right = 1
}

public enum VerticalAnchor : sbyte {
	Top = -1,
	Middle = 0,
	Bottom = 1
}

public struct FontSpec : IEquatable<FontSpec> {
	public bool Bold, Italic;

	public bool Equals(FontSpec other) {
		return Bold == other.Bold && Italic == other.Italic;
	}

	public override bool Equals(object? obj) {
		return obj is FontSpec other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine(Bold, Italic);
	}

	public static bool operator ==(FontSpec left, FontSpec right) {
		return left.Equals(right);
	}

	public static bool operator !=(FontSpec left, FontSpec right) {
		return !left.Equals(right);
	}
}

public static class TextDrawing {
	public static void SelectFont(this SKPaint paint, FontSpec spec) {
		paint.Typeface = SKTypeface.Default;
		if (spec.Bold)
			paint.FakeBoldText = true;
		if (spec.Italic)
			paint.TextSkewX = -0.1875f;
	}

	public static SKPoint GetAnchorPoint(in this SKRect rect, HorizontalAnchor hAnchor, VerticalAnchor vAnchor) {
		return new SKPoint(
			hAnchor switch {
				HorizontalAnchor.Left => rect.Left,
				HorizontalAnchor.Middle => rect.MidX,
				HorizontalAnchor.Right => rect.Right,
				_ => throw new ArgumentOutOfRangeException(nameof(hAnchor), hAnchor, null)
			},
			vAnchor switch {
				VerticalAnchor.Top => rect.Top,
				VerticalAnchor.Middle => rect.MidY,
				VerticalAnchor.Bottom => rect.Bottom,
				_ => throw new ArgumentOutOfRangeException(nameof(vAnchor), vAnchor, null)
			}
		);
	}

	public static float MeasureTextFull(this SKPaint paint, in MutableString text, out SKRect boundRect) {
		boundRect = new SKRect();
		var measure = paint.MeasureText(text.AsSpan(), ref boundRect);

		using var plainFont = SKTypeface.Default.ToFont();
		plainFont.Size = paint.TextSize;

		var vPad = plainFont.Metrics.Leading / 2;
		boundRect.Top = plainFont.Metrics.Top - vPad;
		boundRect.Bottom = plainFont.Metrics.Bottom + vPad;

		return measure;
	}

	public static float DrawAnchoredText(this SKCanvas canvas, in MutableString text, in SKPoint drawAt, HorizontalAnchor hAnchor, VerticalAnchor vAnchor, in TextRenderStyle renderStyle, SKPaint paint, out SKRect bounds) {
		using var font = paint.ToFont();
		return canvas.DrawAnchoredText(in text, in drawAt, hAnchor, vAnchor, in renderStyle, paint, font, out bounds);
	}

	private static float DrawAnchoredText(this SKCanvas canvas, in MutableString text, in SKPoint drawAt, HorizontalAnchor hAnchor, VerticalAnchor vAnchor, in TextRenderStyle renderStyle, SKPaint paint, SKFont font, out SKRect bounds) {
		bounds = new SKRect();

		if (text.Length == 0) {
			return 0;
		}

		var measure = paint.MeasureTextFull(in text, out bounds);

		var x = hAnchor switch {
			HorizontalAnchor.Left => drawAt.X,
			HorizontalAnchor.Middle => drawAt.X - measure / 2,
			HorizontalAnchor.Right => drawAt.X - measure,
			_ => throw new ArgumentOutOfRangeException(nameof(hAnchor), hAnchor, null)
		};
		var y = vAnchor switch {
			VerticalAnchor.Top => drawAt.Y - bounds.Top,
			VerticalAnchor.Middle => drawAt.Y - bounds.MidY,
			VerticalAnchor.Bottom => drawAt.Y - bounds.Bottom,
			_ => throw new ArgumentOutOfRangeException(nameof(vAnchor), vAnchor, null)
		};

		if (renderStyle.IsStrikethrough) {
			var halfHeight = bounds.Bottom / 6;
			var strikethroughRect = new SKRect(x, y + bounds.MidY - halfHeight, x + measure, y + bounds.MidY + halfHeight);
			canvas.DrawRect(strikethroughRect, paint);
		}

		if (renderStyle.IsUnderline) {
			var underlineRect = new SKRect(x, y + bounds.Bottom / 3, x + measure, y + bounds.Bottom * 2 / 3);
			canvas.DrawRect(underlineRect, paint);
		}

		using var textBlob = SKTextBlob.Create(text.AsSpan(), font);
		canvas.DrawText(textBlob, x, y, paint);

		return measure;
	}
}
