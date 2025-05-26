namespace AzCliManagedIdentity;

public static class EnvVariableHelpers
{
    public static string GetValue(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"Environment variable '{name}' is not set.");
        return value;
    }
}