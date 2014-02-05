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
    /// Class NtlmProxy.
    /// </summary>
    public class NtlmProxy : IDisposable
    {
        #region Fields

        /// <summary>
        /// The <see cref="HttpListener"/> instance for creating a small proxy server
        /// </summary>
        private readonly HttpListener listener;

        /// <summary>
        /// The hostname to which incoming requests will be proxied
        /// </summary>
        private readonly Uri hostname;

        #endregion


        #region Properties

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; set; }

        #endregion Properties


        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NtlmProxy"/> class.
        /// </summary>
        /// <param name="proxiedHostname">The proxied hostname.</param>
        /// <param name="port">The port. If 0, a port is randomly chosen and assigned.</param>
        public NtlmProxy(Uri proxiedHostname, int port = 0)
        {
            Port = port == 0 ? GetEmptyPort() : port;
            hostname = proxiedHostname;
            listener = new HttpListener();

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
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
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
                var response = await ProcessRequest(context);

                using (var stream = context.Response.OutputStream)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    stream.Write(bytes, 0, bytes.Count());
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>A task thread that resolves into an HTTP response.</returns>
        private async Task<HttpResponseMessage> ProcessRequest(HttpListenerContext context)
        {
            var credential = CredentialCache.DefaultNetworkCredentials;
            var myCache = new CredentialCache
            {
                {hostname, "NTLM", credential}
            };

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                Credentials = myCache
            };

            using (var client = new HttpClient(handler))
            {
                var target = new Uri(hostname, context.Request.Url.PathAndQuery);
                var responseMessage = await client.GetAsync(target);
                return responseMessage;
            }
        }

        #endregion
    }
}