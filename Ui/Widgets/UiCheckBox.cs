using FancyMapSnapper.DataStructures;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiCheckBox : UiWidget, IDisposable {
	public bool IsClicked { get; private set; }
	public bool IsEnabled { get; set; } = true;

	public CheckBoxGroup Group { get; set; }
	public int GroupIndex { get; set; }

	public bool IsChecked {
		get => Group[GroupIndex];
		set => Group[GroupIndex] = value;
	}

	public UiCheckBox(CheckBoxGroup group, int groupIndex) {
		Group = group;
		GroupIndex = groupIndex;
	}

	public ColorScheme? CustomColors { get; set; }
	public ColorScheme Colors => CustomColors ?? ColorScheme.UiTheme;
	public float BorderSize { get; set; } = 3;
	public float BoxPaddingSize { get; set; } = 1;
	public float BorderRadius { get; set; } = 0;
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
			IsChecked ^= true;
	}

	public override void Tick(in SKRect size) {
		base.Tick(in size);
		if (!IsHovered)
			IsClicked = false;
	}

	private static readonly SKPoint[] Points = {
		new(0.0f, 0.333f),
		new(0.333f, 0.667f),
		new(1.0f, 0.0f),
		new(1.0f, 0.333f),
		new(0.333f, 1.0f),
		new(0.0f, 0.667f)
	};

	private static SKPath GetPath(SKRect size) {
		var path = new SKPath();
		for (var i = 0; i < Points.Length; i++) {
			var point = new SKPoint(Points[i].X * size.Width + size.Left, Points[i].Y * size.Height + size.Top);
			if (i == 0)
				path.MoveTo(point);
			else
				path.LineTo(point);
		}

		path.Close();
		return path;
	}

	private void Render(SKCanvas uiCanvas, SKRect size, int stroke, int fill, int text, int check) {
		var boxSize = size with { Right = size.Left + Size };

		var r = BorderRadius;
		Paint.Color = Colors[stroke];
		uiCanvas.DrawRoundRect(boxSize, r, r, Paint);

		r -= BorderSize;
		Paint.Color = Colors[fill];
		uiCanvas.DrawRoundRect(boxSize.Inset(BorderSize), r, r, Paint);

		Paint.Color = Colors[text];
		Paint.SelectFont(new FontSpec { Bold = true });
		Paint.TextSize = Size * 3 / 4;
		var textPoint = new SKPoint(size.Right, size.MidY);
		uiCanvas.DrawAnchoredText(in _textMutable, textPoint, HorizontalAnchor.Right, VerticalAnchor.Middle, new TextRenderStyle(), Paint, out _);

		if (!IsChecked) return;
		using var path = GetPath(boxSize.Inset(BorderSize + BoxPaddingSize));
		Paint.Color = Colors[check];
		uiCanvas.DrawPath(path, Paint);
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

	~UiCheckBox() {
		ReleaseUnmanagedResources();
	}
}
