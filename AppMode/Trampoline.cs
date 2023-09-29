using FancyMapSnapper.Ui;
using SkiaSharp;

namespace FancyMapSnapper.AppMode;

// This class is necessary so creating a UI doesn't make the program crash on startup
public class Trampoline : ApplicationMode {
	public override void HandleInput(in InputEvent input) { }

	public override void Render(SKCanvas canvas) { }

	public override ApplicationMode NextMode => new InitialPlaceListBuilding();
}
