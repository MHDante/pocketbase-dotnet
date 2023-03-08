using System;
using System.Collections.Generic;
using PocketBase.Net.SDK.Models.Utils;

namespace PocketBase.Net.SDK.Models;

public class Collection : BaseModel
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "base";
    public SchemaField[] Schema { get; set; } = Array.Empty<SchemaField>();
    public bool System { get; set; }
    public string? ListRule { get; set; }
    public string? ViewRule { get; set; }
    public string? CreateRule { get; set; }
    public string? UpdateRule { get; set; }
    public string? DeleteRule { get; set; }
    public Dictionary<string, object> Options { get; set; } = new();


    /**
     * Checks if the current model is "base" collection.
     */
    public bool IsBase => this.Type == "base";

    /**
     * Checks if the current model is "auth" collection.
     */
    public bool IsAuth => this.Type == "auth";

    /**
     * Checks if the current model is "view" collection.
     */
    public bool IsView => this.Type == "views";

}