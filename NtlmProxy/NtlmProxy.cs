using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MikeRogers.NtlmProxy
{
    /// <summary>
    /// Class NtlmProxy.
    /// </summary>
    public sealed class NtlmProxy : IDisposable
    {
        #region Fields

        /// <summary>
        /// The hostname to which incoming requests will be proxied
        /// </summary>
        private readonly Uri hostname;

        /// <summary>
        /// The simple HTTP server that accepts proxied requests
        /// </summary>
        private readonly SimpleHttpServer server;

        #endregion


        #region Properties

        /// <summary>
        /// Gets the proxy server port.
        /// </summary>
        public int Port
        {
            get { return server.Port; }
        }

        #endregion


        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NtlmProxy"/> class.
        /// </summary>
        /// <param name="proxiedHostname">The proxied hostname.</param>
        /// <param name="port">The port. If 0, a port is randomly chosen and assigned.</param>
        public NtlmProxy(Uri proxiedHostname, int port = 0)
        {
            server = new SimpleHttpServer(ProcessRequest, port);
            hostname = proxiedHostname;
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            server.Dispose();
        }

        #endregion


        #region Private Methods

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
                var httpMethod = new HttpMethod(context.Request.HttpMethod);
                var target = new Uri(hostname, context.Request.Url.PathAndQuery);
                var content = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();

                // New implementation suggested by https://github.com/blabla4
                var request = new HttpRequestMessage(httpMethod, target);
                if (content != String.Empty)
                {
                    request.Content = new StringContent(content, context.Request.ContentEncoding, context.Request.ContentType);
                }

                return await client.SendAsync(request);
            }
        }

        #endregion
    }
}