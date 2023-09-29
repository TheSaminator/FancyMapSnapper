using System.Buffers;
using System.Text;

namespace FancyMapSnapper.DataStructures;

public struct MutableString {
	internal char[] Chars = Array.Empty<char>();
	internal int Start;
	internal int Length;

	public MutableString() { }

	public MutableString Clip(int start) {
		return Clip(start, Length - start);
	}

	public readonly MutableString this[Range range] {
		get {
			var (offset, length) = range.GetOffsetAndLength(Length);
			return Clip(offset, length);
		}
	}

	public readonly MutableString Clip(int start, int length) {
		if (start < 0 || start > Length)
			throw new ArgumentOutOfRangeException(nameof(start), start, "Start of clipped MutableString may not be outside range [0, " + Length + "]");
		if (length < 0 || start + length > Length)
			throw new ArgumentOutOfRangeException(nameof(length), length, "Length of clipped MutableString may not be outside range [0, " + (Length - start) + "]");

		return new MutableString {
			Chars = Chars,
			Start = Start + start,
			Length = length
		};
	}

	public ref char GetPinnableReference() => ref Chars[Start];

	public readonly Span<char> AsSpan() {
		return Chars.AsSpan(Start, Length);
	}

	public static readonly ArrayPool<char> CharPool = ArrayPool<char>.Shared;

	public static MutableString RentedEmpty() {
		return new MutableString {
			Chars = CharPool.Rent(0),
			Start = 0,
			Length = 0
		};
	}

	public static MutableString RentedBlank(int length) {
		return new MutableString {
			Chars = CharPool.Rent(length),
			Start = 0,
			Length = length
		};
	}

	public static MutableString RentedCopyOf(ReadOnlySpan<char> chars) {
		var array = CharPool.Rent(chars.Length);
		chars.CopyTo(array.AsSpan());
		return new MutableString {
			Chars = array,
			Start = 0,
			Length = chars.Length
		};
	}

	public static MutableString RentedCopyOf(StringBuilder chars) {
		var array = CharPool.Rent(chars.Length);
		chars.CopyTo(0, array.AsSpan(), chars.Length);
		return new MutableString {
			Chars = array,
			Start = 0,
			Length = chars.Length
		};
	}

	public static void ReturnCopy(in MutableString copy) {
		CharPool.Return(copy.Chars);
	}

	public override string ToString() {
		return new string(Chars, Start, Length);
	}

	public char this[int index] {
		readonly get {
			if (index < 0 || index >= Length)
				throw new ArgumentOutOfRangeException(nameof(index), index, "Index into MutableString may not be outside range [0, " + Length + "]");
			return Chars[index + Start];
		}
		set {
			if (index < 0 || index >= Length)
				throw new ArgumentOutOfRangeException(nameof(index), index, "Index into MutableString may not be outside range [0, " + Length + "]");
			Chars[index + Start] = value;
		}
	}

	public readonly bool FirstOccurrence(char searchFor, out int index) {
		index = -1;
		NextOccurrence(searchFor, ref index);
		return index <= Length;
	}

	public readonly bool LastOccurrence(char searchFor, out int index) {
		index = Chars.Length;
		PrevOccurrence(searchFor, ref index);
		return index >= 0;
	}

	public readonly bool NextOccurrence(char searchFor, ref int index) {
		var isValid = index < Length;

		do
			index++;
		while (index < Length && this[index] != searchFor);

		return isValid;
	}

	public readonly bool PrevOccurrence(char searchFor, ref int index) {
		var isValid = index >= 0;

		do
			index--;
		while (index >= 0 && this[index] != searchFor);

		return isValid;
	}

	public readonly int CompareTo(ReadOnlySpan<char> compare) {
		var minLen = Math.Min(Length, compare.Length);
		for (var i = 0; i < minLen; i++) {
			var charComparison = this[i].CompareTo(compare[i]);
			if (charComparison != 0)
				return charComparison;
		}

		return Length.CompareTo(compare.Length);
	}

	public readonly bool Equals(ReadOnlySpan<char> compare) {
		if (Length != compare.Length) return false;
		for (var i = 0; i < Length; i++)
			if (this[i] != compare[i])
				return false;
		return true;
	}
}
