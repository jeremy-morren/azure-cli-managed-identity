// using System.Net;
// using Shouldly;
//
// namespace AzCliManagedIdentity.Tests;
//
// public class TokenRequestTests
// {
//     [Theory]
//     [MemberData(nameof(GetValidRequests))]
//     public void CreateValidRequest(string method,
//         string requestUrl,
//         string headers,
//         DateOnly expectedApiVersion,
//         string expectedResource,
//         string? expectedObjectId,
//         string? expectedClientId,
//         string? expectedAzureResourceId)
//     {
//         TokenRequest.TryCreateDefaultRequest(method, requestUrl, ParseHeaders(headers), out var request, out var error).ShouldBeTrue();
//         error.ShouldBeNull();
//         request.ShouldNotBeNull();
//         request.ApiVersion.ShouldBe(expectedApiVersion);
//         request.Resource.ShouldBe(expectedResource);
//         request.ObjectId.ShouldBe(expectedObjectId);
//         request.ClientId.ShouldBe(expectedClientId);
//         request.AzureResourceId.ShouldBe(expectedAzureResourceId);
//     }
//
//     /// <summary>
//     /// Valid requests
//     /// </summary>
//     public static TheoryData<string, string, string, DateOnly, string, string?, string?, string?> GetValidRequests() => new()
//     {
//         {
//             "get",
//             "/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://management.azure.com/.default",
//             "Metadata: true",
//             new DateOnly(2018, 2, 1),
//             "https://management.azure.com/.default",
//             null, null, null
//         },
//         {
//             "GET",
//             "/metadata/identity/oauth2/token?api-version=2020-01-11&resource=api%3A%2F%2FclientId%2FApi&object_id=1",
//             "A-Header: \nMetadata: true",
//             new DateOnly(2020, 1, 11),
//             "api://clientId/Api",
//             "1",
//             null, null
//         },
//         {
//             "GET",
//             "/metadata/identity/oauth2/token?api-version=2055-05-21&resource=https%3A%2F%2Fgraph.microsoft.com&client_id=1&msi_res_id=aaa",
//             "METADATA: true",
//             new DateOnly(2055, 5, 21),
//             "https://graph.microsoft.com",
//             null,
//             "1",
//             "aaa"
//         }
//     };
//
//     [Theory]
//     [MemberData(nameof(GetInvalidRequests))]
//     public void CreateInvalidRequest(string method, string requestUrl, string headers, HttpStatusCode expectedError)
//     {
//         TokenRequest.TryCreateDefaultRequest(method, requestUrl, ParseHeaders(headers), out _, out var error).ShouldBeFalse();
//         error.ShouldBe(expectedError);
//     }
//
//     /// <summary>
//     /// Valid requests
//     /// </summary>
//     /// <returns></returns>
//     public static TheoryData<string, string, string, HttpStatusCode> GetInvalidRequests() =>
//         new()
//         {
//             // Version too early
//             {
//                 "get",
//                 "/metadata/identity/oauth2/token?api-version=2016-02-01&resource=resource",
//                 "Metadata: true",
//                 HttpStatusCode.BadRequest
//             },
//             // Metadata header missing
//             {
//                 "get",
//                 "/metadata/identity/oauth2/token?api-version=2018-02-01&resource=resource",
//                 "RandomHeader: true",
//                 HttpStatusCode.BadRequest
//             },
//             // Metadata header is not 'true'
//             {
//                 "get",
//                 "/metadata/identity/oauth2/token?api-version=2018-02-01&resource=resource",
//                 "Metadata: tRUE",
//                 HttpStatusCode.BadRequest
//             },
//             // Wrong method
//             {
//                 "POST",
//                 "/metadata/identity/oauth2/token?api-version=2018-02-01&resource=resource",
//                 "Metadata: true",
//                 HttpStatusCode.NotFound
//             },
//             // Wrong URL
//             {
//                 "GET",
//                 "/metadata/identity/oauth2?api-version=2018-02-01&resource=resource",
//                 "Metadata: true",
//                 HttpStatusCode.NotFound
//             },
//         };
//
//     private static Dictionary<string, string?> ParseHeaders(string headers) =>
//         headers
//             .Split('\n')
//             .Select(h => h.Split(": "))
//             .ToDictionary(p => p[0], string? (p) => p[1], StringComparer.OrdinalIgnoreCase);
// }