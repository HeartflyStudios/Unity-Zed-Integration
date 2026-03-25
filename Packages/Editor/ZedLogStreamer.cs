using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

namespace HFS.ZedEditor
{
    [InitializeOnLoad]
    public static class ZedLogStreamer
    {
        private const int Port = 12345;
        private static TcpListener _listener;
        private static readonly ConcurrentQueue<string> _logQueue = new();
        private static readonly CancellationTokenSource _cts = new();
        private static readonly Regex StackTraceRegex = new(@"\(at (.+):(\d+)\)", RegexOptions.Compiled);

        static ZedLogStreamer()
        {
            // Clean up sockets on script reload or editor exit
            AppDomain.CurrentDomain.DomainUnload += (_, _) => Cleanup();
            EditorApplication.quitting += Cleanup;

            Application.logMessageReceivedThreaded += HandleLog;

            Thread serverThread = new Thread(StartServer)
            {
                IsBackground = true,
                Name = "ZedProxy_IO"
            };
            serverThread.Start();
        }

        private static void StartServer()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
                // Critical: Allow immediate rebinding after Unity reloads scripts
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start();

                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        using (TcpClient client = _listener.AcceptTcpClient())
                        using (NetworkStream stream = client.GetStream())
                        using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true })
                        {
                            while (client.Connected && !_cts.Token.IsCancellationRequested)
                            {
                                if (_logQueue.TryDequeue(out string log))
                                {
                                    writer.WriteLine(log);
                                }
                                Thread.Sleep(10);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Client disconnected - this is expected when Zed closes/restarts
                        Debug.Log($"[Zed] Client disconnected: {ex.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                if (!_cts.Token.IsCancellationRequested)
                {
                    Debug.LogError($"[Zed] Server Error: {e.GetType().Name} - {e.Message}\n{e.StackTrace}");
                }
            }
        }

        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            var entry = new
            {
                time = DateTime.Now.ToString("HH:mm:ss"),
                type = type.ToString(),
                message = condition,
                stack = stackTrace,
                file = ExtractFilePath(stackTrace),
                line = ExtractLineNumber(stackTrace)
            };

            _logQueue.Enqueue(JsonConvert.SerializeObject(entry));
        }

        private static string ExtractFilePath(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return string.Empty;
            var match = StackTraceRegex.Match(stackTrace);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static int ExtractLineNumber(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return 0;
            var match = StackTraceRegex.Match(stackTrace);
            return match.Success && int.TryParse(match.Groups[2].Value, out int line) ? line : 0;
        }

        private static void Cleanup()
        {
            _cts.Cancel();
            _listener?.Stop();
            Application.logMessageReceivedThreaded -= HandleLog;
        }
    }
}
