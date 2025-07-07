namespace AzCliManagedIdentity;

public static class CopyHelpers
{
    public static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            var destFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            var destDir = Path.Combine(destination, Path.GetFileName(dir));
            CopyDirectory(dir, destDir);
        }
    }

    public static void CopyFile(string source, string destination)
    {
        var destFile = Path.Combine(destination, Path.GetFileName(source));
        File.Copy(source, destFile, true);
    }
}