using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using PocketBase.Net.SDK.Models;
using PocketBase.Net.SDK.Models.Utils;

/**
 * Base AuthStore class that is intended to be extended by all other
 * PocketBase AuthStore implementations.
 */
public abstract class BaseAuthStore
{

    protected const string DefaultCookieKey = "pb_auth";

    protected string BaseToken { get; set; } = "";
    private BaseModel? BaseModel { get; set; }

    protected readonly IJsonSerializer Serializer;
    protected readonly List<OnStoreChangeFunc> OnChangeCallbacks = new();

    public delegate void OnStoreChangeFunc(string token, BaseModel? model);

    /**
     * Retrieves the stored token (if any).
     */
    public virtual string Token => BaseToken;

    /**
     * Retrieves the stored model data (if any).
     */
    public BaseModel? Model => BaseModel;

    /**
     * Loosely checks if the store has valid token (aka. existing and unexpired exp claim).
     */
    public virtual bool IsValid => !JwtUtil.IsTokenExpired(Token, Serializer);


    protected BaseAuthStore(IJsonSerializer serializer) => Serializer = serializer;

    /**
     * Saves the provided new token and model data in the auth store.
     */
    public void Save(string? token, Admin? model) => Save(token, (BaseModel?) model);
    public void Save(string? token, Record? model) => Save(token, (BaseModel?) model);
    protected virtual void Save(string? token, BaseModel? model)
    {
        BaseToken = token ?? "";
        // normalize the model instance
        BaseModel = model;
        TriggerChange();
    }

    /**
     * Removes the stored token and model data form the auth store.
     */
    public virtual void Clear()
    {
        BaseToken = "";
        BaseModel = null;
        TriggerChange();
    }

    /**
     * Parses the provided cookie string and updates the store state
     * with the cookie's token and model data.
     *
     * NB! This function doesn't validate the token or its data.
     * Usually this isn't a concern if you are interacting only with the
     * PocketBase API because it has the proper server-side security checks in place,
     * but if you are using the store `isValid` state for permission controls
     * in a node server (eg. SSR), then it is recommended to call `authRefresh()`
     * after loading the cookie to ensure an up-to-date token and model state.
     * For example:
     *
     * ```js
     * pb.authStore.loadFromCookie("cookie string...");
     *
     * try {
     *     // get an up-to-date auth store state by veryfing and refreshing the loaded auth model (if any)
     *     pb.authStore.isValid && await pb.collection("users").authRefresh();
     * } catch (_) {
     *     // clear the auth store on failed refresh
     *     pb.authStore.clear();
     * }
     * ```
     */
    
    public virtual void LoadFromCookie<M>(Cookie cookie, string key = DefaultCookieKey) where M : BaseModel
    {
        AuthData<M>? data = null;

        try
        {
            data = Serializer.FromJson<AuthData<M>>(cookie.Value);
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Failed To Deserialize Cookie");
        }

        Save(data?.Token, data?.Model);
    }

    /**
     * Exports the current store state as cookie string.
     *
     * By default the following optional attributes are added:
     * - Secure
     * - HttpOnly
     * - SameSite=Strict
     * - Path=/
     * - Expires={the token expiration date}
     *
     * NB! If the generated cookie exceeds 4096 bytes, this method will
     * strip the model data to the bare minimum to try to fit within the
     * recommended size in https://www.rfc-editor.org/rfc/rfc6265#section-6.1.
     */
    public virtual Cookie ExportToCookie(Cookie? options = null, string key = DefaultCookieKey)
    {
        options ??= new Cookie
        {
            Secure = true,
            // SameSite = true, // Not supported on .netstandard
            HttpOnly = true,
            Path = "/",
        };

        // extract the token expiration date
        var payload = JwtUtil.GetTokenPayload(Token, Serializer);
        var expiry = JwtUtil.GetTokenExpiryUnixSecs(payload);
        var offset = DateTimeOffset.FromUnixTimeSeconds(expiry ?? 0);

        options.Expires = offset.UtcDateTime;

        // merge with the user defined options
        var textValue = Model switch
        {
            null => Serializer.ToJson(new AuthData<Record> {Token = Token, Model = null}),
            Record record => Serializer.ToJson(new AuthData<Record> {Token = Token, Model = record}),
            Admin admin => Serializer.ToJson(new AuthData<Admin> {Token = Token, Model = admin}),
            _ => throw new ArgumentOutOfRangeException(nameof(Model))
        };



        // The reference source of CookieContainer checks cookie length in this way:
        // see: https://github.com/microsoft/referencesource/blob/51cf7850defa8a17d815b4700b67116e3fa283c2/System/net/System/Net/cookiecontainer.cs#L289

        if (textValue.Length > CookieContainer.DefaultCookieLengthLimit)
        {
            var smallCookie = Serializer.FromJson<Dictionary<string, object>>(textValue);
            var modelDict = smallCookie.TryGetValue("model", out var value) ? value as Dictionary<string, object> : null;
            if (modelDict == null) throw new Exception("Cookie Exceeds maximum size, unable to edit due to serializer error");
            var keys = modelDict.Keys.ToList();
            foreach (var modelKey in keys)
            {
                if (modelKey.Equals("id", StringComparison.InvariantCultureIgnoreCase)) continue;
                if (modelKey.Equals("email", StringComparison.InvariantCultureIgnoreCase)) continue;
                if (Model is Record)
                {
                    if (modelKey.Equals("username", StringComparison.InvariantCultureIgnoreCase)) continue;
                    if (modelKey.Equals("verified", StringComparison.InvariantCultureIgnoreCase)) continue;
                    if (modelKey.Equals("collectionId", StringComparison.InvariantCultureIgnoreCase)) continue;
                }
                modelDict.Remove(modelKey);
            }
            textValue = Serializer.ToJson(modelDict);
        }
        options.Value = textValue;
        return options;
    }

    /**
     * Register a callback function that will be called on store change.
     *
     * You can set the `fireImmediately` argument to true in order to invoke
     * the provided callback right after registration.
     *
     * Returns a removal function that you could call to "unsubscribe" from the changes.
     */
    public virtual Action OnChange(OnStoreChangeFunc callback, bool fireImmediately = false)
    {
        OnChangeCallbacks.Add(callback);

        if (fireImmediately)
        {
            callback(Token, BaseModel);
        }

        return () => OnChangeCallbacks.Remove(callback);
    }

    protected virtual void TriggerChange()
    {
        foreach (var callback in OnChangeCallbacks)
        {
            callback?.Invoke(Token, BaseModel);
        }
    }
    

    protected class AuthData<M> where M : BaseModel
    {
        public string Token { get; set; } = "";
        public M? Model { get; set; }
    }
}