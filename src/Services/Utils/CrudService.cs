using PocketBase.Net.SDK.Models.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

public abstract class CrudService<M> : BaseCrudService<M> where M : BaseModel
{
    /**
     * Base path for the crud actions (without trailing slash, eg. '/admins').
     */
    public abstract string BaseCrudPath { get; }

    /**
     * Returns a promise with all list items batch fetched at once
     * (by default 200 items per request; to change it set the `batch` query param).
     *
     * You can use the generic T to supply a wrapper type of the crud model.
     */
    public virtual Task<List<M>> GetFullList(FullListQueryParams? queryParams = null) =>
        GetFullList(BaseCrudPath, queryParams?.Batch ?? 200, queryParams);

    /**
     * Legacy version of getFullList with explicitly specified batch size.
     */
    public virtual Task<List<M>> GetFullList(int batch, ListQueryParams? queryParams = null) =>
        GetFullList(BaseCrudPath, batch, queryParams);



    /**
     * Returns paginated items list.
     *
     * You can use the generic T to supply a wrapper type of the crud model.
     */
    public virtual Task<ListResult<M>> GetList(int page = 1, int perPage = 30, ListQueryParams? queryParams = null) => 
        GetList(BaseCrudPath, page, perPage, queryParams);

    /**
     * Returns the first found item by the specified filter.
     *
     * Internally it calls `getList(1, 1, { filter })` and returns the
     * first found item.
     *
     * You can use the generic T to supply a wrapper type of the crud model.
     *
     * For consistency with `getOne`, this method will throw a 404
     * ClientResponseError if no item was found.
     */
    public virtual Task<M> GetFirstListItem(string filter, ListQueryParams? queryParams = null) => 
        GetFirstListItem(BaseCrudPath, filter, queryParams);

    /**
     * Returns single item by its id.
     *
     * You can use the generic T to supply a wrapper type of the crud model.
     */
    public virtual Task<M> GetOne(string? id, BaseQueryParams? queryParams = null) => 
        GetOne(BaseCrudPath, id, queryParams);

    /**
     * Creates a new item.
     *
     * You can use the generic T to supply a wrapper type of the crud model.
     */
    public virtual Task<M> Create(M? bodyParams = null, BaseQueryParams? queryParams = null)
        => Create(BaseCrudPath, bodyParams, queryParams);

    /**
     * Updates an existing item by its id.
     *
     * You can use the generic T to supply a wrapper type of the crud model.
     */
    public virtual Task<M> Update(string id, M? bodyParams = null, BaseQueryParams? queryParams = null)
        => Update(BaseCrudPath, id, bodyParams, queryParams);

    /**
     * Deletes an existing item by its id.
     */
    public virtual Task<bool> Delete(string id, BaseQueryParams? queryParams = null)
        => Delete(BaseCrudPath, id, queryParams);

    protected CrudService(Client client) : base(client) { }
}