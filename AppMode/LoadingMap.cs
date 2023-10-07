using System.Xml;
using FancyMapSnapper.Mapping;
using FancyMapSnapper.Ui;
using FancyMapSnapper.Ui.Widgets;
using SkiaSharp;

namespace FancyMapSnapper.AppMode;

public class LoadingMap : ApplicationMode {
	private readonly List<string> _places;
	private readonly List<string> _saveFiles = new();

	public LoadingMap(List<string> places) {
		_places = places;

		var saves = Directory.CreateDirectory("saves");
		foreach (var fileInfo in saves.GetFiles()) {
			if (!fileInfo.Name.EndsWith(".xml")) continue;

			_saveFiles.Add(fileInfo.Name[..^".xml".Length]);
		}
	}

	private readonly UiRoot _ui = new();

	private void Load(string name) {
		var saves = Directory.CreateDirectory("saves");
		var loadPath = Path.Combine(saves.FullName, name + ".xml");
		var xmlDoc = new XmlDocument();
		xmlDoc.LoadXml(File.ReadAllText(loadPath));
		var (places, result) = OsmQueryResult.FromXml(xmlDoc);

		_next = new CustomizingMap(places, result);
	}

	private void Cancel() {
		_next = new InitialPlaceListBuilding(_places);
	}

	private const float InputButtonWidth = 784;
	private const float InputButtonHeight = 48;
	private const float InputButtonSeparation = 72;

	public override void Initialize() {
		var scrollPane = new UiScrollPane { OuterSize = new SKRect(400, 200, 1200, 640) };

		float y = 0;
		foreach (var saveFile in _saveFiles) {
			var loadButton = new UiButton {
				Text = saveFile,
				IsEnabled = true,
				Size = new SKRect(0, y + (InputButtonSeparation - InputButtonHeight) / 2, InputButtonWidth, y + (InputButtonSeparation + InputButtonHeight) / 2)
			};

			loadButton.OnClick += () => Load(saveFile);
			scrollPane.AddChild(loadButton);

			y += InputButtonSeparation;
		}

		_ui.AddChild(scrollPane);

		var cancelButton = new UiButton {
			Text = "Cancel Load",
			IsEnabled = true,
			Size = new SKRect(400, 640 + (InputButtonSeparation - InputButtonHeight) / 2, 1200, 640 + (InputButtonSeparation + InputButtonHeight) / 2)
		};

		cancelButton.OnClick += Cancel;

		_ui.AddChild(cancelButton);
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
