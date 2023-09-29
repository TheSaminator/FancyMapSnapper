namespace FancyMapSnapper.Ui.Widgets;

public sealed class CheckBoxGroup {
	public event Action<int, bool>? OnChange;

	private ulong _bits;

	public CheckBoxGroup(params int[] initialSets) {
		_bits = 0;
		for (var i = 0; i < initialSets.Length; i++)
			_bits |= 1ul << i;
	}

	public bool this[int index] {
		get => (_bits & (1ul << index)) != 0ul;
		set {
			var shift = 1ul << index;
			var prevValue = (_bits & shift) != 0ul;
			if (prevValue != value)
				OnChange?.Invoke(index, value);

			if (value)
				_bits |= shift;
			else
				_bits &= ~shift;
		}
	}
}
