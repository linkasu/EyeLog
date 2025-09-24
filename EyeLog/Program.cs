using Tobii.Interaction;
using System.IO;
using System ;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tobii.Interaction.Framework;
using System.Runtime.CompilerServices;

namespace EyeLog
{
    internal class Program
    {
        private static Host host;
        private static Bound[] bounds = new Bound[0];
        private static StreamData<GazePointData> CurrentEyeValue;
        private static long lastProcessedTS;
        private static long ts;
        private static long lastExitStartTS=-1;
        private static long lastEnterTS;
        private static int lastSelected;
        private static long CLICK_TIMEOUT = 1000;
        private static long lastClicksCount;
        private static long EXIT_TIMEOUT = 1000;

        static bool rawMode = false;
        static StreamWriter logWriter = null;


        static void Main(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg == "--raw")
                {
                    rawMode = true;
                    Console.WriteLine("Running in RAW mode...");
                }
                else if (arg.StartsWith("--out="))
                {
                    var path = arg.Substring("--out=".Length);
                    try
                    {
                        logWriter = new StreamWriter(path, append: true);
                        logWriter.AutoFlush = true;
                        Console.WriteLine($"Logging to file: {path}");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Failed to open log file: " + ex.Message);
                    }
                }
            }

            host = new Host();
            host.EnableConnection();
            host.Streams.CreateGazePointDataStream().Next += OnGaze;

            new Thread(new ThreadStart(InputCycle)).Start();
            new Thread(new ThreadStart(EyeCycle)).Start();
        }

        private static void EyeCycle()
        {
            
            while (true)
            {
                try
                {
                    if (CurrentEyeValue == null) { continue; }

                    var now = DateTime.Now.ToFileTime();

                    var diff = (now - ts) / 10000;
                    if (diff > EXIT_TIMEOUT)
                    {
                        Console.WriteLine("exit");
                        CurrentEyeValue = null;
                        continue;
                    }

                    if (lastProcessedTS == ts)
                    {
                        continue;
                    }

                    int nowInside = -1;
                    for (int i = 0; i < bounds.Length; i++)
                    {
                        bool isInside = bounds[i].isInside(((int)CurrentEyeValue.Data.X), ((int)CurrentEyeValue.Data.Y));
                        if (isInside)
                        {
                            nowInside = i;
                        }
                    }


                    var isEnter = lastSelected == -1 && nowInside != -1 && lastSelected != nowInside;
                    var isExit = lastSelected != nowInside;
                    var isStay = lastSelected != -1 && lastSelected == nowInside;

                    if (isEnter)
                    {
                        OnEnter(nowInside, ts);
                    }
                    if (isExit)
                    {
                        OnExit(now);
                    }
                    if (isStay)
                    {
                        lastExitStartTS = -1;
                        OnStay(ts);
                    }
                    lastProcessedTS = ts;
                } catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.ToString());
                }
            }
        }

        private static void OnStay(long ts)
        {
            long insideTime = (ts - lastEnterTS) / 10000;
            long clicks = insideTime / CLICK_TIMEOUT;
            if (lastClicksCount != clicks)
            {
                lastClicksCount = clicks;
                if (clicks > 0) Console.WriteLine("click:" + lastSelected + "," + clicks);
            }
        }

        private static void OnExit(long ts)
        {
            if (lastExitStartTS == -1)
            {
                lastExitStartTS = ts;
            }
            else if(ts - lastExitStartTS > 50)  
            {
                lastSelected = -1;
                lastExitStartTS = -1;
                Console.WriteLine("exit");
            }
        }

        private static void OnEnter(int nowInside, long ts)
        {
            lastEnterTS = ts;
            lastSelected = nowInside;
            Console.WriteLine("enter:" + lastSelected);
        }

        private static void InputCycle()
        {

            while (true)
            {
                try
                {
                    var line = Console.ReadLine();
                    if (line.StartsWith("timeout:"))
                    {
                        CLICK_TIMEOUT = int.Parse(line.Substring("timeout:".Length));

                        continue;
                    }
                    var robjs = line.Split(';');
                    bounds = new Bound[robjs.Length];
                    lastSelected = -1;
                    for (int i = 0; i < robjs.Length; i++)
                    {
                        string robj = robjs[i];
                        var rcoords = robj.Split(',');
                        var x = int.Parse(rcoords[0]);
                        var y = int.Parse(rcoords[1]);
                        var width = int.Parse(rcoords[2]);
                        var height = int.Parse(rcoords[3]);
                        bounds[i] = new Bound(x, y, width, height);
                    }
                }
                catch
                {
                    bounds = new Bound[0];
                }
            }
        }


        private static void OnGaze(object sender, StreamData<GazePointData> e)
        {
            CurrentEyeValue = e;
            ts = DateTime.Now.ToFileTime();

            string line;
            if (rawMode)
                line = $"{e.Data.X},{e.Data.Y},{e.Data.Timestamp}";
            else
                line = $"{e.Data.X}:{e.Data.Y}";

            if (logWriter != null)
            {
                logWriter.WriteLine(line);
            }
        }


    }

}
