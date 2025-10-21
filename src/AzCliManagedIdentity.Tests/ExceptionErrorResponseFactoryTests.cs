using AzCliManagedIdentity.Api;
using Azure.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzCliManagedIdentity.Tests;

[TestClass]
public class ExceptionErrorResponseFactoryTests
{
    [DataTestMethod]
    [DataRow("invalid_scope", InvalidScopeMessage)]
    public void CreateOAuth2Error(string error, string exceptionMessage)
    {
        var exceptions = new AuthenticationFailedException[]
        {
            new (exceptionMessage),
            new ("", new Exception(exceptionMessage)),
            new CredentialUnavailableException("",
                new AggregateException(new Exception("Some other exception"),
                    new AuthenticationFailedException(exceptionMessage))),
        };
        foreach (var ex in exceptions)
        {
            var result = ExceptionErrorResponseFactory.OAuth2Error(ex);
            Assert.IsTrue(result.BadRequest);
            Assert.AreEqual(error, result.Response.Error);
        }
    }

    private const string InvalidScopeMessage = """
                                               AADSTS500011: The resource principal named api://8047 was not found in the tenant named Tenant. This can happen if the application has not been installed by the administrator of the tenant or consented to by any user in the tenant. You might have sent your authentication request to the wrong tenant. Trace ID: dd7d190f-d219-48b1-98e2-b75c52634800 Correlation ID: 6a49dc30-b46f-41c1-88fc-0084b5d22fb3 Timestamp: 2025-10-20 02:34:44Z
                                               Run the command below to authenticate interactively; additional arguments may be added as needed:
                                               az logout
                                               az login --tenant "Tenant id" --scope "api://8047/.default"
                                               """;
}