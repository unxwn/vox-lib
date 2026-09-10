using System.Text.RegularExpressions;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// Comparing two responses for the tests that are about them being alike.
/// <para>
/// Every problem response carries its own trace identifier, so comparing raw
/// bodies would always differ and would say nothing at all. What has to match is
/// everything else: two responses differing only in a value generated per
/// request disclose nothing about the address that was submitted.
/// </para>
/// </summary>
internal static partial class Responses
{
    public static async Task<string> BodyAsync(HttpResponseMessage response) =>
        TraceId().Replace(await response.Content.ReadAsStringAsync(), "\"traceId\":\"*\"");

    [GeneratedRegex("\"traceId\":\"[^\"]*\"")]
    private static partial Regex TraceId();
}
