using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace FancyMapSnapper.DataStructures;

public struct UnsophisticatedList<T> {
	private readonly T[] _array;
	private int _count;
	private readonly int _capacity;
	private readonly ArrayPool<T>? _poolRef;

	public UnsophisticatedList(int capacity) {
		_array = new T[capacity];
		_count = 0;
		_capacity = capacity;
		_poolRef = null;
	}

	public UnsophisticatedList(int capacity, ArrayPool<T> pool) {
		_array = pool.Rent(capacity);
		Array.Clear(_array);

		_count = 0;
		_capacity = capacity;
		_poolRef = pool;
	}

	public bool IsRented => _poolRef != null;

	public bool IsFull => Count >= Capacity;

	public bool IsValid { get; private set; } = true;

	public ref T GetPinnableReference() => ref _array[0];

	private readonly void CheckValid() {
		if (!IsValid)
			throw new InvalidOperationException("Cannot use invalid UnsophisticatedList");
	}

	public int Count {
		get {
			CheckValid();
			return _count;
		}
	}

	public int Capacity {
		get {
			CheckValid();
			return _capacity;
		}
	}

	public void Push(in T item) {
		CheckValid();
		if (_count >= _capacity)
			throw new InvalidOperationException("Cannot add item to full UnsophisticatedList");

		_array[_count++] = item;
	}

	public readonly bool Peek([MaybeNullWhen(false)] out T item) {
		CheckValid();
		if (_count <= 0) {
			item = default;
			return false;
		}

		item = _array[_count - 1];
		return true;
	}

	public bool Pop([MaybeNullWhen(false)] out T item) {
		CheckValid();
		if (_count <= 0) {
			item = default;
			return false;
		}

		item = _array[_count--];
		return true;
	}

	public T this[int index] {
		readonly get {
			CheckValid();
			if (index < 0 || index >= _count)
				throw new IndexOutOfRangeException($"Index was outside the bounds of the UnsophisticatedList; must be within [0, {_count}), got {index}");
			return _array[index];
		}
		set {
			CheckValid();
			if (index < 0 || index > _count)
				throw new IndexOutOfRangeException($"Index was outside the bounds of the UnsophisticatedList; must be within [0, {_count}], got {index}");
			if (index >= _capacity)
				throw new InvalidOperationException("Cannot add item to full UnsophisticatedList");
			_array[index] = value;
		}
	}

	public void Clear() {
		CheckValid();
		_count = 0;
	}

	public void Free() {
		if (_poolRef == null) return;
		_poolRef.Return(_array);
		IsValid = false;
	}
}
