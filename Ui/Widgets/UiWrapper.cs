namespace FancyMapSnapper.Ui.Widgets;

public abstract class UiWrapper : UiWidget, IDisposable {
	public UiWidget? Child { get; set; }

	protected virtual void Dispose(bool disposing) {
		if (!disposing) return;
		if (Child is IDisposable disposable)
			disposable.Dispose();
	}

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
