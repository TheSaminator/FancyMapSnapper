using System.Text;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FancyMapSnapper.Ui;

public enum UiInputEventPhase : byte {
	Focused,
	HotKeys,
	General
}

public enum InputEventType : byte {
	Unknown = 0,
	KeyboardKey,
	TextWritten,
	MouseButton,
	CursorMoved,
	ScrollWheel
}

public enum InputEventAction : byte {
	Press,
	Release,
	Repeat
}

public readonly struct InputEvent : IEquatable<InputEvent> {
	private readonly InputEventKeyboardKey _keyboardKey;
	private readonly InputEventTextWritten _textWritten;
	private readonly InputEventMouseButton _mouseButton;
	private readonly InputEventCursorMoved _cursorMoved;
	private readonly InputEventScrollWheel _scrollWheel;

	public InputEventType Type { get; }

	public InputEvent(InputEventKeyboardKey keyboardKey) {
		_keyboardKey = keyboardKey;
		Type = InputEventType.KeyboardKey;
	}

	public InputEvent(InputEventTextWritten textWritten) {
		_textWritten = textWritten;
		Type = InputEventType.TextWritten;
	}

	public InputEvent(InputEventMouseButton mouseButton) {
		_mouseButton = mouseButton;
		Type = InputEventType.MouseButton;
	}

	public InputEvent(InputEventCursorMoved cursorMoved) {
		_cursorMoved = cursorMoved;
		Type = InputEventType.CursorMoved;
	}

	public InputEvent(InputEventScrollWheel scrollWheel) {
		_scrollWheel = scrollWheel;
		Type = InputEventType.ScrollWheel;
	}

	public InputEventKeyboardKey? AsKeyboardKey => Type == InputEventType.KeyboardKey ? _keyboardKey : null;

	public InputEventTextWritten? AsTextWritten => Type == InputEventType.TextWritten ? _textWritten : null;

	public InputEventMouseButton? AsMouseButton => Type == InputEventType.MouseButton ? _mouseButton : null;

	public InputEventCursorMoved? AsCursorMoved => Type == InputEventType.CursorMoved ? _cursorMoved : null;

	public InputEventScrollWheel? AsScrollWheel => Type == InputEventType.ScrollWheel ? _scrollWheel : null;

	public static bool operator ==(InputEvent a, InputEvent b) {
		return a.Equals(b);
	}

	public static bool operator !=(InputEvent a, InputEvent b) {
		return !(a == b);
	}

	public bool Equals(InputEvent other) {
		return Type == other.Type && Type switch {
			InputEventType.Unknown => true,
			InputEventType.KeyboardKey => _keyboardKey == other._keyboardKey,
			InputEventType.MouseButton => _mouseButton == other._mouseButton,
			InputEventType.CursorMoved => _cursorMoved == other._cursorMoved,
			InputEventType.ScrollWheel => _scrollWheel == other._scrollWheel,
			InputEventType.TextWritten => _textWritten == other._textWritten,
			_ => throw new ArgumentOutOfRangeException($"Type is forbidden value {(byte)Type}")
		};
	}

	public override bool Equals(object? obj) {
		return obj is InputEvent other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine(Type, Type switch {
			InputEventType.Unknown => 0,
			InputEventType.KeyboardKey => _keyboardKey.GetHashCode(),
			InputEventType.MouseButton => _mouseButton.GetHashCode(),
			InputEventType.CursorMoved => _cursorMoved.GetHashCode(),
			InputEventType.ScrollWheel => _scrollWheel.GetHashCode(),
			InputEventType.TextWritten => _textWritten.GetHashCode(),
			_ => throw new ArgumentOutOfRangeException($"Type is forbidden value {(byte)Type}")
		});
	}
}

public readonly struct InputEventKeyboardKey : IEquatable<InputEventKeyboardKey> {
	public readonly Keys KeyboardKey;
	public readonly InputEventAction KeyAction;

	public InputEventKeyboardKey(Keys key, InputEventAction keyAction) {
		KeyboardKey = key;
		KeyAction = keyAction;
	}

	public static bool operator ==(InputEventKeyboardKey a, InputEventKeyboardKey b) {
		return a.Equals(b);
	}

	public static bool operator !=(InputEventKeyboardKey a, InputEventKeyboardKey b) {
		return !(a == b);
	}

	public bool Equals(InputEventKeyboardKey other) {
		return KeyboardKey == other.KeyboardKey && KeyAction == other.KeyAction;
	}

	public override bool Equals(object? obj) {
		return obj is InputEventKeyboardKey other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine((int)KeyboardKey, (int)KeyAction);
	}
}

public readonly struct InputEventTextWritten : IEquatable<InputEventTextWritten> {
	public readonly Rune CodePoint;

	public InputEventTextWritten(Rune rune) {
		CodePoint = rune;
	}

	public static bool operator ==(InputEventTextWritten a, InputEventTextWritten b) {
		return a.Equals(b);
	}

	public static bool operator !=(InputEventTextWritten a, InputEventTextWritten b) {
		return !(a == b);
	}

	public bool Equals(InputEventTextWritten other) {
		return CodePoint == other.CodePoint;
	}

	public override bool Equals(object? obj) {
		return obj is InputEventTextWritten other && Equals(other);
	}

	public override int GetHashCode() {
		return CodePoint.Value;
	}
}

public readonly struct InputEventMouseButton : IEquatable<InputEventMouseButton> {
	public readonly MouseButton Button;
	public readonly InputEventAction ButtonAction;

	public InputEventMouseButton(MouseButton btn, InputEventAction btnAction) {
		Button = btn;
		ButtonAction = btnAction;
	}

	public static bool operator ==(InputEventMouseButton a, InputEventMouseButton b) {
		return a.Equals(b);
	}

	public static bool operator !=(InputEventMouseButton a, InputEventMouseButton b) {
		return !(a == b);
	}

	public bool Equals(InputEventMouseButton other) {
		return Button == other.Button && ButtonAction == other.ButtonAction;
	}

	public override bool Equals(object? obj) {
		return obj is InputEventMouseButton other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine((int)Button, (int)ButtonAction);
	}
}

public readonly struct InputEventCursorMoved : IEquatable<InputEventCursorMoved> {
	public readonly Vector2 OldPosition;
	public readonly Vector2 NewPosition;

	public InputEventCursorMoved(Vector2 prev, Vector2 curr) {
		OldPosition = prev;
		NewPosition = curr;
	}

	public static bool operator ==(InputEventCursorMoved a, InputEventCursorMoved b) {
		return a.Equals(b);
	}

	public static bool operator !=(InputEventCursorMoved a, InputEventCursorMoved b) {
		return !(a == b);
	}

	public bool Equals(InputEventCursorMoved other) {
		return OldPosition.Equals(other.OldPosition) && NewPosition.Equals(other.NewPosition);
	}

	public override bool Equals(object? obj) {
		return obj is InputEventCursorMoved other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine(OldPosition, NewPosition);
	}
}

public readonly struct InputEventScrollWheel : IEquatable<InputEventScrollWheel> {
	public readonly float AmountScrolled;

	public InputEventScrollWheel(float scrolled) {
		AmountScrolled = scrolled;
	}

	public static bool operator ==(InputEventScrollWheel a, InputEventScrollWheel b) {
		return a.Equals(b);
	}

	public static bool operator !=(InputEventScrollWheel a, InputEventScrollWheel b) {
		return !(a == b);
	}

	public bool Equals(InputEventScrollWheel other) {
		return AmountScrolled.Equals(other.AmountScrolled);
	}

	public override bool Equals(object? obj) {
		return obj is InputEventScrollWheel other && Equals(other);
	}

	public override int GetHashCode() {
		return AmountScrolled.GetHashCode();
	}
}
