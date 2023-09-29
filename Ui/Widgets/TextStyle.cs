using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public struct TextStyle : IEquatable<TextStyle> {
	public SKColor Color = SKColors.White;
	public float FontSize = 24;
	public FontSpec FontSpec;
	public TextRenderStyle RenderStyle;

	public TextStyle() { }

	public bool Equals(TextStyle other) {
		return Color.Equals(other.Color) && FontSize.Equals(other.FontSize) && FontSpec == other.FontSpec && RenderStyle == other.RenderStyle;
	}

	public override bool Equals(object? obj) {
		return obj is TextStyle other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine(Color, FontSize, FontSpec, RenderStyle);
	}

	public static bool operator ==(TextStyle left, TextStyle right) {
		return left.Equals(right);
	}

	public static bool operator !=(TextStyle left, TextStyle right) {
		return !left.Equals(right);
	}
}

public struct TextRenderStyle : IEquatable<TextRenderStyle> {
	public bool IsUnderline;
	public bool IsStrikethrough;

	public TextRenderStyle() { }

	public bool Equals(TextRenderStyle other) {
		return IsUnderline.Equals(other.IsUnderline) && IsStrikethrough.Equals(other.IsStrikethrough);
	}

	public override bool Equals(object? obj) {
		return obj is TextRenderStyle other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine(IsUnderline, IsStrikethrough);
	}

	public static bool operator ==(TextRenderStyle left, TextRenderStyle right) {
		return left.Equals(right);
	}

	public static bool operator !=(TextRenderStyle left, TextRenderStyle right) {
		return !left.Equals(right);
	}
}
