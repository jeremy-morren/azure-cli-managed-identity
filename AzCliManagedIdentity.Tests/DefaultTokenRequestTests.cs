namespace AzCliManagedIdentity.Tests;

/// <summary>
/// Test parsing of the default token request (i.e. Virtual Machine, App Service, etc.)
/// </summary>
public class DefaultTokenRequestTests
{
    [Theory]
    [MemberData(nameof(GetValidRequests))]
    public void CreateValidRequest(CgiRequest cgiRequest,
        DateOnly expectedApiVersion,
        string expectedResource,
        string? expectedObjectId,
        string? expectedClientId,
        string? expectedAzureResourceId)
    {
        TokenRequest.TryCreateDefaultRequest(cgiRequest, out var request, out var error).ShouldBeTrue();
        error.ShouldBe(ErrorCode.None);
        request.ShouldNotBeNull();
        request.ApiVersion.ShouldBe(expectedApiVersion);
        request.Resource.ShouldBe(expectedResource);
        request.ObjectId.ShouldBe(expectedObjectId);
        request.ClientId.ShouldBe(expectedClientId);
        request.AzureResourceId.ShouldBe(expectedAzureResourceId);
    }

    /// <summary>
    /// Valid requests
    /// </summary>
    public static TheoryData<CgiRequest, DateOnly, string, string?, string?, string?> GetValidRequests() => new()
    {
        {
            CreateRequest(
                "get",
                "/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://management.azure.com/.default",
                "Metadata: true"),
            new DateOnly(2018, 2, 1),
            "https://management.azure.com/.default",
            null, null, null
        },
        {
            CreateRequest(
                "GET",
                "/metadata/identity/oauth2/token?api-version=2020-01-11&resource=api%3A%2F%2FclientId%2FApi&object_id=1",
                "A-Header: \nMetadata: true"),
            new DateOnly(2020, 1, 11),
            "api://clientId/Api",
            "1",
            null, null
        },
        {
            CreateRequest(
                "GET",
                "/metadata/identity/oauth2/token?api-version=2055-05-21&resource=https%3A%2F%2Fgraph.microsoft.com&client_id=1&msi_res_id=aaa",
                "METADATA: true"),
            new DateOnly(2055, 5, 21),
            "https://graph.microsoft.com",
            null,
            "1",
            "aaa"
        }
    };

    [Theory]
    [MemberData(nameof(GetInvalidRequests))]
    public void CreateInvalidRequest(CgiRequest cgiRequest, ErrorCode expectedError)
    {
        TokenRequest.TryCreateDefaultRequest(cgiRequest, out _, out var error).ShouldBeFalse();
        error.ShouldBe(expectedError);
    }

    /// <summary>
    /// Valid requests
    /// </summary>
    /// <returns></returns>
    public static TheoryData<CgiRequest, ErrorCode> GetInvalidRequests() =>
        new()
        {
            // Version too early
            {
                CreateRequest(
                    "get",
                    "/metadata/identity/oauth2/token?api-version=2016-02-01&resource=resource",
                    "Metadata: true"),
                ErrorCode.BadRequest
            },
            // No resource
            {
                CreateRequest(
                    "get",
                    "/METADATA/identity/oAuth2/token?api-version=2018-02-01",
                    "Metadata: true"),
                ErrorCode.ResourceNotSpecified
            },
            // Metadata header missing
            {
                CreateRequest(
                    "get",
                    "/METADATA/identity/oAuth2/token?api-version=2018-02-01&resource=resource",
                    "RandomHeader: true"),
                ErrorCode.MetadataHeaderMissing
            },
            // Metadata header is not 'true'
            {
                CreateRequest(
                    "get",
                    "/metadata/identity/oauth2/token?api-version=2018-02-01&resource=resource",
                    "Metadata: tRUE"),
                ErrorCode.MetadataHeaderMissing
            },
            // Wrong method
            {
                CreateRequest(
                    "POST",
                    "/metadata/identity/oauth2/token?api-version=2018-02-01&resource=resource",
                    "Metadata: true"),
                ErrorCode.None
            },
            // Wrong URL
            {
                CreateRequest(
                    "GET",
                    "/metadata/identity/oauth2?api-version=2018-02-01&resource=resource",
                    "Metadata: true"),
                ErrorCode.None
            },
        };

    public static CgiRequest CreateRequest(string method, string requestUrl, string headers) =>
        CloudShellTokenRequestTests.CreateRequest(method, requestUrl, headers, null);
}