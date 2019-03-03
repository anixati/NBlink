using System;

namespace Ko.NBlink
{
    internal abstract class DisposableBase : IDisposable
    {
        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                DisposeLocal();
            }
            _disposed = true;
        }

        protected virtual void DisposeLocal()
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class NBlinkException : Exception
    {
        public NBlinkException()
        {
        }

        public NBlinkException(string msg)
            : base(msg)
        {
        }
    }
}