namespace SuperFood.Api.Common.Exceptions;

/// <summary>An expected "no such record" outcome, not a bug (docs/tech-stack.md §8).</summary>
public class NotFoundException(string message) : Exception(message);
