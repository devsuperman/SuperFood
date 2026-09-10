using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SuperFood.Api.Common;

/// <summary>
/// Adds the JWT bearer scheme to the generated OpenAPI document so the Scalar
/// UI shows an "Authorize" box and sends `Authorization: Bearer &lt;token&gt;`
/// on tried requests. Grab a token from POST /api/auth/login first.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            In = ParameterLocation.Header,
            BearerFormat = "JSON Web Token"
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    }
}
