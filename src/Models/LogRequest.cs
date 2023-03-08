using System.Collections.Generic;
using PocketBase.Net.SDK.Models.Utils;

namespace PocketBase.Net.SDK.Models;

public class LogRequest : BaseModel {
    public string Url { get; set; } = "";
    public string Method { get; set; } = "GET";
    public int Status { get; set; } = 200;
    public string Auth { get; set; } = "guest";
    public string RemoteIp { get; set; } = "";
    public string UserIp { get; set; } = "";
    public string Referer { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public Dictionary<string, object> Meta { get; set; } = new();
    
}