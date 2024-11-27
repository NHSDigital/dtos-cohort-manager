namespace  DataServiceTests;
using DataServices.Core;
using Microsoft.EntityFrameworkCore.Design;

public static class DataServiceTestHelper
{
    public static AccessRule AllowAccess = i => true;
    public static AccessRule DenyAccess = i => false;
    public static AuthenticationConfiguration AllowAllAccessConfig = new AuthenticationConfiguration(AllowAccess,AllowAccess,AllowAccess,AllowAccess,AllowAccess);
    public static AuthenticationConfiguration DenyAllAccessConfig = new AuthenticationConfiguration(DenyAccess,DenyAccess,DenyAccess,DenyAccess,DenyAccess);

}
