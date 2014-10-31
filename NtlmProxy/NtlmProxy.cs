using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MikeRogers.NtlmProxy
{
    /// <summary>
    /// Class NtlmProxy. This class cannot be inherited.
    /// </summary>
    public sealed class NtlmProxy : IDisposable
    {
        #region Fields

        /// <summary>
        /// The hostname to which incoming requests will be proxied
        /// </summary>
        private readonly Uri _hostname;

        /// <summary>
        /// The simple HTTP server that accepts proxied requests
        /// </summary>
        private readonly SimpleHttpServer _server;

        /// <summary>
        /// The options for the server
        /// </summary>
        private readonly SimpleHttpServerOptions _options;

        #endregion


        #region Properties

        /// <summary>
        /// Gets the proxy server port.
        /// </summary>
        public int Port
        {
            get { return _server.Port; }
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
            _options = serverOptions ?? SimpleHttpServerOptions.DefaultOptions;
            _server = new SimpleHttpServer(ProcessRequest, serverOptions);
            _hostname = proxiedHostname;
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _server.Dispose();
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
                {_hostname, "NTLM", credential}
            };

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                Credentials = myCache
            };

            using (var client = new HttpClient(handler))
            {
                var httpMethod = new HttpMethod(context.Request.HttpMethod);
                var target = new Uri(_hostname, context.Request.Url.PathAndQuery);
                var content = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();

                // New implementation suggested by https://github.com/blabla4
                var request = new HttpRequestMessage(httpMethod, target);
                if (content != String.Empty)
                {
                    var contentType = context.Request.ContentType;

                    if (_options.HasAngularContentType && contentType != null)
                    {
                        // Thank you to https://github.com/svantreeck
                        contentType = Regex.Replace(contentType, ";charset=(.)*", string.Empty);
                    }

                    request.Content = new StringContent(content, context.Request.ContentEncoding, contentType);
                }

                // Add headers ('thank you' to https://github.com/svantreeck)
                _options.RequestHeaders.ToList().ForEach(x => request.Headers.Add(x.Key, x.Value));

                if (_options.AreHeadersDuplicated)
                {
                    foreach (var key in context.Request.Headers.AllKeys)
                    {
                        if (!_options.ExcludedHeaders.Contains(key))
                        {
                            request.Headers.TryAddWithoutValidation(key, context.Request.Headers[key]);
                        }
                    }
                }

                return await client.SendAsync(request);
            }
        }

        #endregion
    }
}