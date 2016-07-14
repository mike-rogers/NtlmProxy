using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MikeRogers.NtlmProxy
{
    internal class RetryHandler : DelegatingHandler
    {
        private const int MaxRetries = 3;

        public RetryHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new ByteArrayContent(new byte[0])
            };

            for (var i = 0; i < MaxRetries; i++)
            {
                try
                {
                    response = await base.SendAsync(request, cancellationToken);
                }
                catch (Exception)
                {
                    SleepBeforeRetry(i);
                    continue;
                }

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                SleepBeforeRetry(i);
            }

            return response;
        }

        private static void SleepBeforeRetry(int multiplier)
        {
            Thread.Sleep(1000 * multiplier);
        }
    }
}
