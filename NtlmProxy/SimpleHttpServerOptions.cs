using System.Collections.Generic;
using System.Net;

namespace MikeRogers.NtlmProxy
{
    /// <summary>
    /// Class SimpleHttpServerOptions. This class cannot be inherited.
    /// </summary>
    public sealed class SimpleHttpServerOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance has an Angular content-type header.
        /// </summary>
        /// <value><c>true</c> if this instance has angular content type; otherwise, <c>false</c>.</value>
        public bool HasAngularContentType { get; set; }

        /// <summary>
        /// Gets or sets the request headers.
        /// </summary>
        /// <value>The request headers.</value>
        public Dictionary<string, string> RequestHeaders { get; set; }

        /// <summary>
        /// Gets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the authentication scheme.
        /// </summary>
        /// <value>The authentication scheme.</value>
        public AuthenticationSchemes AuthenticationScheme { get; set; }

        /// <summary>
        /// Should the proxy duplicate individual request headers?
        /// </summary>
        public bool AreHeadersDuplicated { get; set; }

        /// <summary>
        /// List of headers to not repeat when AreHeadersDuplicated is true
        /// </summary>
        public List<string> ExcludedHeaders { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleHttpServerOptions"/> class.
        /// </summary>
        public SimpleHttpServerOptions()
        {
            RequestHeaders = new Dictionary<string, string>();
            AuthenticationScheme = AuthenticationSchemes.Anonymous;
            ExcludedHeaders = new List<string> { "Host", "Accept-Encoding" };
        }

        /// <summary>
        /// The default options
        /// </summary>
        public static SimpleHttpServerOptions GetDefaultOptions()
        {
            return new SimpleHttpServerOptions
            {
                Port = 0,
                AuthenticationScheme = AuthenticationSchemes.Anonymous,
                HasAngularContentType = false,
                AreHeadersDuplicated = false,
                RequestHeaders = new Dictionary<string, string>()
            };
        }
    }
}
