using FancyMapSnapper.Ui;
using FancyMapSnapper.Ui.Widgets;
using SkiaSharp;

namespace FancyMapSnapper.AppMode;

public abstract class ApplicationMode {
	public virtual void Initialize() { }

	public abstract void HandleInput(in InputEvent input);
	public abstract void Render(SKCanvas canvas);

	protected static SKPaint CreatePaint() => UiWidget.CreatePaint();

	public abstract ApplicationMode NextMode { get; }
}
