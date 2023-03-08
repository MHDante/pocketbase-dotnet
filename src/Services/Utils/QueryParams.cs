using System.Collections.Specialized;

public class BaseQueryParams
{
    public bool? _autoCancel { get; set; }
    public string? _cancelKey { get; set; }

    public virtual void AddToQueryParams(NameValueCollection queryParams) { }
}

public class ListQueryParams : BaseQueryParams
{
    public int? Page { get; set; }
    public int? PerPage { get; set; }
    public string? Sort { get; set; }
    public string? Filter { get; set; }

    public override void AddToQueryParams(NameValueCollection queryParams)
    {
        if(Page != null) queryParams.Add("page", Page?.ToString());
        if(PerPage != null) queryParams.Add("perPage", PerPage?.ToString());
        if(Sort != null) queryParams.Add("sort", Sort);
        if(Filter != null) queryParams.Add("filter", Filter);
        base.AddToQueryParams(queryParams);
    }
}

public class FullListQueryParams : ListQueryParams
{
    public int? Batch { get; set; }

    public override void AddToQueryParams(NameValueCollection queryParams)
    {
        if(Batch != null) queryParams.Add("batch", Batch?.ToString());
        base.AddToQueryParams(queryParams);
    }
}

public class RecordQueryParams : BaseQueryParams
{
    public string? Expand { get; set; }

    public override void AddToQueryParams(NameValueCollection queryParams)
    {
        if(Expand != null) queryParams.Add("expand", Expand);
        base.AddToQueryParams(queryParams);
    }
}

public class RecordListQueryParams : ListQueryParams
{
    public string? Expand { get; set; }
    public static implicit operator RecordQueryParams(RecordListQueryParams p) => 
        new() {Expand = p.Expand, _autoCancel = p._autoCancel, _cancelKey = p._cancelKey};

    public override void AddToQueryParams(NameValueCollection queryParams)
    {
        if(Expand != null) queryParams.Add("expand", Expand);
        base.AddToQueryParams(queryParams);
    }
}

public class RecordFullListQueryParams : FullListQueryParams
{
    public string? Expand { get; set; }
    public static implicit operator RecordQueryParams(RecordFullListQueryParams p) => 
        new() {Expand = p.Expand, _autoCancel = p._autoCancel, _cancelKey = p._cancelKey};

    public override void AddToQueryParams(NameValueCollection queryParams)
    {
        if(Expand != null) queryParams.Add("expand", Expand);
        base.AddToQueryParams(queryParams);
    }
}

public class LogStatsQueryParams : BaseQueryParams
{
    public string? Filter { get; set; }

    public override void AddToQueryParams(NameValueCollection queryParams)
    {
        if(Filter != null) queryParams.Add("filter", Filter);
        base.AddToQueryParams(queryParams);
    }
}

public class FileQueryParams : BaseQueryParams
{
    public string? Thumb { get; set; }
    
    public override void AddToQueryParams(NameValueCollection queryParams)
    {
        if(Thumb != null) queryParams.Add("thumb", Thumb);
        base.AddToQueryParams(queryParams);
    }
}