using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MikeRogers.NtlmProxy
{
    internal class RetryHandler : DelegatingHandler
    {
        private readonly int _maxRetries;

        public RetryHandler(HttpMessageHandler innerHandler, int maxRetries) : base(innerHandler)
        {
            _maxRetries = maxRetries;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_maxRetries == 0)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var response = EmptyResponseMesasge();

            for (var i = 0; i < _maxRetries; i++)
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

        private static HttpResponseMessage EmptyResponseMesasge()
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new ByteArrayContent(new byte[0])
            };
        }

        private static void SleepBeforeRetry(int multiplier)
        {
            Thread.Sleep(1000 * multiplier);
        }
    }
}
