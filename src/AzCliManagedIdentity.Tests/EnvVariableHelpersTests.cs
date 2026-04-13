using AzCliManagedIdentity.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzCliManagedIdentity.Tests;

[TestClass]
public class EnvVariableHelpersTests
{
    [TestMethod]
    public void GetEnvVariableShouldSucceed()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        Assert.IsNotNull(path);
        Assert.AreEqual(path, EnvVariableHelpers.GetValue("PATH"));
    }

    [DataTestMethod]
    [DataRow("NON_EXISTENT_ENV_VAR")]
    [DataRow("OTHER_NON_EXISTENT_ENV_VAR")]
    public void GetEnvVariableShouldReturnNullForNonExistentVariables(string varName)
    {
        Assert.ThrowsException<InvalidOperationException>(() => EnvVariableHelpers.GetValue(varName));
    }
}