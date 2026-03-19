// NavisBOQPlugin.cs — Entry Point
// Navisworks Manage 2025 · .NET Framework 4.8
// v4: patron exacto Gpc.ExportProps — [Plugin] simple, HTTP server en Execute()
// El server se inicia la primera vez que el usuario hace clic en el boton
// del ribbon. Permanece activo mientras Navisworks este abierto.

using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Autodesk.Navisworks.Api.Plugins;

namespace NavisBOQ.Plugin
{
    [Plugin(
        "NavisBOQ",
        "NAVBOQ",
        DisplayName = "NavisBOQ MCP",
        ToolTip     = "Inicia el servidor MCP para cuantificaciones con Claude Desktop (puerto 8765)")]
    public class NavisBOQPlugin : AddInPlugin
    {
        private const int PORT = 8765;

        private static readonly string PluginDir;

        static NavisBOQPlugin()
        {
            PluginDir = Path.GetDirectoryName(typeof(NavisBOQPlugin).Assembly.Location)
                        ?? AppDomain.CurrentDomain.BaseDirectory;

            Log("Static ctor ejecutado. PluginDir=" + PluginDir);

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                try
                {
                    var an        = new AssemblyName(args.Name);
                    var candidate = Path.Combine(PluginDir, an.Name + ".dll");
                    if (File.Exists(candidate))
                    {
                        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                            if (string.Equals(a.GetName().Name, an.Name,
                                              StringComparison.OrdinalIgnoreCase))
                                return a;
                        return Assembly.LoadFrom(candidate);
                    }
                }
                catch { }
                return null;
            };
        }

        private static HttpListener _listener;
        private static Thread       _listenerThread;
        private static bool         _running;
        private static readonly object _lock = new object();

        public override int Execute(params string[] parameters)
        {
            lock (_lock)
            {
                if (_running)
                {
                    MessageBox.Show(
                        "NavisBOQ MCP Server ya esta activo en puerto " + PORT + ".\n" +
                        "Claude Desktop puede conectarse via http://localhost:" + PORT,
                        "NavisBOQ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return 0;
                }

                try
                {
                    _listener = new HttpListener();
                    _listener.Prefixes.Add("http://localhost:" + PORT + "/");
                    _listener.Start();
                    _running = true;

                    _listenerThread = new Thread(ListenLoop)
                    {
                        IsBackground = true,
                        Name         = "NavisBOQ-HttpServer"
                    };
                    _listenerThread.Start();

                    Log("HTTP server iniciado en puerto " + PORT);

                    MessageBox.Show(
                        "NavisBOQ MCP Server iniciado en puerto " + PORT + ".\n" +
                        "Claude Desktop puede conectarse ahora.",
                        "NavisBOQ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Log("ERROR al iniciar HTTP server: " + ex.Message);
                    MessageBox.Show(
                        "Error al iniciar el servidor MCP:\n" + ex.Message,
                        "NavisBOQ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            return 0;
        }

        private static void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(
                        ctx => HandleRequest((HttpListenerContext)ctx), context);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log("ListenLoop ERROR: " + ex.Message);
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            var req  = context.Request;
            var resp = context.Response;

            try
            {
                string body;
                using (var sr = new StreamReader(req.InputStream, Encoding.UTF8))
                    body = sr.ReadToEnd();

                string result = CommandDispatcher.Dispatch(body);

                byte[] buffer = Encoding.UTF8.GetBytes(result);
                resp.ContentType     = "application/json; charset=utf-8";
                resp.ContentLength64 = buffer.Length;
                resp.StatusCode      = 200;
                resp.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                string error  = "{\"error\": \"" + ex.Message.Replace("\"", "'") + "\"}";
                byte[] buffer = Encoding.UTF8.GetBytes(error);
                resp.ContentType     = "application/json; charset=utf-8";
                resp.ContentLength64 = buffer.Length;
                resp.StatusCode      = 500;
                resp.OutputStream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                try { resp.OutputStream.Close(); } catch { }
            }
        }

        private static void Log(string msg)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(PluginDir, "NavisBOQ_load.log"),
                    "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch { }
        }
    }
}
