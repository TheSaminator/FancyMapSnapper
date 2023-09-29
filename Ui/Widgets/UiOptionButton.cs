using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiOptionButton : UiAbstractButton {
	public RadioButtonGroup Group { get; set; }
	public int CheckedValue { get; set; }
	public bool IsChecked => Group.Value == CheckedValue;

	public UiOptionButton(RadioButtonGroup group, int checkedValue) {
		Group = group;
		CheckedValue = checkedValue;
	}

	protected override void Click() {
		Group.Value = CheckedValue;
	}

	public override void Render(SKCanvas uiCanvas, in SKRect size, IRootContext ctx) {
		if (!IsEnabled) {
			CustomColors = ColorScheme.DisabledTheme;
			Render(uiCanvas, in size, 3, 1, 4);
			CustomColors = null;
		}
		else if (IsClicked)
			Render(uiCanvas, in size, 4, 5, 1);
		else if (IsChecked)
			Render(uiCanvas, in size, 5, 2, 5);
		else if (IsHovered)
			Render(uiCanvas, in size, 4, 1, 5);
		else
			Render(uiCanvas, in size, 3, 1, 4);
	}
}
