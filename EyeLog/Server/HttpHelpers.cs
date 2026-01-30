using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;

namespace EyeLog.Server
{
    internal static class HttpHelpers
    {
        public static void WriteCors(HttpListenerResponse response)
        {
            response.Headers["Access-Control-Allow-Origin"] = "*";
            response.Headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS";
            response.Headers["Access-Control-Allow-Headers"] = "content-type";
        }

        public static void WriteJson(HttpListenerResponse response, object payload)
        {
            var serializer = new DataContractJsonSerializer(payload.GetType());
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, payload);
                var bytes = ms.ToArray();
                WriteCors(response);
                response.ContentType = "application/json";
                response.ContentLength64 = bytes.Length;
                using (var stream = response.OutputStream)
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
        }

        public static void WriteHtml(HttpListenerResponse response, string html)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            WriteCors(response);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            using (var stream = response.OutputStream)
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public static T ReadJson<T>(HttpListenerRequest request) where T : class
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                var json = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }
                var serializer = new DataContractJsonSerializer(typeof(T));
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return serializer.ReadObject(ms) as T;
                }
            }
        }
    }
}
