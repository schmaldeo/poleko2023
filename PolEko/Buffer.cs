using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace PolEko;

/// <summary>
/// Buffer of <see cref="Measurement"/>s
/// </summary>
/// <typeparam name="T"><see cref="Measurement"/> type</typeparam>
public class Buffer<T> : IEnumerable<T>, INotifyCollectionChanged where T : Measurement
{
  #region Fields

  private readonly Queue<T> _buffer = new();
  private bool _overflownOnce;
  private BufferSize _size;

  #endregion

  #region Constructors

  public Buffer(uint size)
  {
    _size = new BufferSize(size);
    _size.BufferOverflow += OnBufferOverflow;
  }

  #endregion

  #region Fields
  
  public int Size => _buffer.Count;
  
  #endregion

  #region IEnumerable implementations
  public IEnumerator<T> GetEnumerator()
  {
    return _buffer.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  
  #endregion

  #region Events
  
  public event NotifyCollectionChangedEventHandler? CollectionChanged;

  private void OnCollectionChanged(NotifyCollectionChangedAction action, object? item)
  {
    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item));
  }

  public event EventHandler? BufferOverflow;

  private void OnBufferOverflow(object? sender, EventArgs eventArgs)
  {
    if (!_overflownOnce) _overflownOnce = true;
    BufferOverflow?.Invoke(this, EventArgs.Empty);
  }
  
  #endregion

  #region Methods
  
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
  
  #endregion

  private class BufferSize
  {
    #region Fields
    
    public readonly uint Limit;
    public uint Count;
    
    #endregion

    #region Constructors

    public BufferSize(uint limit)
    {
      Limit = limit;
    }
    
    #endregion

    #region Events
    
    public event EventHandler? BufferOverflow;
    
    #endregion

    #region Methods
    
    private void Increment()
    {
      Count++;
      if (Count < Limit) return;
      BufferOverflow?.Invoke(this, EventArgs.Empty);
      Count = 0;
    }
    
    #endregion

    #region Operators
    
    public static BufferSize operator ++(BufferSize a)
    {
      a.Increment();
      return a;
    }
    
    #endregion
  }
}