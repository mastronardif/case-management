using System.Collections.Generic;
using System.Security.Claims;
namespace WebAppMulti.Database.Auth
{
    public interface IUserStore
    {
        DummyUser? FindByUsername(string username);
        List<string> GetRoles(DummyUser user);
        ClaimsPrincipal CreatePrincipal(DummyUser user);
    }
}
