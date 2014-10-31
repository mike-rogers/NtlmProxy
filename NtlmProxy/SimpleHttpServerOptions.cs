using System.Collections.Generic;
using System.Net;

namespace MikeRogers.NtlmProxy
{
    public sealed class SimpleHttpServerOptions
    {
        public bool AngularContentType { get; set; }

        public Dictionary<string, string> RequestHeaders { get; set; }

        public int Port { get; private set; }

        public AuthenticationSchemes AuthenticationScheme { get; set; }

        /// <summary>
        /// Should the proxy duplicate individual request headers?
        /// </summary>
        public bool AreHeadersDuplicated { get; set; }

        /// <summary>
        /// List of headers to not repeat when AreHeadersDuplicated is true
        /// </summary>
        public List<string> ExcludedHeaders { get; private set; }

        public SimpleHttpServerOptions()
        {
            RequestHeaders = new Dictionary<string, string>();
            AuthenticationScheme = AuthenticationSchemes.Anonymous;
            ExcludedHeaders = new List<string> { "Host", "Accept-Encoding" };
        }

        public static readonly SimpleHttpServerOptions DefaultOptions = new SimpleHttpServerOptions
        {
            Port = 0,
            AuthenticationScheme = AuthenticationSchemes.Anonymous,
            AngularContentType = false,
            AreHeadersDuplicated = false,
            RequestHeaders = new Dictionary<string, string>()
        };
    }
}
