namespace FancyMapSnapper.Ui.Widgets;

public sealed class LinkLabelGroup {
	public event Action? OnClick;

	internal void Click() {
		OnClick?.Invoke();
	}

	internal IList<UiLinkLabel> Links { get; } = new List<UiLinkLabel>();

	internal bool ShouldUnderline => Links.Any(link => link.IsHovered);
}
