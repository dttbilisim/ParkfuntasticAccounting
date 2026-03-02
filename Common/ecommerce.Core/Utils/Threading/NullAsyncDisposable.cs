namespace ecommerce.Core.Utils.Threading;

public sealed class NullAsyncDisposable : IAsyncDisposable
{
    public static NullAsyncDisposable Instance { get; } = new NullAsyncDisposable();

    private NullAsyncDisposable()
    {

    }

    public ValueTask DisposeAsync()
    {
        return default;
    }
}
