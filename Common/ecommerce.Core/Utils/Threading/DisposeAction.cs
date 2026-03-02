namespace ecommerce.Core.Utils.Threading;

/// <inheritdoc />
public class DisposeAction : DisposeAction<object?>
{
    public DisposeAction(Action action) : base(_ => action(), null)
    {
    }
}

/// <summary>
/// This class can be used to provide an action when
/// Dipose method is called.
/// </summary>
public class DisposeAction<T> : IDisposable
{
    private readonly Action<T> _action;

    private readonly T _parameter;

    /// <summary>
    /// Creates a new <see cref="DisposeAction"/> object.
    /// </summary>
    /// <param name="action">Action to be executed when this object is disposed.</param>
    /// <param name="parameter">The parameter of the action.</param>
    public DisposeAction(Action<T> action, T parameter)
    {
        _action = action;
        _parameter = parameter;
    }

    public void Dispose()
    {
        _action(_parameter);
    }
}