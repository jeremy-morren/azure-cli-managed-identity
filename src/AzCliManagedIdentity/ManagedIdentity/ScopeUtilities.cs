using System.Text.RegularExpressions;

namespace AzCliManagedIdentity.ManagedIdentity;

public static partial class ScopeUtilities
{
    /// <summary>
    /// Checks whether the provided scope is valid.
    /// </summary>
    public static bool IsValidScope(string scope) =>
        !string.IsNullOrEmpty(scope) && ScopeRegex().IsMatch(scope);

    [GeneratedRegex("^[0-9a-zA-Z-_.:/]+$", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex ScopeRegex();
}