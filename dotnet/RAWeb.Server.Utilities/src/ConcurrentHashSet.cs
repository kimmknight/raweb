using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RAWeb.Server.Utilities;

public sealed class ConcurrentHashSet<T> : IEnumerable<T>, IEnumerable {
  private readonly ConcurrentDictionary<T, byte> _dict;

  public ConcurrentHashSet() {
    _dict = new ConcurrentDictionary<T, byte>();
  }

  public bool TryAdd(T item) => _dict.TryAdd(item, 0);

  public bool TryRemove(T item) => _dict.TryRemove(item, out _);

  public bool Contains(T item) => _dict.ContainsKey(item);

  public IEnumerator<T> GetEnumerator() => _dict.Keys.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public int Count => _dict.Count;
}
