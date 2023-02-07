using Java.Net;
using MAUI_HttpService.Helpers;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Timeout;
using Polly.Wrap;
using System.Net;
using System.Text;

namespace MAUI_HttpService.Services;

public static class HttpManager
{
    static readonly AsyncPolicyWrap _retryPolicy = Policy
        .Handle<TimeoutRejectedException>()
        .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(1), (exception, timespan, retryAttempt, context) =>
        {
            // Text stuff, not important
        })
        .WrapAsync(Policy.TimeoutAsync(11, TimeoutStrategy.Pessimistic));

    static bool debug = false;
    static HttpsClientHandlerService handler = new HttpsClientHandlerService();
    static HttpClient HttpClient = debug ?
        new HttpClient(handler.GetPlatformMessageHandler()) :
        new HttpClient(new PolicyHttpMessageHandler(GetRetryPolicy())
        {
            InnerHandler = new HttpClientHandler()
        });
        //new HttpClient();

    // Retry policy as a method
    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        var tttt = Policy
        .Handle<TimeoutRejectedException>()
        .OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.InternalServerError)
        .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(1), (exception, timespan, retryAttempt, context) =>
        {
            // Text stuff, not important
        });
        var zzz = Policy.TimeoutAsync(11, TimeoutStrategy.Optimistic);
        return tttt.WrapAsync(zzz);
    }

    public static async Task<string> GetAsync(string endpoint, bool shouldRetry = true)
    {
        //the local iis and local api calls work
        string baseAddress = debug ? "http://10.0.2.2:5122/" : "INTERNET URL CAUSES THE ERROR";

        // Get request
        var response = await Get(baseAddress + endpoint, shouldRetry);
        if (response.StatusCode is HttpStatusCode.RequestTimeout)
            return "Operation Timed Out";

        return await response.Content.ReadAsStringAsync();
    }

    public static async Task<string> PostAsync(string endpoint, string json, bool shouldRetry = true)
    {
        //the local iis and local api calls work
        string baseAddress = debug ? "http://10.0.2.2:5122/" : "INTERNET URL CAUSES THE ERROR";

        // Post request
        // TODO: Fix cannot access a closed stream
        //var response = await _retryPolicy.ExecuteAndCaptureAsync(async token =>
        //    await HttpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), token), CancellationToken.None);

        var response = await HttpClient.PostAsync(baseAddress + endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
        if (response.StatusCode is HttpStatusCode.RequestTimeout)
            return "Operation Timed Out";

        return await response.Content.ReadAsStringAsync();
    }

    #region Helper methods
    static async Task<HttpResponseMessage> Get(string url, bool shouldRetry)
    {
        if (shouldRetry)
        {
            var response = await _retryPolicy.ExecuteAndCaptureAsync(async token => 
                await HttpClient.GetAsync(url, token), CancellationToken.None);

            if (response.Outcome == OutcomeType.Failure)
                if (response.FinalException is TimeoutRejectedException)
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.RequestTimeout };
                else return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(response.FinalException.Message)
                };

            return response.Result;
        }

        try
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(11));
            return await HttpClient.GetAsync(url, cts.Token);
        }
        catch (WebException)
        {
            return new HttpResponseMessage { StatusCode = HttpStatusCode.RequestTimeout };
        }
    }
    #endregion
}
