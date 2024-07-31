using System;
using System.Collections.Generic;

public interface IPoolable : IDisposable
{
    public void OnCreate();
    public void OnRetrievedFromPool();
    public void OnReturnedToPool();
}

public class ConfigureableObjectPool<T> where T : IPoolable, new()
{
    public int MaximumSize;
    private readonly Stack<T> _availableObjects = new Stack<T>();
    private readonly Dictionary<int, T> _inUse = new Dictionary<int, T>();
    private int _nextId = 1;

    // Singleton instance variable
    private static readonly Lazy<ConfigureableObjectPool<T>> _instance =
        new Lazy<ConfigureableObjectPool<T>>(() => new ConfigureableObjectPool<T>(100000));

    // Private constructor to prevent direct instantiation
    private ConfigureableObjectPool(int maximumSize)
    {
        if (maximumSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSize), "Maximum pool size must be greater than zero.");
        }

        MaximumSize = maximumSize;
    }

    // Static property to access the singleton instance
    public static ConfigureableObjectPool<T> Instance => _instance.Value;

    // Method to get an object from the pool
    public T GetObject()
    {
        lock (_availableObjects)
        {
            if (_availableObjects.Count > 0)
            {
                T item = _availableObjects.Pop();
                item.OnRetrievedFromPool();
                return item;
            }
            else
            {
                // Create a new object if the pool is empty
                T newItem = new T();
                newItem.OnCreate();
                return newItem;
            }
        }
    }

    // Method to return an object to the pool
    public void ReturnObject(T item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        lock (_availableObjects)
        {
            if (_availableObjects.Count < MaximumSize)
            {
                item.OnReturnedToPool();
                _availableObjects.Push(item);
            }
            else
            {
                item.Dispose();
            }
        }
    }

    // Method to grow the pool by a specified number of objects
    public void GrowPool(int numberOfObjects)
    {
        if (numberOfObjects <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numberOfObjects),
                "Number of objects must be greater than zero.");
        }

        lock (_availableObjects)
        {
            for (int i = 0; i < numberOfObjects; i++)
            {
                if (_availableObjects.Count < MaximumSize)
                {
                    T newItem = new T();
                    newItem.OnCreate();
                    _availableObjects.Push(newItem);
                }
                else
                {
                    break; // Stop if the pool has reached its maximum size
                }
            }
        }
    }

    // Method to set the maximum pool size
    public void SetMaxPoolSize(int maxPoolSize)
    {
        if (maxPoolSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPoolSize), "Maximum pool size must be greater than zero.");
        }

        lock (_availableObjects)
        {
            MaximumSize = maxPoolSize;

            // Dispose of excess objects if the new max size is smaller than the current pool size
            while (_availableObjects.Count > MaximumSize)
            {
                T item = _availableObjects.Pop();
                item.Dispose();
            }
        }
    }
}

public class ManagedObjectPool<T> where T : new()
{
    private readonly Stack<T> _availableObjects = new Stack<T>();
    private readonly HashSet<T> _inUse = new HashSet<T>();

    // Singleton instance variable
    private static readonly Lazy<ManagedObjectPool<T>> _instance =
        new Lazy<ManagedObjectPool<T>>(() => new ManagedObjectPool<T>());

    // Private constructor to prevent direct instantiation
    private ManagedObjectPool()
    {
    }

    // Static property to access the singleton instance
    public static ManagedObjectPool<T> Instance
    {
        get { return _instance.Value; }
    }

    // Method to get an object from the pool
    public T GetObject()
    {
        lock (_availableObjects)
        {
            if (_availableObjects.Count > 0)
            {
                T item = _availableObjects.Pop();
                _inUse.Add(item);
                return item;
            }
            else
            {
                // Create a new object if the pool is empty
                T newItem = new T();
                _inUse.Add(newItem);
                return newItem;
            }
        }
    }

    // Method to return an object to the pool
    public void ReturnObject(T item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        lock (_availableObjects)
        {
            if (_inUse.Contains(item))
            {
                _inUse.Remove(item);
                _availableObjects.Push(item);
            }
            else
            {
                throw new InvalidOperationException("The item being returned was not created by this pool.");
            }
        }
    }
}