using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace PolEko;

public class Buffer<T> : IEnumerable<T>, INotifyCollectionChanged where T : Measurement
{
  private readonly Queue<T> _buffer = new();
  private bool _overflownOnce;
  private BufferSize _size;

  public Buffer(uint size)
  {
    _size = new BufferSize(size);
    _size.BufferOverflow += delegate
    {
      if (!_overflownOnce) _overflownOnce = true;
      BufferOverflow?.Invoke(this, EventArgs.Empty);
    };
  }

  public int Size => _buffer.Count;

  private void OnCollectionChanged(NotifyCollectionChangedAction action, object? item)
  {
    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item));
  }

  public IEnumerator<T> GetEnumerator()
  {
    return _buffer.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  public event EventHandler? BufferOverflow;
  
  public event NotifyCollectionChangedEventHandler? CollectionChanged;

  public void Add(T item)
  {
    _buffer.Enqueue(item);
    if (_overflownOnce)
    {
      _buffer.Dequeue();
      OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
    }
    _size++;
    OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
  }

  public IEnumerable<T> GetCurrentIteration()
  {
    if (!_overflownOnce) return _buffer;
    var tempBuffer = new Queue<T>(_buffer);
    var amountToDequeue = _size.Limit - _size.Count;
    for (var i = 0; i < amountToDequeue; i++) tempBuffer.Dequeue();

    return tempBuffer;
  }

  private class BufferSize
  {
    public readonly uint Limit;
    public uint Count;

    public BufferSize(uint limit)
    {
      Limit = limit;
    }

    public event EventHandler? BufferOverflow;

    private void Increment()
    {
      Count++;
      if (Count < Limit) return;
      BufferOverflow?.Invoke(this, EventArgs.Empty);
      Count = 0;
    }

    public static BufferSize operator ++(BufferSize a)
    {
      a.Increment();
      return a;
    }
  }
}