namespace ServiceRequests.Domain.Users;

public enum UserRole : byte
{
    /// <summary>Views requests and updates a single request.</summary>
    Handler = 1,

    /// <summary>Everything a handler can do, plus bulk updates.</summary>
    Manager = 2,
}
