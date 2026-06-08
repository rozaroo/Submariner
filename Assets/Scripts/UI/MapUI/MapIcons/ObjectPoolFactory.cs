using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolFactory<TKey, TObject> where TObject : class
{
    private readonly Dictionary<TKey, ObjectPool<TObject>> _pools = new();
    private readonly Func<TKey, ObjectPool<TObject>> _poolFactory;

    public ObjectPoolFactory(Func<TKey, ObjectPool<TObject>> poolFactory)
    {
        _poolFactory = poolFactory;
    }

    public TObject Get(TKey key)
        => GetOrCreate(key).Get();

    public void Release(TKey key, TObject obj)
    {
        if (_pools.TryGetValue(key, out var pool))
            pool.Release(obj);
    }

    private ObjectPool<TObject> GetOrCreate(TKey key)
    {
        if (!_pools.ContainsKey(key))
            _pools[key] = _poolFactory(key);
        return _pools[key];
    }
}