using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using PocketBase.Net.SDK.Models;

/**
 * PocketBase C# Client.
 */
public class Client
{
    /**
     * The base PocketBase backend url address (eg. "http://127.0.0.1.8090").
     */
    public string BaseUrl { get; set; }

    /**
     * Hook that get triggered right before sending the fetch request,
     * allowing you to inspect and modify the url and request options.
     *
     * For list of the possible options check https://developer.mozilla.org/en-US/docs/Web/API/fetch#options
     *
     * You can return a non-empty result object `{ url, options }` to replace the url and request options entirely.
     *
     * Example:
     * ```js
     * client.beforeSend = function (url, options) {
     *     options.headers = Object.assign({}, options.headers, {
     *         "X-Custom-Header": "example",
     *     });
     *
     *     return { url, options }
     * };
     * ```
     */
    public Func<HttpRequestMessage, HttpRequestMessage?>? BeforeSend { get; set; }


    /**
     * Hook that get triggered after successfully sending the fetch request,
     * allowing you to inspect/modify the response object and its parsed data.
     *
     * Returns the new Promise resolved `data` that will be returned to the client.
     *
     * Example:
     * ```js
     * client.afterSend = function (response, data) {
     *     if (response.status != 200) {
     *         throw new ClientResponseError({
     *             url:      response.url,
     *             status:   response.status,
     *             data:     data,
     *         });
     *     }
     *
     *     return data;
     * };
     * ```
     */
    public Func<HttpResponseMessage, object?, object?>? AfterSend { get; set; }


    /**
     * Optional language code (default to `en-US`) that will be sent
     * with the requests to the server as `Accept-Language` header.
     */
    public string Lang { get; set; }

    /**
     * A replaceable instance of the local auth store service.
     */
    public BaseAuthStore AuthStore { get; set; }

    /**
     * An instance of the service that handles the **Settings APIs**.
     */
    public SettingsService Settings { get; }

    /**
     * An instance of the service that handles the **Admin APIs**.
     */
    public AdminService Admins { get; }

    /**
     * An instance of the service that handles the **Collection APIs**.
     */
    public CollectionService Collections { get; }

    /**
     * An instance of the service that handles the **Log APIs**.
     */
    public LogService Logs { get; }

    /**
     * An instance of the service that handles the **Realtime APIs**.
     */
    public RealtimeService Realtime { get; }

    /**
     * An instance of the service that handles the **Health APIs**.
     */
    public HealthService Health { get; }

    private Dictionary<string, CancellationTokenSource> _cancelControllers = new();
    private Dictionary<string, RecordService> _recordServices = new();
    private bool _enableAutoCancellation = true;


    public IJsonSerializer Serializer { get;  }
    private readonly HttpClient _client;

    public Client(
        IJsonSerializer serializer,
        string baseUrl = "/",
        BaseAuthStore? authStore = null,
        string lang = "en-US",
        HttpClient? client = null
    )
    {
        this.BaseUrl = baseUrl;
        this.Lang = lang;
        this.AuthStore = authStore || new LocalAuthStore();

        // services
        Admins = new AdminService(this);
        Collections = new CollectionService(this);
        Logs = new LogService(this);
        Settings = new SettingsService(this);
        Realtime = new RealtimeService(this);
        Health = new HealthService(this);

        // C# specific implementation
        Serializer = serializer;
        _client = new HttpClient();

    }

    /**
     * Returns the RecordService associated to the specified collection.
     *
     * @param  {string} idOrName
     * @return {RecordService}
     */
    RecordService Collection(string idOrName)
    {
        if (!_recordServices[idOrName])
        {
            _recordServices[idOrName] = new RecordService(this, idOrName);
        }

        return _recordServices[idOrName];
    }

    /**
     * Globally enable or disable auto cancellation for pending duplicated requests.
     */
    Client AutoCancellation(bool enable)
    {
        _enableAutoCancellation = enable;

        return this;
    }

    /**
     * Cancels single request by its cancellation key.
     */
    Client CancelRequest(string cancelKey)
    {
        if (_cancelControllers.ContainsKey(cancelKey))
        {
            _cancelControllers[cancelKey].Cancel();
            _cancelControllers.Remove(cancelKey);
        }

        return this;
    }

    /**
     * Cancels all pending requests.
     */
    Client CancelAllRequests()
    {
        foreach (var kvp in _cancelControllers)
            kvp.Value.Cancel();

        _cancelControllers.Clear();

        return this;
    }

    /**
     * Sends an api http request.
     */
    public async Task<T> Send<T>(string path, HttpRequestMessage options, BaseQueryParams? parameters, Type? deserializedType = null)
    {
        var cancellationToken = CancellationToken.None;
        options.Method ??= HttpMethod.Get;

        // serialize the body if needed and set the correct content type
        // note1: for FormData body the Content-Type header should be skipped
        // note2: we are checking the constructor name because FormData is not available natively in node

        if (options.Content is not null and not MultipartFormDataContent)
        {
            // add the json header (if not already)
            if (!options.Headers.Contains("Content-Type"))
            {
                options.Headers.Add("Content-Type", "application/json");
            }
        }

        // add the Accept-Language header (if not already)
        if (!options.Headers.Contains("Accept-Language"))
        {
            options.Headers.Add("Accept-Language", Lang);
        }

        // check if Authorization header can be added
        if (
            // has stored token
            AuthStore?.Token != null &&
            // auth header is not explicitly set
            options.Headers.Authorization == null
        )
        {
            options.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthStore.Token);
        }

        // handle auto cancellation for duplicated pending request
        if (_enableAutoCancellation && parameters?._autoCancel != false)
        {
            var cancelKey = parameters?._cancelKey ?? (options.Method ?? HttpMethod.Get) + path;

            // cancel previous pending requests
            CancelRequest(cancelKey);

            var controller = new CancellationTokenSource();
            _cancelControllers[cancelKey] = controller;

            cancellationToken = controller.Token;
        }

        // remove the special cancellation params from the other valid query params

        // serialize the query parameters

        // build url + path
        var url = BuildUrl(path, parameters);


        options.RequestUri = url;

        var result = BeforeSend?.Invoke(options);

        if (result != null)
        {
            if (result.RequestUri == null) result.RequestUri = options.RequestUri;
        }

        // send the request
        try
        {
            T? data = default;
            var response = await _client.SendAsync(options, cancellationToken);

            var responseString = await response.Content.ReadAsStringAsync();

            try
            {
                data = deserializedType == null
                    ? Serializer.FromJson<T>(responseString)
                    : (T)Serializer.FromJson(deserializedType, responseString);
            }
            catch (Exception)
            {
                // all api responses are expected to return json
                // with the exception of the realtime event and 204
            }

            if (AfterSend != null)
                data = (T?)AfterSend.Invoke(response, data);

            if ((int)response.StatusCode >= 400)
            {
                throw new ClientResponseError(null, response, url.ToString(), false);
            }

            return data ?? Activator.CreateInstance<T>();
        }
        catch (ClientResponseError e)
        {
            throw;
        }
        catch (OperationCanceledException e) when (e.CancellationToken == cancellationToken)
        {
            throw new ClientResponseError(e, null, url.ToString(), true);
        }
        catch (Exception e)
        {
            // wrap to normalize all errors
            throw new ClientResponseError(e, null, url.ToString(), false);
        }
    }

    /**
* Builds and returns an absolute record file url for the provided filename.
*/
    public Uri GetFileUrl(Record record, string filename, FileQueryParams? queryParams = null)
    {
        var collectionKey = string.IsNullOrEmpty(record.CollectionId) ? record.CollectionName : record.CollectionId;
        var path = string.Join("/",
                "api",
                "files",
                HttpUtility.UrlEncode(collectionKey),
                HttpUtility.UrlEncode(record.Id),
                HttpUtility.UrlEncode(filename)
                )
    ;
        return BuildUrl(path, queryParams);

    }

    /**
    * Builds a full client url by safely concatenating the provided path.
*/
    public Uri BuildUrl(string path, BaseQueryParams? queryParams)
    {
        var builder = new UriBuilder($"{BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}");
        if (queryParams == null) return builder.Uri;

        var query = HttpUtility.ParseQueryString(builder.Query);
        queryParams.AddToQueryParams(query);
        builder.Query = query.ToString();

        return builder.Uri;
    }
}

public interface IJsonSerializer
{
    public string ToJson(object obj);
    public T FromJson<T>(string json);
    public object FromJson(Type t, string json);
    public Dictionary<string, object> DecodeJwt(string fullToken, string jsonPayload);
}