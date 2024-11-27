namespace DataServices.Core;

using Microsoft.Azure.Functions.Worker.Http;

public delegate bool AccessRule(HttpRequestData req);
public class AuthenticationConfiguration
{
    public AuthenticationConfiguration(
        AccessRule canGet,
        AccessRule canGetById,
        AccessRule canDelete,
        AccessRule canPost,
        AccessRule canPut)
    {
        CanGet = canGet;
        CanGetById = canGetById;
        CanDelete = canDelete;
        CanPost = canPost;
        CanPut = canPut;

    }
    public AccessRule CanGet {get;}
    public AccessRule CanGetById {get;}
    public AccessRule CanDelete {get;}
    public AccessRule CanPost {get;}
    public AccessRule CanPut {get;}


}
