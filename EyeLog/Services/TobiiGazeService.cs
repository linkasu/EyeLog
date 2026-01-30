using System;
using Tobii.Interaction;
using Tobii.Interaction.Framework;

namespace EyeLog.Services
{
    internal class TobiiGazeService : IDisposable
    {
        private Host host;

        public void Start(Action<double, double, long> onGaze)
        {
            host = new Host();
            host.EnableConnection();
            host.Streams.CreateGazePointDataStream().Next += (sender, e) =>
            {
                onGaze?.Invoke(e.Data.X, e.Data.Y, e.Data.Timestamp);
            };
        }

        public void Dispose()
        {
            try { host?.Dispose(); } catch { }
        }
    }
}
