namespace AzCliManagedIdentity;

public static class EnvVariableHelpers
{
    /// <summary>
    /// Gets the value of an environment variable or throws an exception if it is not set.
    /// </summary>
    public static string GetValue(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"Environment variable '{name}' not set.");
        return value;
    }
}