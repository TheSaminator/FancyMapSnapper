using FancyMapSnapper.DataStructures;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiRadioButton : UiWidget, IDisposable {
	public bool IsClicked { get; private set; }
	public bool IsEnabled { get; set; } = true;

	public RadioButtonGroup Group { get; set; }
	public int CheckedValue { get; set; }
	public bool IsChecked => Group.Value == CheckedValue;

	public UiRadioButton(RadioButtonGroup group, int checkedValue) {
		Group = group;
		CheckedValue = checkedValue;
	}

	public void Check() {
		Group.Value = CheckedValue;
	}

	public ColorScheme? CustomColors { get; set; }
	public ColorScheme Colors => CustomColors ?? ColorScheme.UiTheme;
	public float BorderSize { get; set; } = 3;
	public float OvalPaddingSize { get; set; } = 5;
	public float TextShift { get; set; } = 2;
	public float Size { get; set; } = 32;

	private MutableString _textMutable = MutableString.RentedEmpty();

	public void SetText(ReadOnlySpan<char> text) {
		var length = text.Length;
		if (_textMutable.Chars.Length < length) {
			MutableString.ReturnCopy(in _textMutable);
			_textMutable = MutableString.RentedBlank(length);
		}

		text.CopyTo(_textMutable.AsSpan());
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

	public override void HandleInput(in InputEvent input, in SKRect size, ref EventState isHandled, IRootContext ctx, UiInputEventPhase phase) {
		if (phase == UiInputEventPhase.Focused || isHandled.IsHandled) return;

		var prevIsClicked = IsClicked;

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
			default:
				break;
		}

		if (IsEnabled && IsHovered && prevIsClicked && !IsClicked)
			Check();
	}

	public override void Tick(in SKRect size) {
		base.Tick(in size);
		if (!IsHovered)
			IsClicked = false;
	}

	private void Render(SKCanvas uiCanvas, SKRect size, int stroke, int fill, int text, int check) {
		var ovalSize = size with { Right = size.Left + Size };

		Paint.Color = Colors[stroke];
		uiCanvas.DrawOval(ovalSize, Paint);

		Paint.Color = Colors[fill];
		uiCanvas.DrawOval(ovalSize.Inset(BorderSize), Paint);

		Paint.Color = Colors[text];
		Paint.SelectFont(new FontSpec { Bold = true });
		Paint.TextSize = Size * 3 / 4;
		uiCanvas.DrawAnchoredText(in _textMutable, new SKPoint(size.Right, size.MidY), HorizontalAnchor.Right, VerticalAnchor.Middle, new TextRenderStyle(), Paint, out _);

		if (!IsChecked) return;
		Paint.Color = Colors[check];
		uiCanvas.DrawOval(ovalSize.Inset(BorderSize + OvalPaddingSize), Paint);
	}

	public override void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx) {
		if (!IsEnabled)
			Render(uiCanvas, size, 2, 1, 3, 3);
		else if (IsClicked)
			Render(uiCanvas, size, 4, 5, 4, 2);
		else if (IsHovered)
			Render(uiCanvas, size, 4, 2, 4, 5);
		else
			Render(uiCanvas, size, 3, 1, 4, 4);
	}

	public override SKRect CalculateSize() {
		Paint.SelectFont(new FontSpec { Bold = true });
		Paint.TextSize = Size * 3 / 4;

		Paint.MeasureTextFull(in _textMutable, out var rect);
		var width = Size + TextShift + rect.Width;
		var height = Size;

		var x = AnchorPoint.X - Size * HAnchor switch {
			HorizontalAnchor.Left => 0,
			HorizontalAnchor.Middle => 0.5f,
			HorizontalAnchor.Right => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(HAnchor), HAnchor, null)
		};
		var y = AnchorPoint.Y - Size * VAnchor switch {
			VerticalAnchor.Top => 0,
			VerticalAnchor.Middle => 0.5f,
			VerticalAnchor.Bottom => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(VAnchor), VAnchor, null)
		};

		return new SKRect(x, y, x + width, y + height);
	}

	private void ReleaseUnmanagedResources() {
		MutableString.ReturnCopy(in _textMutable);
	}

	public void Dispose() {
		ReleaseUnmanagedResources();
		GC.SuppressFinalize(this);
	}

	~UiRadioButton() {
		ReleaseUnmanagedResources();
	}
}
