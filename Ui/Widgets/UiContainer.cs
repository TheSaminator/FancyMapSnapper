using SkiaSharp;

namespace FancyMapSnapper.Ui.Widgets;

public abstract class UiContainer : UiWidget, IDisposable {
	private IList<UiWidget> Children { get; } = new List<UiWidget>();

	public int NumChildren => Children.Count;
	public UiWidget ChildAt(int index) => Children[index];

	public virtual void AddChild(UiWidget child) {
		Children.Add(child);
	}

	public virtual void RemoveChild(UiWidget child) {
		Children.Remove(child);
	}

	public void ClearChildren() {
		while (NumChildren > 0)
			RemoveChild(ChildAt(0));
	}

	public abstract override void Tick(in SKRect size);

	protected virtual void Dispose(bool disposing) {
		if (!disposing) return;
		while (NumChildren > 0)
			if (ChildAt(0) is IDisposable disposable)
				disposable.Dispose();
	}

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
