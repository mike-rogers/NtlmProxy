using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using RestSharp;
using System.Text;
using System.Threading.Tasks;

namespace MikeRogers.NtlmProxy.Tests
{
    /// <summary>
    /// These are the worst tests I have ever written.
    /// </summary>
    [TestFixture]
    public sealed class NtlmProxyTests
    {
        /// <summary>
        /// Sample text string to be served by the fake server.
        /// </summary>
        private const string ExpectedResultText = "I enjoy cows.";

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
        /// Passes if the response from <code>server</code> is returned correctly,
        /// and if the credentials make it all the way from the web request through
        /// <code>proxy</code> to <code>server</code>.
        /// </summary>
        /// <param name="method">The HTTP method to be tested.</param>
        [Test, TestCaseSource("HttpMethods")]
        public void ShouldProxyCredentialsOnRequest(Method method)
        {
            var serverOptions = SimpleHttpServerOptions.GetDefaultOptions();
            var proxyOptions = SimpleHttpServerOptions.GetDefaultOptions();
            serverOptions.AuthenticationScheme = AuthenticationSchemes.Ntlm;
            proxyOptions.AuthenticationScheme = AuthenticationSchemes.Ntlm;

            var serverAssertion = new Action<HttpListenerContext>(context =>
            {
                var currentCredentials = System.Security.Principal.WindowsIdentity.GetCurrent();
                if (currentCredentials == null)
                {
                    Assert.Fail("Unable to determine current Windows user credentials");
                }
                else
                {
                    Assert.That(context.User.Identity.Name, Is.EqualTo(currentCredentials.Name));
                }
            });

            var clientAssertion = new Action<NtlmProxy>(proxy =>
            {
                var proxyUrl = string.Format("http://localhost:{0}/", proxy.Port);

                var client = new RestClient(proxyUrl)
                {
                    Authenticator = new NtlmAuthenticator()
                };

                var request = new RestRequest("/", method);

                var response = client.Execute(request);

                Assert.That(response.Content, Is.EqualTo(ExpectedResultText));
            });

            ExecuteTestInContext(serverOptions, proxyOptions, serverAssertion, clientAssertion);
        }

        /// <summary>
        /// https://github.com/svantreeck reported in https://github.com/mike-rogers/NtlmProxy/pull/2 that
        /// Angular.js has a weird behavior with certain ContentType strings. The option under test
        /// removes the 'charset' string from the ContentType.
        /// </summary>
        [Test]
        public void ShouldRemoveCharsetForAngularOption()
        {
            var options = SimpleHttpServerOptions.GetDefaultOptions();
            options.HasAngularContentType = true;

            var serverAssertion = new Action<HttpListenerContext>(context =>
            {
                var contentType = context.Request.ContentType;
                Assert.That(contentType == null || !contentType.Contains("charset="));
            });

            var clientAssertion = new Action<NtlmProxy>(proxy =>
            {
                var proxyUrl = string.Format("http://localhost:{0}/", proxy.Port);

                var client = new RestClient(proxyUrl);

                var request = new RestRequest("/", Method.GET);

                client.Execute(request);
            });

            ExecuteTestInContext(options, SimpleHttpServerOptions.GetDefaultOptions(), serverAssertion, clientAssertion);
        }

        /// <summary>
        /// Should be able to inject headers into proxied requests.
        /// </summary>
        [Test]
        public void ShouldInjectHeadersIntoProxiedRequest()
        {
            var serverOptions = SimpleHttpServerOptions.GetDefaultOptions();
            var proxyOptions = SimpleHttpServerOptions.GetDefaultOptions();
            proxyOptions.RequestHeaders = new Dictionary<string, string>
            {
                {"HeaderOne", "Donkey"},
                {"HeaderTwo", "Pony"}
            };

            var serverAssertion = new Action<HttpListenerContext>(context =>
            {
                Assert.That(context.Request.Headers["HeaderOne"], Is.EqualTo("Donkey"));
                Assert.That(context.Request.Headers["HeaderTwo"], Is.EqualTo("Pony"));
            });

            var clientAssertion = new Action<NtlmProxy>(proxy =>
            {
                var proxyUrl = string.Format("http://localhost:{0}/", proxy.Port);

                var client = new RestClient(proxyUrl);

                var request = new RestRequest("/", Method.GET);

                client.Execute(request);
            });

            ExecuteTestInContext(serverOptions, proxyOptions, serverAssertion, clientAssertion);
        }

        /// <summary>
        /// The responses from the server should be added to the response object
        /// passed back to the executing context.
        /// </summary>
        [Test]
        public void ShouldDuplicateHeadersInProxiedRequest()
        {
            var proxyOptions = new SimpleHttpServerOptions { AreHeadersDuplicated = true };

            var serverAssertion = new Action<HttpListenerContext>(context =>
            {
                Assert.That(context.Request.Headers["HeaderOne"], Is.EqualTo("Donkey"));
                Assert.That(context.Request.Headers["HeaderTwo"], Is.EqualTo("Pony"));
            });

            var clientAssertion = new Action<NtlmProxy>(proxy =>
            {
                var proxyUrl = string.Format("http://localhost:{0}/", proxy.Port);

                var client = new RestClient(proxyUrl);

                var request = new RestRequest("/", Method.GET);
                request.AddHeader("HeaderOne", "Donkey");
                request.AddHeader("HeaderTwo", "Pony");

                client.Execute(request);
            });

            ExecuteTestInContext(SimpleHttpServerOptions.GetDefaultOptions(), proxyOptions, serverAssertion, clientAssertion);
        }

        /// <summary>
        /// The responses from the server should be added to the response object
        /// passed back to the executing context.
        /// </summary>
        [Test]
        public void ShouldIncludeProxiedResponseDataBackToClient()
        {
            var options = SimpleHttpServerOptions.GetDefaultOptions();

            var clientAssertion = new Action<NtlmProxy>(proxy =>
            {
                var proxyUrl = string.Format("http://localhost:{0}/", proxy.Port);

                var client = new RestClient(proxyUrl);

                var request = new RestRequest("/", Method.GET);

                var response = client.Execute(request);

                Assert.That(response.ContentLength, Is.EqualTo(13));
                Assert.That(response.ContentType, Is.Empty);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(response.StatusDescription, Is.EqualTo("OK"));
            });

            ExecuteTestInContext(options, SimpleHttpServerOptions.GetDefaultOptions(), null, clientAssertion);
        }

        /// <summary>
        /// Should proxy simple GET request per read me instructions.
        /// </summary>
        [Test]
        public void ShouldProxySimpleGetRequestOnCustomPort()
        {
            var proxyOptions = new SimpleHttpServerOptions
            {
                Port = 3999
            };

            var serverOptions = SimpleHttpServerOptions.GetDefaultOptions();

            var clientAssertion = new Action<NtlmProxy>(proxy =>
            {
                var proxyUrl = string.Format("http://localhost:{0}/", proxy.Port);

                var client = new RestClient(proxyUrl);

                var request = new RestRequest("/", Method.GET);

                var response = client.Execute(request);

                Assert.That(response.ContentLength, Is.EqualTo(13));
                Assert.That(response.ContentType, Is.Empty);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(response.StatusDescription, Is.EqualTo("OK"));
            });

            ExecuteTestInContext(serverOptions, proxyOptions, null, clientAssertion);
        }

        /// <summary>
        /// This method creates a stub server (<code>server</code>).
        /// It will then create the proxy (<code>proxy</code>).
        /// Finally it issues a web request to the proxy.
        /// </summary>
        /// <param name="serverOptions">The options for the running HTTP server.</param>
        /// <param name="proxyOptions">The options for the running proxy server.</param>
        /// <param name="serverAssertion">The configuration/assertion code to run in the context of the server.</param>
        /// <param name="clientAssertion">The configuration/assertion code to run in the context of the client.</param>
        private static void ExecuteTestInContext(
            SimpleHttpServerOptions serverOptions,
            SimpleHttpServerOptions proxyOptions,
            Action<HttpListenerContext> serverAssertion,
            Action<NtlmProxy> clientAssertion)
        {
            using (var server = new SimpleHttpServer(context =>
            {
                return new Task<HttpResponseMessage>(() =>
                {
                    var response = new HttpResponseMessage
                    {
                        Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(ExpectedResultText)))
                    };

                    if (serverAssertion != null)
                    {
                        serverAssertion(context);
                    }

                    return response;
                });
            }, serverOptions))
            {
                var serverUri = new Uri(string.Format("http://localhost:{0}/", server.Port));
                using (var proxy = new NtlmProxy(serverUri, proxyOptions))
                {
                    clientAssertion(proxy);
                }
            }
        }
    }
}
