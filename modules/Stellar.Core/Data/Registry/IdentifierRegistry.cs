// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

using Stellar.Kernel;
using Stellar.Kernel.Data.Registry;

namespace Stellar.Core.Data.Registry;

public sealed class IdentifierRegistry
    : IRegistry<Identifier>
{
    private readonly Dictionary<Guid, Identifier> _identifiers = new();
    private readonly ReaderWriterLockSlim _lock = new();

    private static readonly Lazy<IdentifierRegistry> _instance = new(() => new IdentifierRegistry());
    public static IdentifierRegistry Instance => _instance.Value;

    private IdentifierRegistry()
    {
    }

    public bool Exists(IIdentifier id)
    {
        if (id == null) return false;
        _lock.EnterReadLock();
        try
        {
            return _identifiers.ContainsKey(id.UID);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool Register(Identifier obj)
    {
        if (obj == null) return false;
        _lock.EnterWriteLock();
        try
        {
            return _identifiers.TryAdd(obj.UID, obj);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Identifier? Get(Guid uid)
    {
        _lock.EnterReadLock();
        try
        {
            return _identifiers.GetValueOrDefault(uid);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Identifier? Get(IIdentifier id)
    {
        return id == null ? null : Get(id.UID);
    }

    public Identifier? Pop(Guid uid)
    {
        _lock.EnterWriteLock();
        try
        {
            return _identifiers.Remove(uid, out var obj) ? obj : null;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Identifier? Pop(IIdentifier id)
    {
        return id == null ? null : Pop(id.UID);
    }

    public int Size
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _identifiers.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public ICollection<IIdentifier> Keys
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _identifiers.Values.Cast<IIdentifier>().ToArray();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public ICollection<Identifier> Values
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _identifiers.Values.ToArray();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}