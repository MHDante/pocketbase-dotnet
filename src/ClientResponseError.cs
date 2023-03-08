using System;
using System.Net;
using System.Net.Http;

/**
 * ClientResponseError is a custom Error class that is intended to wrap
 * and normalize any error thrown by `Client.send()`.
 */
public class ClientResponseError : Exception
{
    public string Url { get; } = "";
    public HttpStatusCode? Status { get; }
    public HttpResponseMessage? Response { get; }
    public bool IsAbort { get; }
    public Exception? OriginalError { get; }

    public override string Message { get; }

    public ClientResponseError(HttpStatusCode status, string message) => 
        Message = message;

    public ClientResponseError(Exception? errData, HttpResponseMessage? response, string? url, bool isAbort)
    {
        if (!(errData is ClientResponseError))
        {
            this.OriginalError = errData;
        }

        Url = url ?? "";
        Response = response;
        Status = response?.StatusCode;
        IsAbort = isAbort;
        
        var initMessage = $"ClientResponseError {Status}\n";
        var message = response?.ReasonPhrase;
        if (message == null)
        {
            if (IsAbort)
            {
                message = "The request was auto-cancelled. You can find more info in https://github.com/pocketbase/js-sdk#auto-cancellation.";
            }
            else if (OriginalError is WebException { Status: WebExceptionStatus.ConnectFailure })
            {
                message = "Failed to connect to the PocketBase server. Try changing the SDK URL from localhost to 127.0.0.1 (https://github.com/pocketbase/js-sdk/issues/21).";
            }
            else
            {
                message = "Something went wrong while processing your request.";
            }
        }

        Message = initMessage + message;

    }


}