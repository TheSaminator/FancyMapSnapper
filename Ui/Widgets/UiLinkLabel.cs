using FancyMapSnapper.DataStructures;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiLinkLabel : UiWidget, IDisposable {
	public bool IsClicked { get; private set; }
	public bool IsPressed => IsClicked;

	private LinkLabelGroup _group;

	internal void Click() => _group.Click();

	public UiLinkLabel(LinkLabelGroup group) {
		_group = group;
		_group.Links.Add(this);
	}

	public override void HandleInput(in InputEvent input, in SKRect size, ref EventState isHandled, IRootContext ctx, UiInputEventPhase phase) {
		if (phase == UiInputEventPhase.Focused || isHandled.IsHandled) return;

		var prevIsPressed = IsPressed;

		switch (input.Type) {
			case InputEventType.CursorMoved when phase == UiInputEventPhase.General: {
				if (size.IsImpactedBy(input.AsCursorMoved!.Value.NewPosition))
					isHandled.Handle();
				break;
			}
			case InputEventType.MouseButton when IsHovered && phase == UiInputEventPhase.General: {
				isHandled.Handle();

				var mouseButton = input.AsMouseButton!.Value;
				if (mouseButton.Button != MouseButton.Left)
					break;

				IsClicked = mouseButton.ButtonAction == InputEventAction.Press;
				break;
			}
			case InputEventType.KeyboardKey:
			case InputEventType.TextWritten:
			case InputEventType.ScrollWheel:
			case InputEventType.Unknown:
			default: break;
		}

		if (IsHovered && prevIsPressed && !IsPressed)
			Click();
	}

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

	public override void Tick(in SKRect size) {
		base.Tick(in size);
		if (!IsHovered)
			IsClicked = false;
	}

	public override void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx) {
		Paint.Color = Style.Color;
		Paint.SelectFont(Style.FontSpec);
		Paint.TextSize = Style.FontSize;
		uiCanvas.DrawAnchoredText(in _textMutable, size.GetAnchorPoint(HAnchor, VAnchor), HAnchor, VAnchor, Style.RenderStyle with { IsUnderline = Style.RenderStyle.IsUnderline ^ _group.ShouldUnderline }, Paint, out _);
	}

	private void ReleaseUnmanagedResources() {
		MutableString.ReturnCopy(in _textMutable);
	}

	public void Dispose() {
		ReleaseUnmanagedResources();
		GC.SuppressFinalize(this);
	}

	~UiLinkLabel() {
		ReleaseUnmanagedResources();
	}
}
