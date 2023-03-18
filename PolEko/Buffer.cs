using System;
using System.Collections.Generic;

namespace PolEko;

public class Buffer<T> where T : Measurement
{
  private BufferSize _size;
  private List<T> _buffer;
  
  public Buffer(uint size)
  {
    _size = new BufferSize(size);
    // _size.BufferOverflow += 
  }

  public void Add(T item)
  {
    _buffer.Add(item);
  }
}

// public class BufferSize
// {
//   private readonly uint _limit;
//   private uint _count;
//   public event EventHandler? BufferOverflow;
//   
//   public BufferSize(uint limit)
//   {
//     _limit = limit;
//   }
//   
//   public void Increment()
//   {
//     _count++;
//     if (_count < _limit) return;
//     BufferOverflow?.Invoke(this,EventArgs.Empty);
//     _count = 0;
//   }
//   public static BufferSize operator ++(BufferSize a)
//   {
//     a.Increment();
//     return a;
//   }
// }