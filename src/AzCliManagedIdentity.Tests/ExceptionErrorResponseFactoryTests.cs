using AzCliManagedIdentity.Api;
using Azure.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzCliManagedIdentity.Tests;

[TestClass]
public class ExceptionErrorResponseFactoryTests
{
    [DataTestMethod]
    [DataRow(ErrorResponseFactory.InvalidScope, InvalidScopeMessage, true)]
    [DataRow(ErrorResponseFactory.InteractiveAuthenticationRequired, MultifactorAuthenticationExpiredMessage, false)]
    public void CreateOAuth2Error(string error, string exceptionMessage, bool isBadRequest)
    {
        var exceptions = new AuthenticationFailedException[]
        {
            new (exceptionMessage),
            new ("", new Exception(exceptionMessage)),
            new CredentialUnavailableException("",
                new AggregateException(
                    new Exception("Some other exception"),
                    new AuthenticationFailedException(exceptionMessage))),
            new CredentialUnavailableException("",
                new AggregateException(
                    new AuthenticationFailedException("Environment variables are not fully configured"),
                    new AuthenticationFailedException(exceptionMessage))),
        };
        foreach (var ex in exceptions)
        {
            var result = ExceptionErrorResponseFactory.OAuth2Error(ex);
            Assert.AreEqual(isBadRequest, result.BadRequest);
            Assert.AreEqual(error, result.Response.Error);
        }
    }

    /// <summary>
    /// Message returned when the scope is invalid
    /// </summary>
    private const string InvalidScopeMessage =
        """
        AADSTS500011: The resource principal named api://8047 was not found in the tenant named Tenant. This can happen if the application has not been installed by the administrator of the tenant or consented to by any user in the tenant. You might have sent your authentication request to the wrong tenant. Trace ID: dd7d190f-d219-48b1-98e2-b75c52634800 Correlation ID: 6a49dc30-b46f-41c1-88fc-0084b5d22fb3 Timestamp: 2025-10-20 02:34:44Z
        Run the command below to authenticate interactively; additional arguments may be added as needed:
        az logout
        az login --tenant "Tenant id" --scope "api://8047/.default"
        """;

    /// <summary>
    /// Message returned when the user needs to perform multi-factor authentication, but their session has expired and they need to re-authenticate
    /// </summary>
    private const string MultifactorAuthenticationExpiredMessage =
        """
        Azure CLI authentication failed due to an unknown error. See the troubleshooting guide for more information. https://aka.ms/azsdk/net/identity/azclicredential/troubleshoot ERROR: AADSTS50078: Presented multi-factor authentication has expired due to policies configured by your administrator, you must refresh your multi-factor authentication to access '278c3f7a-0513-4643-9b40-eb6e8ee3937e'. Trace ID: bfab3651-96f6-4811-9e37-6452c334d492 Correlation ID: 7cea4938-492c-40ff-8223-d6de9a7a60b4 Timestamp: 2026-02-20 12:11:59Z
        Run the command below to authenticate interactively; additional arguments may be added as needed:
        az logout
        az login --tenant "tenant-id" --scope "scope-name"
        """;
}