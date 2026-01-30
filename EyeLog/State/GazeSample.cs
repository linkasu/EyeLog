using System;

namespace EyeLog.State
{
    internal struct GazeSample
    {
        public double X;
        public double Y;
        public long DeviceTimestamp;
        public DateTime ReceivedAtUtc;
    }
}
