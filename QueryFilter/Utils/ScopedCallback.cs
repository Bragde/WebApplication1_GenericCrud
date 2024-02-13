namespace QueryFilter.Utils;

//
// Summary:
//     Helper class that invokes a callback when disposed
internal sealed class ScopedCallback : IDisposable
{
    private readonly Action _action;

    public ScopedCallback(Action action)
    {
        _action = action;
    }

    public void Dispose()
    {
        _action();
    }
}
