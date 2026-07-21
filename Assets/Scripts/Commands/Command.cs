using System;

public sealed class Command
{
    private Action _handler;
    private UnityEngine.Object _owner;
    private readonly string _name;

    public Command([System.Runtime.CompilerServices.CallerMemberName] string name = null)
    {
        _name = name ?? nameof(Command);
    }

    public void Bind(Action handler, UnityEngine.Object owner = null)
    {
        _handler = handler;
        _owner = owner ?? (handler?.Target as UnityEngine.Object);
    }

    public void Unbind(Action handler)
    {
        if (_handler != handler) return;

        _handler = null;
        _owner = null;
    }

    public void Invoke()
    {
        if (!TryGetHandler(out var handler))
            return;

        handler();
    }

    public bool IsBound => _handler != null && IsOwnerAlive();

    private bool TryGetHandler(out Action handler)
    {
        handler = _handler;

        if (handler == null)
        {
            Logger.Log($"[Command:{_name}] Invoked while unbound.");
            return false;
        }

        if (!IsOwnerAlive())
        {
            Logger.Log($"[Command:{_name}] Owner destroyed — auto-unbinding.");
            _handler = null;
            _owner = null;
            return false;
        }

        return true;
    }

    private bool IsOwnerAlive()
    {
        if (_owner == null && _handler != null)
            return true;

        return _owner != null;
    }
}

public sealed class Command<T>
{
    private Action<T> _handler;
    private UnityEngine.Object _owner;
    private readonly string _name;

    public Command([System.Runtime.CompilerServices.CallerMemberName] string name = null)
    {
        _name = name ?? nameof(Command);
    }

    public void Bind(Action<T> handler, UnityEngine.Object owner = null)
    {
        _handler = handler;
        _owner = owner ?? (handler?.Target as UnityEngine.Object);
    }

    public void Unbind(Action<T> handler)
    {
        if (_handler != handler) return;

        _handler = null;
        _owner = null;
    }

    public void Invoke(T arg)
    {
        if (!TryGetHandler(out var handler))
            return;

        handler(arg);
    }

    public bool IsBound => _handler != null && IsOwnerAlive();

    private bool TryGetHandler(out Action<T> handler)
    {
        handler = _handler;

        if (handler == null)
        {
            Logger.Log($"[Command:{_name}] Invoked while unbound.");
            return false;
        }

        if (!IsOwnerAlive())
        {
            Logger.Log($"[Command:{_name}] Owner destroyed — auto-unbinding.");
            _handler = null;
            _owner = null;
            return false;
        }

        return true;
    }

    private bool IsOwnerAlive()
    {
        if (_owner == null && _handler != null)
            return true;

        return _owner != null;
    }
}

public sealed class Query<TResult>
{
    private Func<TResult> _handler;
    private UnityEngine.Object _owner;
    private readonly string _name;

    public Query([System.Runtime.CompilerServices.CallerMemberName] string name = null)
    {
        _name = name ?? nameof(Query<TResult>);
    }

    public void Bind(Func<TResult> handler, UnityEngine.Object owner = null)
    {
        _handler = handler;
        _owner = owner ?? (handler?.Target as UnityEngine.Object);
    }

    public void Unbind(Func<TResult> handler)
    {
        if (_handler != handler) return;

        _handler = null;
        _owner = null;
    }

    public TResult Invoke()
    {
        if (!TryGetHandler(out var handler))
            return default;

        return handler();
    }

    public bool IsBound => _handler != null && IsOwnerAlive();

    private bool TryGetHandler(out Func<TResult> handler)
    {
        handler = _handler;

        if (handler == null)
        {
            Logger.Log($"[Query:{_name}] Invoked while unbound.");
            return false;
        }

        if (!IsOwnerAlive())
        {
            Logger.Log($"[Query:{_name}] Owner destroyed — auto-unbinding.");
            _handler = null;
            _owner = null;
            return false;
        }

        return true;
    }

    private bool IsOwnerAlive()
    {
        if (_owner == null && _handler != null)
            return true;

        return _owner != null;
    }
}

public sealed class Query<TArg, TResult>
{
    private Func<TArg, TResult> _handler;
    private UnityEngine.Object _owner;
    private readonly string _name;

    public Query([System.Runtime.CompilerServices.CallerMemberName] string name = null)
    {
        _name = name ?? nameof(Query<TArg, TResult>);
    }

    public void Bind(Func<TArg, TResult> handler, UnityEngine.Object owner = null)
    {
        _handler = handler;
        _owner = owner ?? (handler?.Target as UnityEngine.Object);
    }

    public void Unbind(Func<TArg, TResult> handler)
    {
        if (_handler != handler) return;

        _handler = null;
        _owner = null;
    }

    public TResult Invoke(TArg arg)
    {
        if (!TryGetHandler(out var handler))
            return default;

        return handler(arg);
    }

    public bool IsBound => _handler != null && IsOwnerAlive();

    private bool TryGetHandler(out Func<TArg, TResult> handler)
    {
        handler = _handler;

        if (handler == null)
        {
            Logger.Log($"[Query:{_name}] Invoked while unbound.");
            return false;
        }

        if (!IsOwnerAlive())
        {
            Logger.Log($"[Query:{_name}] Owner destroyed — auto-unbinding.");
            _handler = null;
            _owner = null;
            return false;
        }

        return true;
    }

    private bool IsOwnerAlive()
    {
        if (_owner == null && _handler != null)
            return true;

        return _owner != null;
    }
}