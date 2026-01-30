namespace EyeLog.Server
{
    internal static class HtmlTemplates
    {
        public static string LogsPage => "<!doctype html>\n" +
            "<html>\n" +
            "<head>\n" +
            "  <meta charset=\"utf-8\">\n" +
            "  <title>EyeLog Logs</title>\n" +
            "  <style>\n" +
            "    body { font-family: Arial, sans-serif; background: #0f1115; color: #e6e6e6; margin: 0; padding: 16px; }\n" +
            "    #log { white-space: pre-wrap; font-size: 13px; }\n" +
            "  </style>\n" +
            "</head>\n" +
            "<body>\n" +
            "  <h1>EyeLog Logs</h1>\n" +
            "  <div id=\"log\"></div>\n" +
            "  <script>\n" +
            "    const logEl = document.getElementById('log');\n" +
            "    const proto = location.protocol === 'https:' ? 'wss:' : 'ws:';\n" +
            "    const ws = new WebSocket(proto + '//' + location.host + '/logs');\n" +
            "    ws.onmessage = (e) => {\n" +
            "      logEl.textContent += e.data;\n" +
            "      if (!e.data.endsWith('\\n')) logEl.textContent += '\\n';\n" +
            "      logEl.scrollTop = logEl.scrollHeight;\n" +
            "    };\n" +
            "  </script>\n" +
            "</body>\n" +
            "</html>";
    }
}
