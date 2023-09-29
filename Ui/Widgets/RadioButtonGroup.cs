namespace FancyMapSnapper.Ui.Widgets;

public sealed class RadioButtonGroup {
	public event Action<int, int>? OnChange;

	private int _value;

	public int Value {
		get => _value;
		set {
			if (_value != value)
				OnChange?.Invoke(_value, value);
			_value = value;
		}
	}

	public RadioButtonGroup(int initialValue) {
		_value = initialValue;
	}
}
