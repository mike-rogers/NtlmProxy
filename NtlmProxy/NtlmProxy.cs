using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MikeRogers.NtlmProxy
{
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

        /// <summary>
        /// The options for the server
        /// </summary>
        private readonly SimpleHttpServerOptions options;

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
        /// <param name="serverOptions">Configuration options for the server.</param>
        public NtlmProxy(Uri proxiedHostname, SimpleHttpServerOptions serverOptions = null)
        {
            options = serverOptions ?? SimpleHttpServerOptions.DefaultOptions;
            server = new SimpleHttpServer(ProcessRequest, serverOptions);
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
                    var contentType = context.Request.ContentType;

                    if (options.AngularContentType && contentType != null)
                    {
                        // Thank you to https://github.com/svantreeck
                        contentType = Regex.Replace(contentType, ";charset=(.)*", string.Empty);
                    }

                    request.Content = new StringContent(content, context.Request.ContentEncoding, contentType);
                }

                // Add headers ('thank you' to https://github.com/svantreeck)
                options.RequestHeaders.ToList().ForEach(x => request.Headers.Add(x.Key, x.Value));

                if (options.DuplicateRequestHeaders)
                {
                    foreach (var key in context.Request.Headers.AllKeys)
                    {
                        if (!options.ForbiddenHeaders.Contains(key))
                        {
                            request.Headers.Add(key, context.Request.Headers[key]);
                        }
                    }
                }

                return await client.SendAsync(request);
            }
        }

        #endregion
    }
}