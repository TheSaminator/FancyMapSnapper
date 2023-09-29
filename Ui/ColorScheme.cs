using SkiaSharp;

namespace FancyMapSnapper.Ui;

public readonly struct ColorScheme {
	private readonly ColorStrength _red;
	private readonly ColorStrength _green;
	private readonly ColorStrength _blue;

	public ColorScheme(ColorStrength red, ColorStrength green, ColorStrength blue) {
		_red = red;
		_green = green;
		_blue = blue;
	}

	public SKColor this[int index] => index switch {
		1 => new SKColor((byte)((byte)_red * 0x11), (byte)((byte)_green * 0x11), (byte)((byte)_blue * 0x11), 0xFF),
		2 => new SKColor((byte)((byte)_red * 0x22), (byte)((byte)_green * 0x22), (byte)((byte)_blue * 0x22), 0xFF),
		3 => new SKColor((byte)((byte)_red * 0x33), (byte)((byte)_green * 0x33), (byte)((byte)_blue * 0x33), 0xFF),
		4 => new SKColor((byte)((byte)_red * 0x44), (byte)((byte)_green * 0x44), (byte)((byte)_blue * 0x44), 0xFF),
		5 => new SKColor((byte)((byte)_red * 0x55), (byte)((byte)_green * 0x55), (byte)((byte)_blue * 0x55), 0xFF),
		_ => throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be in range [1, 5]")
	};

	private static ColorScheme _uiTheme = new(ColorStrength.One, ColorStrength.Two, ColorStrength.Three);
	private static ColorScheme _disabledTheme = new(ColorStrength.Two, ColorStrength.Two, ColorStrength.Two);

	public static ref ColorScheme UiTheme => ref _uiTheme;
	public static ref ColorScheme DisabledTheme => ref _disabledTheme;
}

public enum ColorStrength : byte {
	Zero = 0,
	One = 1,
	Two = 2,
	Three = 3
}
