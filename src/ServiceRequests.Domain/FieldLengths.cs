namespace ServiceRequests.Domain;

/// <summary>Maximum text lengths, shared by the database schema and request validation.</summary>
public static class FieldLengths
{
    public const int Title = 200;
    public const int OrganizationName = 200;
    public const int AssignedTo = 100;
    public const int ChangedBy = 100;
    public const int Search = 100;
    public const int UserName = 50;
    public const int DisplayName = 100;
}
