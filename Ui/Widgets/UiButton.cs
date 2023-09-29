using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiButton : UiAbstractButton {
	public event Action? OnClick;

	protected override void Click() {
		OnClick?.Invoke();
	}

	public override void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx) {
		if (!IsEnabled) {
			CustomColors = ColorScheme.DisabledTheme;
			Render(uiCanvas, in size, 3, 1, 4);
			CustomColors = null;
		}
		else if (IsClicked)
			Render(uiCanvas, in size, 4, 5, 1);
		else if (IsHovered)
			Render(uiCanvas, in size, 4, 1, 5);
		else
			Render(uiCanvas, in size, 3, 1, 4);
	}
}
