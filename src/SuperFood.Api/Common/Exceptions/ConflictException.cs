namespace SuperFood.Api.Common.Exceptions;

/// <summary>An expected business-rule conflict (e.g. "table already occupied"), not a bug.</summary>
public class ConflictException(string message) : Exception(message);
