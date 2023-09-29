using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public sealed class UiToggleButton : UiAbstractButton {
	public CheckBoxGroup Group { get; set; }
	public int GroupIndex { get; set; }

	public bool IsChecked {
		get => Group[GroupIndex];
		set => Group[GroupIndex] = value;
	}

	public UiToggleButton(CheckBoxGroup group, int groupIndex) {
		Group = group;
		GroupIndex = groupIndex;
	}

	protected override void Click() {
		IsChecked ^= true;
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
