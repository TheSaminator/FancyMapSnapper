using FancyMapSnapper.DataStructures;
using FancyMapSnapper.Ui;
using FancyMapSnapper.Ui.Widgets;
using SkiaSharp;

namespace FancyMapSnapper.AppMode;

public class InitialPlaceListBuilding : ApplicationMode {
	private readonly List<string> _places;

	public InitialPlaceListBuilding() {
		_places = new List<string>();
	}

	public InitialPlaceListBuilding(List<string> places) {
		_places = places;
	}

	private readonly UiRoot _ui = new();
	private readonly UiScrollPane _inputs = new() { OuterSize = new SKRect(400, 200, 1200, 640) };

	private readonly UiButton _addButton = new() {
		Size = new SKRect(400, 672, 784, 720),
		IsEnabled = true,
		Text = "+"
	};

	private readonly UiButton _removeButton = new() {
		Size = new SKRect(816, 672, 1200, 720),
		IsEnabled = false,
		Text = "-"
	};

	private readonly UiButton _doneButton = new() {
		Size = new SKRect(608, 752, 992, 800),
		IsEnabled = false,
		Text = "Build Map"
	};

	private const float InputBoxWidth = 656;
	private const float InputButtonSeparation = 32;
	private const float InputGroupHeight = 48;
	private const float InputGroupSeparation = 72;

	private void AddInputAtEnd() {
		AddInputAtEnd("");
	}

	private void AddInputAtEnd(string withText) {
		if (_inputs.NumChildren > 0) {
			var prevLastGroup = (UiGroup)_inputs.ChildAt(_inputs.NumChildren - 1);
			var moveDownButton = (UiButton)prevLastGroup.ChildAt(2);
			moveDownButton.IsEnabled = true;
		}

		var newIndex = _inputs.NumChildren;
		var yValue = newIndex * InputGroupSeparation;
		var newGroup = new UiGroup();
		var newInputBox = new UiInputField {
			Size = new SKRect(0, yValue + (InputGroupSeparation - InputGroupHeight) / 2, InputBoxWidth, yValue + (InputGroupSeparation + InputGroupHeight) / 2)
		};
		newInputBox.Builder.Append(withText);

		var newInputMoveUpButton = new UiButton {
			Size = new SKRect(InputBoxWidth + InputButtonSeparation / 2, yValue + (InputGroupSeparation - InputGroupHeight) / 2, InputBoxWidth + InputButtonSeparation / 2 + InputGroupHeight, yValue + (InputGroupSeparation + InputGroupHeight) / 2),
			IsEnabled = newIndex != 0,
			Text = "^"
		};
		var newInputMoveDownButton = new UiButton {
			Size = new SKRect(InputBoxWidth + InputButtonSeparation + InputGroupHeight, yValue + (InputGroupSeparation - InputGroupHeight) / 2, InputBoxWidth + InputButtonSeparation + InputGroupHeight * 2, yValue + (InputGroupSeparation + InputGroupHeight) / 2),
			IsEnabled = false,
			Text = "v"
		};
		newInputMoveUpButton.OnClick += () => SwapInputWithNext(newIndex - 1);
		newInputMoveDownButton.OnClick += () => SwapInputWithNext(newIndex);

		newGroup.AddChild(newInputBox);
		newGroup.AddChild(newInputMoveUpButton);
		newGroup.AddChild(newInputMoveDownButton);
		_inputs.AddChild(newGroup);

		_removeButton.IsEnabled = true;
		_doneButton.IsEnabled = true;
	}

	private void SwapInputWithNext(int index) {
		var inputGroup = (UiGroup)_inputs.ChildAt(index);
		var nextInputGroup = (UiGroup)_inputs.ChildAt(index + 1);

		var input = (UiInputField)inputGroup.ChildAt(0);
		var nextInput = (UiInputField)nextInputGroup.ChildAt(0);

		var inputValue = MutableString.RentedCopyOf(input.Builder);
		var nextInputValue = MutableString.RentedCopyOf(nextInput.Builder);

		input.Builder.Clear();
		input.Builder.Append(nextInputValue.AsSpan());

		nextInput.Builder.Clear();
		nextInput.Builder.Append(inputValue.AsSpan());

		MutableString.ReturnCopy(inputValue);
		MutableString.ReturnCopy(nextInputValue);
	}

	private void RemoveInputAtEnd() {
		var lastGroup = _inputs.ChildAt(_inputs.NumChildren - 1);
		_inputs.RemoveChild(lastGroup);

		if (_inputs.NumChildren == 0) {
			_removeButton.IsEnabled = false;
			_doneButton.IsEnabled = false;
			return;
		}

		var newLastGroup = (UiGroup)_inputs.ChildAt(_inputs.NumChildren - 1);
		var moveDownButton = (UiButton)newLastGroup.ChildAt(2);
		moveDownButton.IsEnabled = false;
	}

	private void DoneListing() {
		var places = new List<string>();

		for (var i = 0; i < _inputs.NumChildren; i++) {
			var group = (UiGroup)_inputs.ChildAt(i);
			var input = (UiInputField)group.ChildAt(0);
			var inputText = input.Builder.ToString();
			if (string.IsNullOrWhiteSpace(inputText)) continue;

			// Special case: abbreviating University Hospitals as UH
			if (inputText.StartsWith("UH "))
				inputText = "University Hospitals " + inputText[3..];
			places.Add(inputText);
		}

		if (places.Count > 0) {
			_next = new LoadingOsmData(places);
		}
	}

	public override void Initialize() {
		_ui.AddChild(_inputs);

		_addButton.OnClick += AddInputAtEnd;
		_removeButton.OnClick += RemoveInputAtEnd;
		_doneButton.OnClick += DoneListing;

		_ui.AddChild(_addButton);
		_ui.AddChild(_removeButton);
		_ui.AddChild(_doneButton);

		foreach (var place in _places) {
			if (!string.IsNullOrWhiteSpace(place))
				AddInputAtEnd(place);
		}
	}

	public override void HandleInput(in InputEvent input) {
		_ui.HandleInput(input);
	}

	public override void Render(SKCanvas canvas) {
		_ui.Tick();
		_ui.Render(canvas);
	}

	private ApplicationMode? _next;
	public override ApplicationMode NextMode => _next ?? this;
}
