using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AzCliManagedIdentity.Tests;

/// <summary>
/// Test parsing of the Cloud Shell token request (i.e. FORM POST)
/// </summary>
public class CloudShellTokenRequestTests
{
    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void CreateValidRequest(CgiRequest cgiRequest, string expectedResource)
    {
        TokenRequest.TryCreateCloudShellRequest(cgiRequest, out var request, out var error).ShouldBeTrue();
        error.ShouldBe(ErrorCode.None);
        request.ShouldNotBeNull();
        request.Resource.ShouldBe(expectedResource);
    }
    
    public static TheoryData<CgiRequest, string> ValidRequests() => new()
    {
        {
            CreateRequest(
                "POST",
                "/oauth2/token",
                "Metadata: true",
                "application/x-www-form-urlencoded",
                "resource=A Resource"),
            "A Resource"
        },
        {
            CreateRequest(
                "Post",
                "/OAUTH2/Token",
                "METADATA: true",
                "APPLICATION/x-www-form-urlencoded",
                "resource=scope&otherParam=5"),
            "scope"
        },
    };
    
    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void CreateInvalidRequest(CgiRequest cgiRequest, ErrorCode expectedError)
    {
        TokenRequest.TryCreateCloudShellRequest(cgiRequest, out var request, out var error).ShouldBeFalse();
        error.ShouldBe(expectedError);
        request.ShouldBeNull();
    }

    public static TheoryData<CgiRequest, ErrorCode> InvalidRequests() => new()
    {
        {
            // Not found
            CreateRequest(
                "GET",
                "/oauth2/token",
                "Metadata: true",
                "application/x-www-form-urlencoded",
                "resource=A%20Resource"),
            ErrorCode.None
        },
        {
            CreateRequest(
                "Post",
                "/OAUTH2/Token",
                "METADATA: false",
                "APPLICATION/x-www-form-urlencoded",
                "resource=scope&otherParam=5"),
            ErrorCode.MetadataHeaderMissing
        },
        {
            CreateRequest(
                "Post",
                "/OAUTH2/Token",
                "METADATA: true",
                "APPLICATION/x-www-form-urlencoded",
                "otherParam="),
            ErrorCode.ResourceNotSpecified
        },
    };


    public static CgiRequest CreateRequest(string method, string requestUrl, string headers, string? contentType = null, string? body = null) =>
        new()
        {
            Method = HttpMethod.Parse(method),
            RequestUri = new Uri(new Uri("http://localhost", UriKind.Absolute), requestUrl),
            Headers = headers
                .Split('\n')
                .Select(h => h.Split(": "))
                .ToDictionary(p => p[0], string? (p) => p[1], StringComparer.OrdinalIgnoreCase),
            ContentType = contentType,
            Body = body != null ? Encoding.UTF8.GetBytes(body) : []
        };
}