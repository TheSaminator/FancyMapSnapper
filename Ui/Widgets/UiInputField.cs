using System.Text;
using FancyMapSnapper.DataStructures;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiInputField : UiFocusable {
	public ColorScheme? CustomColors { get; set; }
	public ColorScheme Colors => CustomColors ?? ColorScheme.UiTheme;
	public float BorderSize { get; set; } = 2;
	public float PaddingSize { get; set; } = 4;
	public float BorderRadius { get; set; } = 8;
	public float CursorWidthRadius { get; set; } = 1;
	public float CursorHeightInset { get; set; } = 3;

	public float FontSize { get; set; } = 24;

	public event Action<StringBuilder>? OnSubmit;

	public Predicate<StringBuilder>? InputFilter { get; set; }
	public Func<char, char>? OutputFilter { get; set; }

	public StringBuilder Builder { get; } = new();

	private static void RestoreFromCopy(StringBuilder builder, in MutableString copy) {
		builder.Clear();
		builder.Append(copy.AsSpan());
	}

	private bool UpdateBuilderIfMatchesRegex(StringBuilder builder, ref MutableString copy) {
		if (InputFilter == null || InputFilter(Builder)) {
			MutableString.ReturnCopy(in copy);
			return true;
		}

		RestoreFromCopy(builder, in copy);
		MutableString.ReturnCopy(in copy);
		return false;
	}

	private int _cursorPosition;

	private float _drawShift;

	private void MoveCursorTo(int newPos, in SKRect size) {
		if (newPos < 0)
			newPos = 0;
		if (newPos > Builder.Length)
			newPos = Builder.Length;

		_cursorPosition = newPos;

		KeepCursorInBox(size);
	}

	private void MoveCursorBy(int delta, in SKRect size) {
		MoveCursorTo(_cursorPosition + delta, in size);
	}

	private void KeepCursorInBox(in SKRect size) {
		Paint.SelectFont(new FontSpec());
		Paint.TextSize = FontSize;

		var drawText = MutableString.RentedCopyOf(Builder);
		drawText.Length = _cursorPosition;
		var measure = Paint.MeasureTextFull(in drawText, out _);

		if (_drawShift > measure)
			_drawShift = measure;
		if (_drawShift < measure - (size.Width - PaddingSize * 2))
			_drawShift = measure - (size.Width - PaddingSize * 2);

		MutableString.ReturnCopy(in drawText);
	}

	public override void HandleInput(in InputEvent input, in SKRect size, ref EventState isHandled, IRootContext ctx, UiInputEventPhase phase) {
		if (isHandled.IsHandled) return;

		if (phase == UiInputEventPhase.Focused) {
			if (ctx.FocusedWidget != this)
				return;

			if (input.Type == InputEventType.TextWritten) {
				var copy = MutableString.RentedCopyOf(Builder);

				var text = input.AsTextWritten!.Value.CodePoint;
				var length = text.Utf16SequenceLength;
				Span<char> span = stackalloc char[length];
				text.EncodeToUtf16(span);
				Builder.Insert(_cursorPosition, span);

				if (UpdateBuilderIfMatchesRegex(Builder, ref copy))
					MoveCursorBy(length, in size);

				isHandled.Handle();
				return;
			}

			if (input.Type != InputEventType.KeyboardKey) return;

			var keyEvent = input.AsKeyboardKey!.Value;
			if (keyEvent.KeyAction == InputEventAction.Release) return;

			switch (keyEvent.KeyboardKey) {
				case Keys.Enter:
					OnSubmit?.Invoke(Builder);
					isHandled.Handle();
					return;
				case Keys.Backspace: {
					if (_cursorPosition <= 0) {
						isHandled.Handle();
						return;
					}

					var copy = MutableString.RentedCopyOf(Builder);
					Builder.Remove(_cursorPosition - 1, 1);
					if (UpdateBuilderIfMatchesRegex(Builder, ref copy))
						MoveCursorBy(-1, in size);

					isHandled.Handle();
					return;
				}
				case Keys.Delete: {
					if (_cursorPosition >= Builder.Length) {
						isHandled.Handle();
						return;
					}

					var copy = MutableString.RentedCopyOf(Builder);
					Builder.Remove(_cursorPosition, 1);

					UpdateBuilderIfMatchesRegex(Builder, ref copy);

					isHandled.Handle();
					return;
				}
				case Keys.Left:
					MoveCursorBy(-1, in size);
					isHandled.Handle();
					return;
				case Keys.Right:
					MoveCursorBy(1, in size);
					isHandled.Handle();
					return;
				case Keys.Home:
				case Keys.Up:
					MoveCursorTo(0, in size);
					isHandled.Handle();
					return;
				case Keys.End:
				case Keys.Down:
					MoveCursorTo(Builder.Length, in size);
					isHandled.Handle();
					return;
				default:
					return;
			}
		}

		if (phase == UiInputEventPhase.HotKeys) return;

		if (input.Type != InputEventType.MouseButton) {
			if (input.Type == InputEventType.CursorMoved && IsHovered)
				isHandled.Handle();
			return;
		}

		var mouseButton = input.AsMouseButton!.Value;
		if (mouseButton.Button != MouseButton.Left) {
			if (IsHovered)
				isHandled.Handle();
			return;
		}

		if (IsHovered) {
			ctx.FocusedWidget = this;

			Paint.SelectFont(new FontSpec());
			Paint.TextSize = FontSize;

			var positionInText = FmsApp.Instance.MousePosition.X - (size.Left + PaddingSize - _drawShift);
			var measuredText = MutableString.RentedCopyOf(Builder);
			var minDistance = 0f;
			int i;
			for (i = 0; i <= Builder.Length; i++) {
				measuredText.Length = i;
				var textLength = Paint.MeasureTextFull(measuredText, out _);
				var distance = MathF.Abs(positionInText - textLength);

				if (minDistance > distance || i == 0)
					minDistance = distance;
				else break;
			}

			MoveCursorTo(i - 1, in size);
		}
		else if (ctx.FocusedWidget == this)
			ctx.FocusedWidget = null;

		if (IsHovered)
			isHandled.Handle();
	}

	private void Render(SKCanvas uiCanvas, SKRect size, int stroke, int fill, int text, int cursor) {
		var r = BorderRadius;
		Paint.Color = Colors[stroke];
		uiCanvas.DrawRoundRect(size, r, r, Paint);

		r -= BorderSize;
		Paint.Color = Colors[fill];
		uiCanvas.DrawRoundRect(size.Inset(BorderSize), r, r, Paint);

		Paint.Color = Colors[text];
		Paint.SelectFont(new FontSpec());
		Paint.TextSize = FontSize;

		uiCanvas.Save();
		uiCanvas.ClipRect(size.Inset(PaddingSize));

		var drawText = MutableString.RentedCopyOf(Builder);
		if (OutputFilter != null)
			for (var i = 0; i < drawText.Length; i++)
				drawText.Chars[i] = OutputFilter(drawText.Chars[i]);

		drawText.Length = _cursorPosition;
		var drawTextAt = new SKPoint(size.Left + PaddingSize - _drawShift, size.MidY);
		var textWidth = uiCanvas.DrawAnchoredText(in drawText, in drawTextAt, HorizontalAnchor.Left, VerticalAnchor.Middle, new TextRenderStyle(), Paint, out _);
		drawTextAt.X += textWidth;

		drawText.Start = _cursorPosition;
		drawText.Length = Builder.Length - _cursorPosition;
		uiCanvas.DrawAnchoredText(in drawText, in drawTextAt, HorizontalAnchor.Left, VerticalAnchor.Middle, new TextRenderStyle(), Paint, out _);

		uiCanvas.Restore();

		MutableString.ReturnCopy(in drawText);

		if (cursor == 0) return;
		Paint.Color = Colors[cursor];
		uiCanvas.DrawRect(new SKRect(drawTextAt.X - CursorWidthRadius, size.Top + CursorHeightInset, drawTextAt.X + CursorWidthRadius, size.Bottom - CursorHeightInset), Paint);
	}

	public override void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx) {
		MoveCursorBy(0, in size);

		Render(uiCanvas, size, IsHovered ? 4 : 3, 1, 5, ctx.FocusedWidget == this ? 5 : 0);
	}

	public SKRect Size { get; set; }

	public override SKRect CalculateSize() => Size;
}
