using PocketBase.Net.SDK.Models.Utils;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

// @todo since there is no longer need of SubCrudService consider merging with CrudService in v0.9+
public abstract class BaseCrudService<M> : BaseService where M : BaseModel
{
    /**
     * Returns a promise with all list items batch fetched at once.
     */
    protected async Task<List<M>> GetFullList(string basePath, int batchSize = 200, ListQueryParams? queryParams = null)
    {
        var result = new List<M>();
        int pageCount = 1;

        while (true)
        {
            var page = await GetList(basePath, pageCount, batchSize, queryParams);

            var items = page.Items;
            var totalItems = page.TotalItems;

            result.AddRange(items);

            if (items.Count <= 0 || totalItems <= result.Count)
            {
                break;
            }

            pageCount++;
        }

        return result;
    }

    /**
     * Returns paginated items list.
     */
    protected async Task<ListResult<M>> GetList(string basePath, int page = 1, int perPage = 30, ListQueryParams? queryParams = null)
    {
        queryParams ??= new();
        queryParams.Page ??= page;
        queryParams.PerPage ??= perPage;

        var options = new HttpRequestMessage();
        options.Method = HttpMethod.Get;

        var responseData = await Client.Send<ListResult<M>>(basePath, options, queryParams);

        return responseData;
    }

    /**
     * Returns single item by its id.
     */
    protected async Task<M> GetOne(string basePath, string id, BaseQueryParams? queryParams = null)
    {
        var path = basePath + '/' + HttpUtility.HtmlEncode(id);
        var options = new HttpRequestMessage();
        options.Method = HttpMethod.Get;
        var responseData = await Client.Send<M>(path, options, queryParams);
        return responseData;

    }

    /**
     * Returns the first found item by a list filter.
     *
     * Internally it calls `_getList(basePath, 1, 1, { filter })` and returns its
     * first item.
     *
     * For consistency with `_getOne`, this method will throw a 404
     * ClientResponseError if no item was found.
     */
    protected async Task<M> GetFirstListItem(string basePath, string filter, ListQueryParams? queryParams = null)
    {

        queryParams ??= new();
        queryParams.Filter ??= filter;
        queryParams._cancelKey ??= "one_by_filter_" + basePath + "_" + filter;

        var result = await GetList(basePath, 1, 1, queryParams);
        if (result?.Items?.Count is null or 0)
        {
            throw new ClientResponseError(HttpStatusCode.NotFound, "The requested resource wasn't found.");
        }

        return result.Items[0];

    }

    /**
     * Creates a new item.
     */
    protected async Task<M> Create(string basePath, M? bodyParams, BaseQueryParams? queryParams = null)
    {
        var options = new HttpRequestMessage();
        options.Method = HttpMethod.Post;
        if (bodyParams != null) options.Content = new StringContent(Client.Serializer.ToJson(bodyParams));
        var responseData = await Client.Send<M>(basePath, options, queryParams);
        return responseData;

    }

    /**
     * Updates an existing item by its id.
     */
    protected async Task<M> Update(string basePath, string id, M? bodyParams, BaseQueryParams? queryParams = null)
    {
        var path = basePath + '/' + HttpUtility.HtmlEncode(id);
        var options = new HttpRequestMessage();
        options.Method = HttpUtils.Patch;
        if (bodyParams != null) options.Content = new StringContent(Client.Serializer.ToJson(bodyParams));
        var responseData = await Client.Send<M>(path, options, queryParams);
        return responseData;
    }

    /**
     * Deletes an existing item by its id.
     */
    protected async Task<bool> Delete(string basePath, string id, BaseQueryParams? queryParams = null)
    {
        var path = basePath + '/' + HttpUtility.HtmlEncode(id);
        var options = new HttpRequestMessage();
        options.Method = HttpMethod.Delete;
        var responseData = await Client.Send<object>(path, options, queryParams);
        return true;
    }

    protected BaseCrudService(Client client) : base(client)
    {
    }
}

internal static class HttpUtils
{
    public static readonly HttpMethod Patch = new("PATCH");
}