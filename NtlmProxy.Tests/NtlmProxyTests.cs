using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using RestSharp;
using System.Text;

namespace MikeRogers.NtlmProxy.Tests
{
    /// <summary>
    /// These are the worst tests I have ever written.
    /// </summary>
    [TestFixture]
    public sealed class NtlmProxyTests
    {
        /// <summary>
        /// The types of HTTP methods supported.
        /// </summary>
        private static readonly object[] HttpMethods =
        {
            Method.GET,
            Method.POST,
            Method.DELETE,
            Method.PUT
        };

        /// <summary>
        /// This test will create a stub server that accepts NTLM (<code>server</code>).
        /// It will then create the proxy (<code>proxy</code>).
        /// Finally it issues a web request to the proxy.
        /// 
        /// It passes if the response from <code>server</code> is returned correctly,
        /// and if the credentials make it all the way from the web request through
        /// <code>proxy</code> to <code>server</code>.
        /// </summary>
        /// <param name="method">The HTTP method to be tested.</param>
        [Test, TestCaseSource("HttpMethods")]
        public void ShouldProxyCredentialsOnRequest(Method method)
        {
            const string expectedResultText = "I enjoy cows.";
            var currentCredentials = System.Security.Principal.WindowsIdentity.GetCurrent();
            Debug.Assert(currentCredentials != null);

            // ReSharper disable CSharpWarnings::CS1998 (I don't know async/await well enough to fix this)
            using (var server = new SimpleHttpServer(async context =>
            // ReSharper restore CSharpWarnings::CS1998
            {
                var response = new HttpResponseMessage
                {
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(expectedResultText)))
                };

                Assert.That(context.User.Identity.Name, Is.EqualTo(currentCredentials.Name));

                return response;
            }, 0, AuthenticationSchemes.Ntlm))
            {
                var serverUri = new Uri(string.Format("http://localhost:{0}/", server.Port));
                using (var proxy = new NtlmProxy(serverUri))
                {
                    var proxyUrl = string.Format("http://localhost:{0}/", proxy.Port);

                    var client = new RestClient(proxyUrl)
                    {
                        Authenticator = new NtlmAuthenticator()
                    };

                    var request = new RestRequest("/", method);

                    var response = client.Execute(request);

                    Assert.That(response.Content, Is.EqualTo(expectedResultText));
                }
            }
        }
    }
}
