using System.Xml;
using FancyMapSnapper.Mapping;
using FancyMapSnapper.Ui;
using FancyMapSnapper.Ui.Widgets;
using SkiaSharp;

namespace FancyMapSnapper.AppMode;

public class AddPlaceToListByAddress : ApplicationMode {
	private readonly List<string> _places;

	public AddPlaceToListByAddress(List<string> places) {
		_places = places;
	}

	private readonly UiInputField _houseNum = new() { Size = new SKRect(400, 386, 584, 434) };
	private readonly UiInputField _streetName = new() { Size = new SKRect(616, 386, 1200, 434) };
	private readonly UiInputField _city = new() { Size = new SKRect(400, 466, 784, 514) };
	private readonly UiInputField _state = new() { Size = new SKRect(816, 466, 984, 514) };
	private readonly UiInputField _postCode = new() { Size = new SKRect(1016, 466, 1200, 514) };

	private readonly UiButton _add = new() { Size = new SKRect(500, 600, 784, 648), Text = "Add" };
	private readonly UiButton _cancel = new() { Size = new SKRect(816, 600, 1100, 648), Text = "Cancel" };

	private readonly UiRoot _ui = new();

	private static async Task<List<string>?> PerformAddressQuery(string houseNum, string streetName, string city, string state, string postCode) {
		var taskResult = await XmlHelper.ConstructOsmQuery(
			(scriptDoc, osmScript) => scriptDoc.AddressQuery(osmScript, houseNum, streetName, city, state, postCode)
		).GetPlaceXml();

		var osmRoot = taskResult["osm"];
		if (osmRoot == null) {
			Console.WriteLine($"Got error document: {taskResult.ToXmlString()}");
			return null;
		}

		// Clear out extra stuff that we don't need
		var noteElement = osmRoot["note"];
		if (noteElement != null) osmRoot.RemoveChild(noteElement);

		var metaElement = osmRoot["meta"];
		if (metaElement != null) osmRoot.RemoveChild(metaElement);

		var places = from XmlNode wayNode in osmRoot.GetElementsByTagName("way")
			where wayNode is XmlElement
			select "id:" + ((XmlElement)wayNode).GetAttribute("id");

		return places.ToList();
	}

	private void AddFromAddress() {
		_add.IsEnabled = false;
		_cancel.IsEnabled = false;

		PerformAddressQuery(
			_houseNum.Builder.ToString(),
			_streetName.Builder.ToString(),
			_city.Builder.ToString(),
			_state.Builder.ToString(),
			_postCode.Builder.ToString()
		).ContinueWith(task => {
			var taskResult = task.Result;
			FmsApp.Instance.PostAction(() => {
				var places = _places.ToList();
				if (taskResult != null)
					places.AddRange(taskResult);
				_next = new InitialPlaceListBuilding(places);
			});
		});
	}

	private void Cancel() {
		_next = new InitialPlaceListBuilding(_places);
	}

	public override void Initialize() {
		_add.OnClick += AddFromAddress;
		_cancel.OnClick += Cancel;

		_ui.AddChild(_houseNum);
		_ui.AddChild(_streetName);
		_ui.AddChild(_city);
		_ui.AddChild(_state);
		_ui.AddChild(_postCode);
		_ui.AddChild(_add);
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
