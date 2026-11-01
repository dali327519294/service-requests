namespace ServiceRequests.Domain.Users;

public class User
{
    public int Id { get; private set; }
    public string UserName { get; private set; }
    public string DisplayName { get; private set; }

    /// <summary>Produced by ASP.NET Core's PasswordHasher; the password itself is never stored.</summary>
    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public User(string userName, string displayName, string passwordHash, UserRole role)
    {
        UserName = userName;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        Role = role;
    }
}
