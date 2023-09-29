namespace FancyMapSnapper.Ui.Widgets;

public struct EventState {
	public bool IsHandled { get; private set; }

	public void Handle() {
		IsHandled = true;
	}
}
