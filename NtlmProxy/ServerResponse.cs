using System.Net;
using System.Net.Http;

namespace MikeRogers.NtlmProxy
{
    public class ServerResponse
    {
        public HttpResponseMessage Message { get; set; }

        public CookieCollection Cookies { get; set; }
    }
}
