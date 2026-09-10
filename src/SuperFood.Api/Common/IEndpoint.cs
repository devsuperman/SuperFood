namespace SuperFood.Api.Common;

/// <summary>
/// Every vertical slice's endpoint implements this so it self-registers its
/// route — adding a slice never requires touching Program.cs (docs/tech-stack.md §3).
/// </summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
