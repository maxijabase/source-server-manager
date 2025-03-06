using ReactiveUI;
using System;

namespace SourceServerManager.ViewModels;
public class ViewModelBase : ReactiveObject, IDisposable
{
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Free managed resources
            }

            // Free unmanaged resources

            _disposed = true;
        }
    }

    ~ViewModelBase()
    {
        Dispose(false);
    }
}
