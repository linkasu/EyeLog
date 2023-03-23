using Tobii.Interaction;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeLog
{
    internal class Program
    {
        private static Host host;

        static void Main(string[] args)
        {
            host = new Host();
            host.Streams.CreateGazePointDataStream()
                .Next += OnGazePoint;
            Console.ReadKey();
        }

        private static void OnGazePoint(object sender, StreamData<GazePointData> e)
        {
            Console.WriteLine(Math.Round( e.Data.X) + "," + Math.Round(e.Data.Y) + ","+ Math.Round (e.Data.Timestamp));

        }
    }
}
