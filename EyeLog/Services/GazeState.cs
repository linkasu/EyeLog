using System;
using EyeLog.State;

namespace EyeLog.Services
{
    internal class GazeState
    {
        private readonly object sync = new object();
        private GazeSample latest;
        private bool hasGaze;

        public void Update(double x, double y, long deviceTimestamp)
        {
            lock (sync)
            {
                latest = new GazeSample
                {
                    X = x,
                    Y = y,
                    DeviceTimestamp = deviceTimestamp,
                    ReceivedAtUtc = DateTime.UtcNow
                };
                hasGaze = true;
            }
        }

        public bool TryGetLatest(out GazeSample gaze)
        {
            lock (sync)
            {
                gaze = latest;
                return hasGaze;
            }
        }
    }
}
