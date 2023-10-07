using FancyMapSnapper.Mapping;
using FancyMapSnapper.Ui;
using FancyMapSnapper.Ui.Widgets;
using SkiaSharp;

namespace FancyMapSnapper.AppMode;

public class SavingMap : ApplicationMode {
	private readonly List<string> _places;
	private readonly OsmQueryResult _result;

	public SavingMap(List<string> places, OsmQueryResult result) {
		_places = places;
		_result = result;
	}

	private readonly UiInputField _saveName = new() { Size = new SKRect(400, 386, 1200, 434) };
	private readonly UiButton _save = new() { Size = new SKRect(400, 466, 784, 514), Text = "Save" };
	private readonly UiButton _cancel = new() { Size = new SKRect(816, 466, 1200, 514), Text = "Cancel" };
	private readonly UiRoot _ui = new();

	private void Save() {
		var xmlString = OsmQueryResult.ToXml(_places, _result).ToXmlString();
		var saves = Directory.CreateDirectory("saves");
		var saveName = _saveName.Builder.ToString();
		var forbidChars = Path.GetInvalidFileNameChars();
		if (saveName.Any(ch => forbidChars.Contains(ch)))
			return;

		var savePath = Path.Combine(saves.FullName, _saveName.Builder + ".xml");
		File.WriteAllText(savePath, xmlString);

		_next = new CustomizingMap(_places, _result);
	}

	private void Cancel() {
		_next = new CustomizingMap(_places, _result);
	}

	public override void Initialize() {
		_save.OnClick += Save;
		_cancel.OnClick += Cancel;

		_ui.AddChild(_saveName);
		_ui.AddChild(_save);
		_ui.AddChild(_cancel);
	}

	public override void HandleInput(in InputEvent input) {
		_ui.HandleInput(in input);
	}

	public override void Render(SKCanvas canvas) {
		_ui.Tick();
		_ui.Render(canvas);
	}

	private ApplicationMode? _next;
	public override ApplicationMode NextMode => _next ?? this;
}
