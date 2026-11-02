namespace ServiceRequests.Application.Common;

/// <summary>The user making the current request; recorded as ChangedBy in the status history.</summary>
public interface ICurrentUser
{
    /// <exception cref="MissingUserException">The request does not identify a valid user.</exception>
    string UserName { get; }
}
