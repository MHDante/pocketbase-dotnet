
using System.Collections.Generic;
using PocketBase.Net.SDK.Models.Utils;

/**
 *  Default token store.
 *  Does not function as it does in JS-SDK since .NET Standard cannot access local storage
 *  Only stores auth in memory. This is true even in Blazor
 */
public class LocalAuthStore : BaseAuthStore
{
    public LocalAuthStore(IJsonSerializer serializer) : base(serializer) { }
    
}
