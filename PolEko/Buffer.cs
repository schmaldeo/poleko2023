using System;
using System.Collections;
using System.Collections.Generic;

namespace PolEko;

// TODO: buffer needs to be a Queue<T>, not a List<T>. Need to keep filling the Queue until it reaches the buffer limit,
// then start dequeuing and enqueueing new measurements 
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
    if (!_overflownOnce) return;
    _buffer.Dequeue();
    
    _size++;
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
    private readonly uint _limit;
    private uint _count;
    public event EventHandler? BufferOverflow;
  
    public BufferSize(uint limit)
    {
      _limit = limit;
    }
  
    private void Increment()
    {
      _count++;
      if (_count < _limit) return;
      BufferOverflow?.Invoke(this,EventArgs.Empty);
      _count = 0;
    }
    
    public static BufferSize operator ++(BufferSize a)
    {
      a.Increment();
      return a;
    }
  }
}
