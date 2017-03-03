using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SoapServicesMocker
{
    /// <summary>
    /// Creates a mini web-server capable of handle Request-Response flows in localhost
    /// </summary>
    public class UnitaryWebServer : IDisposable
    {
        private bool _disposed;
        private readonly HttpListener _listener = new HttpListener();
        private readonly Action<HttpListenerRequest, HttpListenerResponse> _responderMethod;
        
        public UnitaryWebServer(string[] prefixes, Action<HttpListenerRequest, HttpListenerResponse> method)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");

            // URI prefixes are required, for example 
            // "http://localhost:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // A responder method is required
            if (method == null)
                throw new ArgumentException("method");

            foreach (string s in prefixes)
                _listener.Prefixes.Add(s);

            _responderMethod = method;
            _listener.Start();
        }

        public UnitaryWebServer(Action<HttpListenerRequest, HttpListenerResponse> method, params string[] prefixes)
            : this(prefixes, method)
        {
        }

        public UnitaryWebServer(Uri registeredUri, Action<HttpListenerRequest, HttpListenerResponse> method)
            : this(method, registeredUri.ToString())
        {
        }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Debug.Print("Webserver running...");
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                _responderMethod.Invoke(ctx.Request, ctx.Response);
                            }
                            catch { } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
            _disposed = true;
            Debug.Print("Webserver stopped...");
        }

        public void Dispose()
        {
            if(!_disposed)
                this.Stop();
        }

        private static int FreeTcpPort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public static Uri ResolveLocalRandomPrefix(string absolutePath)
        {
            var urlTemplate = string.Format("http://localhost:{0}/{1}/", FreeTcpPort(), absolutePath.Trim('/'));
            return new Uri(urlTemplate, UriKind.Absolute);
        }
    }
}