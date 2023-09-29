using FancyMapSnapper.DataStructures;
using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiLabel : UiWidget, IDisposable {
	public override void HandleInput(in InputEvent input, in SKRect size, ref EventState isHandled, IRootContext ctx, UiInputEventPhase phase) { }

	public TextStyle Style { get; set; } = new();
	private MutableString _textMutable = MutableString.RentedEmpty();

	internal ref MutableString TextMutable => ref _textMutable;

	public void SetText(ReadOnlySpan<char> text) {
		var length = text.Length;
		if (_textMutable.Chars.Length < length) {
			MutableString.ReturnCopy(in _textMutable);
			_textMutable = MutableString.RentedBlank(length);
		}

		text.CopyTo(_textMutable.AsSpan());
	}

	public void AppendText(ReadOnlySpan<char> text) {
		var length = _textMutable.Length + text.Length;
		var destination = _textMutable.Chars;
		if (destination.Length < length) {
			destination = MutableString.CharPool.Rent(length);
			_textMutable.AsSpan().CopyTo(destination.AsSpan());

			MutableString.CharPool.Return(_textMutable.Chars);
			_textMutable.Chars = destination;
		}

		text.CopyTo(new Span<char>(_textMutable.Chars, _textMutable.Length, text.Length));
		_textMutable.Start = 0;
		_textMutable.Length = length;
	}

	public string Text {
		get => _textMutable.ToString();
		set {
			var length = value.Length;
			if (_textMutable.Chars.Length < length) {
				MutableString.ReturnCopy(in _textMutable);
				_textMutable = MutableString.RentedBlank(length);
			}

			value.CopyTo(_textMutable.AsSpan());
		}
	}

	public HorizontalAnchor HAnchor { get; set; } = HorizontalAnchor.Middle;
	public VerticalAnchor VAnchor { get; set; } = VerticalAnchor.Middle;
	public SKPoint AnchorPoint { get; set; }

	public override SKRect CalculateSize() {
		Paint.SelectFont(Style.FontSpec);
		Paint.TextSize = Style.FontSize;

		Paint.MeasureTextFull(in _textMutable, out var boundRect);

		var width = boundRect.Width;
		var height = boundRect.Height;

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
		var style = Style;
		Paint.Color = style.Color;
		Paint.SelectFont(Style.FontSpec);
		Paint.TextSize = style.FontSize;
		uiCanvas.DrawAnchoredText(in _textMutable, size.GetAnchorPoint(HAnchor, VAnchor), HAnchor, VAnchor, in style.RenderStyle, Paint, out _);
	}

	private void ReleaseUnmanagedResources() {
		MutableString.ReturnCopy(in _textMutable);
	}

	public void Dispose() {
		ReleaseUnmanagedResources();
		GC.SuppressFinalize(this);
	}

	~UiLabel() {
		ReleaseUnmanagedResources();
	}
}
