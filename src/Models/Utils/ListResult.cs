
using System.Collections.Generic;

namespace PocketBase.Net.SDK.Models.Utils;

public class ListResult<TModel> where TModel : BaseModel
{
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public List<TModel> Items { get; set; }

    public ListResult(int page, int perPage, int totalItems, int totalPages, List<TModel>? items)
    {
        Page = page > 0 ? page : 1;
        PerPage = perPage >= 0 ? perPage : 0;
        TotalItems = totalItems >= 0 ? totalItems : 0;
        TotalPages = totalPages >= 0 ? totalPages : 0;
        Items = items ?? new List<TModel>();
    }
}