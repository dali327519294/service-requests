namespace ServiceRequests.Application.Common;

/// <summary>The request does not say who is making it; answered with 400.</summary>
public sealed class MissingUserException(string message) : Exception(message);
