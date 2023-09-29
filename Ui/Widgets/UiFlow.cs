using System.Buffers;
using FancyMapSnapper.DataStructures;
using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiFlow : UiContainer {
	public override void HandleInput(in InputEvent input, in SKRect size, ref EventState isHandled, IRootContext ctx, UiInputEventPhase phase) {
		var rootPoint = size.Location;

		var items = CalculateLines(out _);
		for (var i = 0; i < items.Count; i++) {
			var item = items[i];
			if (item.Widget == null)
				break;

			var widgetSize = size.CalculateChild(item.Widget);
			var widgetLocation = new SKRect(
				item.DrawAt.X + rootPoint.X,
				item.DrawAt.Y + rootPoint.Y,
				item.DrawAt.X + widgetSize.Width + rootPoint.X,
				item.DrawAt.Y + widgetSize.Height + rootPoint.Y
			);
			item.Widget.HandleInput(in input, in widgetLocation, ref isHandled, ctx, phase);
		}

		items.Free();
	}

	public override void Tick(in SKRect size) {
		var rootPoint = size.Location;

		var items = CalculateLines(out _);
		for (var i = 0; i < items.Count; i++) {
			var item = items[i];
			if (item.Widget == null)
				break;

			var widgetSize = size.CalculateChild(item.Widget);
			var widgetLocation = new SKRect(
				item.DrawAt.X + rootPoint.X,
				item.DrawAt.Y + rootPoint.Y,
				item.DrawAt.X + widgetSize.Width + rootPoint.X,
				item.DrawAt.Y + widgetSize.Height + rootPoint.Y
			);
			item.Widget.Tick(widgetLocation);
		}

		items.Free();
	}

	public override void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx) {
		var rootPoint = size.Location;

		var items = CalculateLines(out _);
		for (var i = 0; i < items.Count; i++) {
			var item = items[i];
			if (item.Widget == null)
				break;

			var widgetSize = size.CalculateChild(item.Widget);
			var widgetLocation = new SKRect(
				item.DrawAt.X + rootPoint.X,
				item.DrawAt.Y + rootPoint.Y,
				item.DrawAt.X + widgetSize.Width + rootPoint.X,
				item.DrawAt.Y + widgetSize.Height + rootPoint.Y
			);
			item.Widget.Render(uiCanvas, widgetLocation, ctx);
		}

		items.Free();
	}

	#region Flow calculation

	public float CalculateRealHeight() {
		CalculateLines(out var height);
		return height;
	}

	private static readonly ArrayPool<FlowItem> FlowItemPool = ArrayPool<FlowItem>.Shared;
	private static readonly ArrayPool<UiWidget> UiWidgetPool = ArrayPool<UiWidget>.Shared;

	private void CalculateLine(float lineWidth, float lineHeight, ref UnsophisticatedList<UiWidget> currLine, ref UnsophisticatedList<FlowItem> allItems, ref float height) {
		var widgetX = (Size.Width - lineWidth) * HAnchor switch {
			HorizontalAnchor.Left => 0,
			HorizontalAnchor.Middle => 0.5f,
			HorizontalAnchor.Right => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(HAnchor), HAnchor, null)
		};

		for (var i = 0; i < currLine.Count; i++) {
			var widget = currLine[i];
			var widgetSize = widget.CalculateSize();
			var widgetY = height + (lineHeight - widgetSize.Height) * LineVerticalAlign switch {
				VerticalAnchor.Top => 0,
				VerticalAnchor.Middle => 0.5f,
				VerticalAnchor.Bottom => 1,
				_ => throw new ArgumentOutOfRangeException(nameof(LineVerticalAlign), LineVerticalAlign, null)
			};

			allItems.Push(new FlowItem {
				DrawAt = new SKPoint(widgetX, widgetY),
				Widget = widget
			});

			widgetX += widgetSize.Width;
		}

		height += lineHeight;
	}

	private UnsophisticatedList<FlowItem> CalculateLines(out float height) {
		var allItems = new UnsophisticatedList<FlowItem>(NumChildren, FlowItemPool);
		height = 0f;

		var lineWidth = 0f;
		var lineHeight = 0f;
		var currLine = new UnsophisticatedList<UiWidget>(NumChildren, UiWidgetPool);
		for (var i = 0; i < NumChildren; i++) {
			var child = ChildAt(i);
			var childSize = child.CalculateSize();
			if (child is UiVerticalFlowSpacer || lineWidth + childSize.Width > Size.Width) {
				CalculateLine(lineWidth, lineHeight, ref currLine, ref allItems, ref height);

				currLine.Clear();
				if (child is UiHorizontalFlowSpacer) {
					lineWidth = 0;
					lineHeight = 0;
				}
				else {
					currLine.Push(child);
					lineWidth = childSize.Width;
					lineHeight = childSize.Height;
				}
			}
			else {
				lineWidth += childSize.Width;
				if (childSize.Height > lineHeight)
					lineHeight = childSize.Height;
				currLine.Push(child);
			}
		}

		CalculateLine(lineWidth, lineHeight, ref currLine, ref allItems, ref height);

		currLine.Free();

		var shiftY = (Size.Height - height) * VAnchor switch {
			VerticalAnchor.Top => 0,
			VerticalAnchor.Middle => 0.5f,
			VerticalAnchor.Bottom => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(VAnchor), VAnchor, null)
		};

		for (var i = 0; i < allItems.Count; i++) {
			var item = allItems[i];
			if (item.Widget == null)
				break;

			item.DrawAt.Y += shiftY;
			allItems[i] = item;
		}

		return allItems;
	}

	#endregion

	#region Text processing

	private delegate UiWidget LabelFactory(string labelText, in TextStyle labelStyle);

	private void AddText(ReadOnlySpan<char> text, in TextStyle style, LabelFactory labelFactory) {
		var textMutable = MutableString.RentedCopyOf(text);

		Paint.SelectFont(style.FontSpec);
		Paint.TextSize = style.FontSize;

		var spaceWidth = Paint.MeasureText(" ");

		var prevLineIndex = -1;
		var lineIndex = -1;
		while (textMutable.NextOccurrence('\n', ref lineIndex)) {
			prevLineIndex++;
			var line = textMutable.Clip(prevLineIndex, lineIndex - prevLineIndex);

			var prevWordIndex = -1;
			var wordIndex = -1;
			while (line.NextOccurrence(' ', ref wordIndex)) {
				prevWordIndex++;
				var word = line.Clip(prevWordIndex, wordIndex - prevWordIndex);
				if (word.Length > 0)
					base.AddChild(labelFactory(word.ToString(), in style));
				if (wordIndex < line.Length)
					base.AddChild(new UiHorizontalFlowSpacer { Width = spaceWidth });

				prevWordIndex = wordIndex;
			}

			if (lineIndex < textMutable.Length)
				base.AddChild(new UiVerticalFlowSpacer());

			prevLineIndex = lineIndex;
		}

		MutableString.ReturnCopy(in textMutable);
	}

	public void AddText(ReadOnlySpan<char> text, in TextStyle style) {
		AddText(text, style, (string labelText, in TextStyle labelStyle) => new UiLabel {
			Style = labelStyle,
			Text = labelText,
			HAnchor = HorizontalAnchor.Left,
			VAnchor = VerticalAnchor.Top
		});
	}

	public void AddLinkText(ReadOnlySpan<char> text, in TextStyle style, Action? onClick) {
		var group = new LinkLabelGroup();
		group.OnClick += onClick;
		AddText(text, style, (string labelText, in TextStyle labelStyle) => new UiLinkLabel(group) {
			Style = labelStyle,
			Text = labelText,
			HAnchor = HorizontalAnchor.Left,
			VAnchor = VerticalAnchor.Top
		});
	}

	public void AddText(UiLabel label) {
		var style = label.Style;
		AddText(label.TextMutable.AsSpan(), in style);
	}

	public void AddLinkText(UiLinkLabel label) {
		var style = label.Style;
		AddLinkText(label.TextMutable.AsSpan(), in style, label.Click);
	}

	public void AppendChar(char ch, in TextStyle style) {
		switch (ch) {
			case '\n':
				base.AddChild(new UiVerticalFlowSpacer());
				return;
			case ' ': {
				Paint.SelectFont(style.FontSpec);
				Paint.TextSize = style.FontSize;

				var spaceWidth = Paint.MeasureText(" ");
				base.AddChild(new UiHorizontalFlowSpacer { Width = spaceWidth });
				return;
			}
		}

		var charSpan = new ReadOnlySpan<char>(in ch);
		if (NumChildren == 0)
			AddText(charSpan, style);
		else {
			var lastChild = ChildAt(NumChildren - 1);
			if (lastChild is UiLabel label && label.Style == style)
				label.AppendText(charSpan);
			else
				AddText(charSpan, style);
		}
	}

	public void AddStyledImage(SKImage image, in TextStyle style, bool applyColor = false) {
		Paint.SelectFont(style.FontSpec);
		Paint.TextSize = style.FontSize;
		var fontMetrics = Paint.FontMetrics;
		var textHeight = fontMetrics.Leading + fontMetrics.Bottom - fontMetrics.Top;

		var scaling = textHeight / image.Height;
		var uiImage = new UiImage {
			Image = image,
			ImageScaling = scaling,
			HAnchor = HorizontalAnchor.Left,
			VAnchor = VerticalAnchor.Top
		};

		if (applyColor)
			uiImage.MultiplyColor = style.Color;

		base.AddChild(uiImage);
	}

	#endregion

	public override void AddChild(UiWidget child) {
		switch (child) {
			case UiLabel label:
				AddText(label);
				break;
			case UiLinkLabel linklabel:
				AddLinkText(linklabel);
				break;
			default:
				base.AddChild(child);
				break;
		}
	}

	public HorizontalAnchor HAnchor { get; set; } = HorizontalAnchor.Left;
	public VerticalAnchor LineVerticalAlign { get; set; } = VerticalAnchor.Middle;
	public VerticalAnchor VAnchor { get; set; } = VerticalAnchor.Top;

	public SKRect Size { get; set; }

	public override SKRect CalculateSize() => Size;
}

public struct FlowItem {
	public UiWidget? Widget;
	public SKPoint DrawAt;
}
