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
	private readonly UiLabel _errorMessage = new() { AnchorPoint = new SKPoint(800, 720), Style = new TextStyle { Color = new SKColor(0xFF, 0x55, 0x55), FontSize = 24 } };

	private readonly UiButton _add = new() { Size = new SKRect(500, 600, 784, 648), Text = "Add" };
	private readonly UiButton _cancel = new() { Size = new SKRect(816, 600, 1100, 648), Text = "Cancel" };

	private readonly UiRoot _ui = new();

	private AddPlaceToListByAddress(List<string> places, string houseNum, string streetName, string city, string state, string postCode, string? errorMessage = null) {
		_places = places;

		_houseNum.Builder.Append(houseNum);
		_streetName.Builder.Append(streetName);
		_city.Builder.Append(city);
		_state.Builder.Append(state);
		_postCode.Builder.Append(postCode);
		if (errorMessage != null)
			_errorMessage.Text = errorMessage;
	}

	private static async Task<List<string>?> PerformAddressQuery(string houseNum, string streetName, string city, string state, string postCode) {
		var taskResult = await XmlHelper.ConstructOsmQuery(
			(scriptDoc, osmScript) => scriptDoc.AddressQuery(osmScript, houseNum, streetName, city, state, postCode)
		).GetPlaceXml();

		var osmRoot = taskResult["osm"];
		if (osmRoot == null) {
			await Console.Error.WriteLineAsync($"Got error document from address query: {taskResult.ToXmlString()}");
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

		var placesList = places.ToList();

		return placesList.Any() ? placesList : null;
	}

	private void AddFromAddress() {
		_add.IsEnabled = false;
		_cancel.IsEnabled = false;

		var houseNum = _houseNum.Builder.ToString();
		var streetName = _streetName.Builder.ToString();
		var city = _city.Builder.ToString();
		var state = _state.Builder.ToString();
		var postCode = _postCode.Builder.ToString();

		PerformAddressQuery(
			houseNum,
			streetName,
			city,
			state,
			postCode
		).ContinueWith(task => {
			var taskResult = task.Result;
			if (taskResult == null)
				FmsApp.Instance.PostAction(() => { _next = new AddPlaceToListByAddress(_places.ToList(), houseNum, streetName, city, state, postCode, "Unable to find location on OpenStreetMap with that address"); });
			else
				FmsApp.Instance.PostAction(() => {
					var places = _places.ToList();
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
		_ui.AddChild(_errorMessage);
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
