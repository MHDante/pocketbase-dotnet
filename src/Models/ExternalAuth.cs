
using PocketBase.Net.SDK.Models.Utils;

namespace PocketBase.Net.SDK.Models;

public class ExternalAuth : BaseModel
{
    public string RecordId { get; set; } = "";
    public string CollectionId { get; set; } = "";
    public string Provider { get; set; } = "";
    public string ProviderId { get; set; } = "";
}