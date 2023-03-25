using System;
using System.Collections;
using System.Collections.Generic;

namespace PolEko;

public class Buffer<T> : IEnumerable<T> where T : Measurement
{
  private readonly Queue<T> _buffer = new();
  private BufferSize _size;
  private bool _overflownOnce;
  public event EventHandler? BufferOverflow;

  public Buffer(uint size)
  {
    _size = new BufferSize(size);
    _size.BufferOverflow += delegate
    {
      if (!_overflownOnce) _overflownOnce = true;
      BufferOverflow?.Invoke(this, EventArgs.Empty);
    };
  }
  
  public void Add(T item)
  {
    _buffer.Enqueue(item);
    if (_overflownOnce) _buffer.Dequeue();
    _size++;
  }
  
  public IEnumerable<T> GetCurrentIteration()
  {
    if (!_overflownOnce) return _buffer;
    var tempBuffer = new Queue<T>(_buffer);
    var amountToDequeue = _size.Limit - _size.Count;
    for (var i = 0; i < amountToDequeue; i++)
    {
      tempBuffer.Dequeue();
    }

    return tempBuffer;
  }

  public IEnumerator<T> GetEnumerator()
  {
    return _buffer.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  
  private class BufferSize
  {
    public readonly uint Limit;
    public uint Count;
    public event EventHandler? BufferOverflow;
  
    public BufferSize(uint limit)
    {
      Limit = limit;
    }
  
    private void Increment()
    {
      Count++;
      if (Count < Limit) return;
      BufferOverflow?.Invoke(this,EventArgs.Empty);
      Count = 0;
    }
    
    public static BufferSize operator ++(BufferSize a)
    {
      a.Increment();
      return a;
    }
  }
}
