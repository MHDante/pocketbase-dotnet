using PocketBase.Net.SDK;

/**
 * BaseService class that should be inherited from all API services.
 */
public abstract class BaseService
{
    public Client Client { get; }

    protected BaseService(Client client) => 
        Client = client;
}