using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;

namespace MikeRogers.NtlmProxy
{
    public sealed class SimpleHttpServerOptions
    {
        public bool AngularContentType { get; set; }

        public Dictionary<string, string> RequestHeaders { get; set; }

        public int Port { get; set; }

        public AuthenticationSchemes AuthenticationScheme { get; set; }

        /// <summary>
        /// Should the proxy duplicate individual request headers?
        /// </summary>
        public bool DuplicateRequestHeaders { get; set; }

        /// <summary>
        /// List of headers to not repeat when DuplicateRequestHeaders is true
        /// </summary>
        public List<string> ForbiddenHeaders { get; set; }

        public SimpleHttpServerOptions()
        {
            RequestHeaders = new Dictionary<string, string>();
            AuthenticationScheme = AuthenticationSchemes.Anonymous;
            ForbiddenHeaders = new List<string>() { "Host", "Accept-Encoding" };
        }

        public static readonly SimpleHttpServerOptions DefaultOptions = new SimpleHttpServerOptions
        {
            Port = 0,
            AuthenticationScheme = AuthenticationSchemes.Anonymous,
            AngularContentType = false,
            DuplicateRequestHeaders = false,
            RequestHeaders = new Dictionary<string, string>()
        };
    }
}
