using System.Collections.Generic;
using PocketBase.Net.SDK.Models.Utils;

namespace PocketBase.Net.SDK.Models;

public class Record : BaseModel
{
    public string CollectionId { get; set; } = "";
    public string CollectionName { get; set; } = "";
    // values could be a record or a list of records
    public Dictionary<string, object> Expand { get; set; } = new();
}