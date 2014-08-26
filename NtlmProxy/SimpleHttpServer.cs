using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MikeRogers.NtlmProxy
{
    /// <summary>
    /// A simple HTTP server that can be set to a random unused port
    /// and expect a given type of authentication.
    /// </summary>
    public sealed class SimpleHttpServer :IDisposable
    {
        #region Fields

        /// <summary>
        /// The <see cref="HttpListener"/> instance for creating a small proxy server
        /// </summary>
        private readonly HttpListener listener;

        /// <summary>
        /// The function that handles incoming proxied requests
        /// </summary>
        private readonly Func<HttpListenerContext, Task<HttpResponseMessage>> requestHandler;

        #endregion


        #region Properties

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; private set; }

        #endregion Properties


        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleHttpServer"/> class.
        /// </summary>
        /// <param name="handler">The function that handles incoming proxied requests.</param>
        /// <param name="port">The port. If 0, a port is randomly chosen and assigned.</param>
        /// <param name="authScheme">A customizable authentication scheme for the server.</param>
        public SimpleHttpServer(
            Func<HttpListenerContext, Task<HttpResponseMessage>> handler,
            int port = 0,
            AuthenticationSchemes authScheme = AuthenticationSchemes.Anonymous)
        {
            Port = port == 0 ? GetEmptyPort() : port;
            requestHandler = handler;
            listener = new HttpListener
            {
                AuthenticationSchemes = authScheme,
                IgnoreWriteExceptions = true
            };

            listener.Prefixes.Add(string.Format("http://localhost:{0}/", Port.ToString(CultureInfo.InvariantCulture)));

            listener.Start();
            StartListenLoop();
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            listener.Stop();
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Gets the empty port.
        /// </summary>
        /// <returns>An unused port number</returns>
        private static int GetEmptyPort()
        {
            // from http://stackoverflow.com/a/3978040/996184
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// Starts the listen loop.
        /// </summary>
        private async void StartListenLoop()
        {
            while (true)
            {
                var context = await listener.GetContextAsync();
                var response = await requestHandler(context);

                using (var stream = context.Response.OutputStream)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    // set content type and content length
                    context.Response.ContentLength64 = bytes.Length;
                    context.Response.ContentType = response.Content.Headers.ContentType.ToString();

                    stream.Write(bytes, 0, bytes.Count());
                    stream.Close();
                }
            }
        }

        #endregion
 
    }
}
