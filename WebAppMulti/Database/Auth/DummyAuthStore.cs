using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace WebAppMulti.Database.Auth
{
    public class DummyUser
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public List<string> Roles { get; set; } = new();
    }

    public class DummyAuthStore : IUserStore
    {
        // Pretend this is your "database"
        private static readonly List<DummyUser> Users = new()
        {
            new DummyUser { Id = "1", UserName = "frank", Roles = new List<string>{ "Admin", "User" }},
            new DummyUser { Id = "2", UserName = "jane", Roles = new List<string>{ "User" }},
            new DummyUser { Id = "3", UserName = "bob", Roles = new List<string>{ "Manager" }}
        };

        public DummyUser? FindByUsername(string username)
        {
            return Users.FirstOrDefault(u => u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public List<string> GetRoles(DummyUser user)
        {
            return user.Roles;
        }

        public ClaimsPrincipal CreatePrincipal(DummyUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            // Add role claims
            claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, "DummyAuth");
            return new ClaimsPrincipal(identity);
        }
    }


}
