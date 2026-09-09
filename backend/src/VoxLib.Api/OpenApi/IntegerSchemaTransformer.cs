using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace VoxLib.Api.OpenApi;

/// <summary>
/// Publishes whole numbers as integers.
/// <para>
/// ASP.NET Core describes an int32 as <c>type: ["integer", "string"]</c> with a
/// numeric pattern, because a string carrying digits would also bind. That is
/// true of the binder and untrue of the responses, and every client generated
/// from the document inherits it: <c>page</c> arrives typed as
/// <c>number | string</c>, so callers either cast or carry the lie onwards.
/// contracts/catalogue.yaml says <c>type: integer</c>, and this makes the
/// published document agree with it.
/// </para>
/// </summary>
internal sealed class IntegerSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (schema.Type is { } type
            && type.HasFlag(JsonSchemaType.Integer)
            && type.HasFlag(JsonSchemaType.String))
        {
            schema.Type = type & ~JsonSchemaType.String;

            // The pattern only existed to describe the string form.
            schema.Pattern = null;
        }

        return Task.CompletedTask;
    }
}
