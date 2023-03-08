using PocketBase.Net.SDK.Models;
using System;
using System.Threading.Tasks;

public class AdminAuthResponse
{

    public string token;
    public Admin admin;
}

public class AdminRequest
{

    public string token;
    public Admin admin;
}

public class AdminService : CrudService<Admin>
{

    /**
     * @inheritdoc
     */
    public override string BaseCrudPath => "/api/admins";


    // ---------------------------------------------------------------
    // Post update/delete AuthStore sync
    // ---------------------------------------------------------------

    /**
     * @inheritdoc
     *
     * If the current `client.authStore.model` matches with the updated id, then
     * on success the `client.authStore.model` will be updated with the result.
     */
    public override async Task<Admin> Update(string id, Admin? bodyParams = null, BaseQueryParams? queryParams = null)
    {
        var item = await base.Update(id, bodyParams, queryParams);
        // update the store state if the updated item id matches with the stored model
        if (Client.AuthStore is { Model: Admin a } && a.Id == item.Id)
            Client.AuthStore.Save(Client.AuthStore.Token, item);

        return item;
    }

    /**
     * @inheritdoc
     *
     * If the current `client.authStore.model` matches with the deleted id,
     * then on success the `client.authStore` will be cleared.
     */
    public override async Task<bool> Delete(string id, BaseQueryParams? queryParams = null)
    {
        var success = await base.Delete(id, queryParams);
        // clear the store state if the deleted item id matches with the stored model
        if (
            success && Client.AuthStore is { Model: Admin a } && a.Id == id)
        {
            Client.AuthStore.Clear();
        }
        return success;
    }

    // ---------------------------------------------------------------
    // Auth handlers
    // ---------------------------------------------------------------

    /**
     * Prepare successful authorize response.
     */
    protected virtual AdminAuthResponse authResponse(object responseData: any)
    {
        const admin = this.decode(responseData?.admin || { });

        if (responseData?.token && responseData?.admin)
        {
            this.client.authStore.save(responseData.token, admin);
        }

        return Object.assign({ }, responseData, {
            // normalize common fields
            "token": responseData?.token || "",
            "admin": admin,
        });
    }

    /**
     * Authenticate an admin account with its email and password
     * and returns a new admin token and data.
     *
     * On success this method automatically updates the client"s AuthStore data.
     */
    Task<AdminAuthResponse> authWithPassword(
        string email,
        string password,
        AdminRequest? bodyParams = null,
        BaseQueryParams? = null,
    )  {
    bodyParams = Object.assign({
        "identity": email,
            "password": password,
        }, bodyParams);

return this.client.send(this.BaseCrudPath + "/auth-with-password", {
    "method":  "POST",
            "params":  queryParams,
            "body":    bodyParams,
        }).then(this.authResponse.bind(this));
}

/**
 * Refreshes the current admin authenticated instance and
 * returns a new token and admin data.
 *
 * On success this method automatically updates the client"s AuthStore data.
 */
authRefresh(bodyParams = { }, queryParams: BaseQueryParams = { }): Task<AdminAuthResponse> {
    return this.client.send(this.BaseCrudPath + "/auth-refresh", {
        "method": "POST",
            "params": queryParams,
            "body":   bodyParams,
        }).then(this.authResponse.bind(this));
}

/**
 * Sends admin password reset request.
 */
requestPasswordReset(
    email: string,
    bodyParams = { },
        queryParams: BaseQueryParams = { },
    ): Task<boolean> {
    bodyParams = Object.assign({
        "email": email,
        }, bodyParams);

    return this.client.send(this.BaseCrudPath + "/request-password-reset", {
        "method": "POST",
            "params": queryParams,
            "body":   bodyParams,
        }).then(() => true);
}

/**
 * Confirms admin password reset request.
 */
Task<bool> confirmPasswordReset(
    passwordResetToken: string,
        password: string,
        passwordConfirm: string,
        bodyParams = { },
        queryParams: BaseQueryParams = { },
    ) {
    bodyParams = Object.assign({
        "token":           passwordResetToken,
            "password":        password,
            "passwordConfirm": passwordConfirm,
        }, bodyParams);

    return this.client.send(this.BaseCrudPath + "/confirm-password-reset", {
        "method": "POST",
            "params": queryParams,
            "body":   bodyParams,
        }).then(() => true);
}
}