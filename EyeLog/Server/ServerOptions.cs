namespace EyeLog.Server
{
    internal class ServerOptions
    {
        public int Port { get; set; } = 81203;
        public string Host { get; set; } = "localhost";
        public bool UseHttps { get; set; } = true;
        public int ExitTimeoutMs { get; set; } = 1000;
        public int GazeRateHz { get; set; } = 30;
        public int GazeStaleMs { get; set; } = 100;

        public string Prefix => (UseHttps ? "https" : "http") + "://" + Host + ":" + Port + "/";
    }
}
