using System.Collections.Generic;

namespace PocketBase.Net.SDK.Models.Utils;

public class SchemaField
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool System { get; set; }
    public bool Required { get; set; }
    public bool Unique { get; set; }
    public Dictionary<string, object> Options { get; set; } = new();

}