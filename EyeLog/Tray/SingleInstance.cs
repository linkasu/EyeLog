using System;
using System.Threading;

namespace EyeLog.Tray
{
    internal sealed class SingleInstance : IDisposable
    {
        private readonly Mutex mutex;
        public bool IsOwner { get; }

        private SingleInstance(Mutex mutex, bool isOwner)
        {
            this.mutex = mutex;
            IsOwner = isOwner;
        }

        public static SingleInstance Create(string name)
        {
            bool created;
            var mutex = new Mutex(true, name, out created);
            return new SingleInstance(mutex, created);
        }

        public void Dispose()
        {
            try
            {
                if (IsOwner)
                {
                    mutex.ReleaseMutex();
                }
            }
            catch
            {
            }
            finally
            {
                mutex.Dispose();
            }
        }
    }
}
