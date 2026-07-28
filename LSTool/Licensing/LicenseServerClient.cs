using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LSTool.Licensing
{
    internal sealed class LicenseServerClient
    {
        private const int RequestTimeoutSeconds = 15;
        private const int MaximumAttempts = 3;
        private const int RetryDelayMilliseconds = 250;
        private const int MaximumRedirects = 5;
        private static readonly HttpClient HttpClient = CreateHttpClient();
        private readonly string _apiUrl;

        public LicenseServerClient(string apiUrl)
        {
            _apiUrl = apiUrl;
        }

        public async Task<LicenseServerCallResult> SendAsync(
            string action,
            string credential,
            string deviceHash)
        {
            LicenseServerRequest request = new LicenseServerRequest
            {
                Action = action,
                Credential = credential,
                DeviceHash = deviceHash,
                Product = LeaseVerifier.ProductName,
                RequestId = Guid.NewGuid().ToString("N")
            };
            string requestJson = JsonConvert.SerializeObject(request);
            string lastError =
                "Không kết nối được dịch vụ xác nhận.";

            for (int attempt = 1;
                 attempt <= MaximumAttempts;
                 attempt++)
            {
                try
                {
                    using (CancellationTokenSource timeout =
                           new CancellationTokenSource(
                               TimeSpan.FromSeconds(
                                   RequestTimeoutSeconds)))
                    using (StringContent content = new StringContent(
                               requestJson,
                               Encoding.UTF8,
                               "application/json"))
                    using (HttpResponseMessage response =
                           await SendWithGoogleRedirectsAsync(
                                   content,
                                   timeout.Token)
                               .ConfigureAwait(false))
                    {
                        string responseJson =
                            await response.Content.ReadAsStringAsync()
                                .ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode)
                        {
                            lastError =
                                "Dịch vụ xác nhận trả về HTTP " +
                                (int)response.StatusCode + ".";
                            if (attempt < MaximumAttempts &&
                                ShouldRetry(response.StatusCode))
                            {
                                await DelayBeforeRetryAsync()
                                    .ConfigureAwait(false);
                                continue;
                            }

                            return LicenseServerCallResult.Unreachable(
                                lastError);
                        }

                        LicenseServerResponse? serverResponse =
                            JsonConvert.DeserializeObject<
                                LicenseServerResponse>(
                                responseJson);
                        if (serverResponse == null)
                        {
                            lastError =
                                "Dịch vụ xác nhận trả về dữ liệu rỗng.";
                            if (attempt < MaximumAttempts)
                            {
                                await DelayBeforeRetryAsync()
                                    .ConfigureAwait(false);
                                continue;
                            }

                            return LicenseServerCallResult.Unreachable(
                                lastError);
                        }

                        return LicenseServerCallResult.Reachable(
                            serverResponse);
                    }
                }
                catch (OperationCanceledException)
                {
                    lastError =
                        "Kết nối dịch vụ xác nhận quá thời gian " +
                        RequestTimeoutSeconds + " giây.";
                }
                catch (Exception exception) when (
                    exception is HttpRequestException ||
                    exception is JsonException)
                {
                    lastError =
                        "Không kết nối được dịch vụ xác nhận: " +
                        exception.Message;
                }

                if (attempt < MaximumAttempts)
                {
                    await DelayBeforeRetryAsync().ConfigureAwait(false);
                }
            }

            return LicenseServerCallResult.Unreachable(lastError);
        }

        private async Task<HttpResponseMessage>
            SendWithGoogleRedirectsAsync(
                HttpContent content,
                CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            using (HttpRequestMessage request = new HttpRequestMessage(
                       HttpMethod.Post,
                       _apiUrl))
            {
                request.Content = content;
                response = await HttpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            for (int redirect = 0;
                 redirect < MaximumRedirects &&
                 IsRedirect(response.StatusCode);
                 redirect++)
            {
                Uri? location = response.Headers.Location;
                if (location == null)
                {
                    return response;
                }

                Uri nextUri = location.IsAbsoluteUri
                    ? location
                    : new Uri(new Uri(_apiUrl), location);
                if (!IsTrustedGoogleRedirect(nextUri))
                {
                    return response;
                }

                response.Dispose();
                using (HttpRequestMessage request =
                       new HttpRequestMessage(HttpMethod.Get, nextUri))
                {
                    response = await HttpClient.SendAsync(
                            request,
                            HttpCompletionOption.ResponseHeadersRead,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            return response;
        }

        private static HttpClient CreateHttpClient()
        {
            return new HttpClient(
                new HttpClientHandler
                {
                    AllowAutoRedirect = false
                });
        }

        private static bool IsRedirect(HttpStatusCode statusCode)
        {
            int value = (int)statusCode;
            return value == 301 ||
                   value == 302 ||
                   value == 303 ||
                   value == 307 ||
                   value == 308;
        }

        private static bool ShouldRetry(HttpStatusCode statusCode)
        {
            int value = (int)statusCode;
            return value == 404 ||
                   value == 408 ||
                   value == 429 ||
                   value == 500 ||
                   value == 502 ||
                   value == 503 ||
                   value == 504;
        }

        private static Task DelayBeforeRetryAsync()
        {
            return Task.Delay(RetryDelayMilliseconds);
        }

        private static bool IsTrustedGoogleRedirect(Uri uri)
        {
            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            string host = uri.Host;
            return host.Equals(
                       "script.google.com",
                       StringComparison.OrdinalIgnoreCase) ||
                   host.EndsWith(
                       ".googleusercontent.com",
                       StringComparison.OrdinalIgnoreCase);
        }
    }

    internal sealed class LicenseServerCallResult
    {
        private LicenseServerCallResult(
            bool isReachable,
            string errorMessage,
            LicenseServerResponse? response)
        {
            IsReachable = isReachable;
            ErrorMessage = errorMessage;
            Response = response;
        }

        public bool IsReachable { get; }
        public string ErrorMessage { get; }
        public LicenseServerResponse? Response { get; }

        public static LicenseServerCallResult Reachable(
            LicenseServerResponse response)
        {
            return new LicenseServerCallResult(true, string.Empty, response);
        }

        public static LicenseServerCallResult Unreachable(string message)
        {
            return new LicenseServerCallResult(false, message, null);
        }
    }
}
