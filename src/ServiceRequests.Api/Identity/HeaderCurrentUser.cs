using ServiceRequests.Application.Common;
using ServiceRequests.Domain;

namespace ServiceRequests.Api.Identity;

/// <summary>
/// Temporary: reads the user from the X-User-Name header until sign-in exists.
/// Replacing it with a claims-based implementation is a one-class change.
/// </summary>
internal sealed class HeaderCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private const string HeaderName = "X-User-Name";

    public string UserName
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString().Trim();
            if (string.IsNullOrEmpty(value) || value.Length > FieldLengths.ChangedBy)
            {
                throw new MissingUserException(
                    $"The {HeaderName} header must contain a user name of up to {FieldLengths.ChangedBy} characters.");
            }

            return value;
        }
    }
}
