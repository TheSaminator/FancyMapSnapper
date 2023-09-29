using FancyMapSnapper.DataStructures;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public abstract class UiAbstractButton : UiWidget, IDisposable {
	public bool IsClicked { get; private set; }
	public bool IsEnabled { get; set; } = true;

	protected abstract void Click();

	public ColorScheme? CustomColors { get; set; }
	public ColorScheme Colors => CustomColors ?? ColorScheme.UiTheme;
	public float BorderSize { get; set; } = 4;
	public float BorderRadius { get; set; } = 8;
	public float FontSize { get; set; } = 24;

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

	public SKImage? Icon { get; set; }

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
			default: break;
		}

		if (IsEnabled && IsHovered && prevIsClicked && !IsClicked)
			Click();
	}

	public override void Tick(in SKRect size) {
		base.Tick(in size);
		if (!IsHovered)
			IsClicked = false;
	}

	protected void Render(SKCanvas uiCanvas, in SKRect size, int stroke, int fill, int text) {
		var r = BorderRadius;
		Paint.Color = Colors[stroke];
		uiCanvas.DrawRoundRect(size, r, r, Paint);

		r -= BorderSize;
		Paint.Color = Colors[fill];
		uiCanvas.DrawRoundRect(size.Inset(BorderSize), r, r, Paint);

		Paint.Color = Colors[text];
		Paint.SelectFont(new FontSpec { Bold = true });
		Paint.TextSize = FontSize;

		var fontMetrics = Paint.FontMetrics;
		var iconWidth = Icon == null ? 0f : Icon.Width * (fontMetrics.Bottom + fontMetrics.Leading - fontMetrics.Top) / Icon.Height;
		var drawAt = size.GetAnchorPoint(HorizontalAnchor.Middle, VerticalAnchor.Middle);
		drawAt.Offset(iconWidth / 2, 0);
		uiCanvas.DrawAnchoredText(in _textMutable, in drawAt, HorizontalAnchor.Middle, VerticalAnchor.Middle, new TextRenderStyle(), Paint, out var textBounds);

		if (Icon == null) return;
		Paint.FilterQuality = SKFilterQuality.High;
		var iconRect = new SKRect(textBounds.Left - iconWidth, textBounds.Top, textBounds.Left, textBounds.Bottom);

		using var colorFilter = SKColorFilter.CreateBlendMode(Colors[text], SKBlendMode.Modulate);
		Paint.ColorFilter = colorFilter;
		uiCanvas.DrawImage(Icon, iconRect, Paint);
		Paint.ColorFilter = null;
	}

	public SKRect Size { get; set; }

	public override SKRect CalculateSize() => Size;

	private void ReleaseUnmanagedResources() {
		MutableString.ReturnCopy(in _textMutable);
	}

	public void Dispose() {
		ReleaseUnmanagedResources();
		GC.SuppressFinalize(this);
	}

	~UiAbstractButton() {
		ReleaseUnmanagedResources();
	}
}
