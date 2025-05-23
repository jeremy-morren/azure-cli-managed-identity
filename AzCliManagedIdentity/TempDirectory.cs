namespace AzCliManagedIdentity;

public sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.GetTempFileName();
        File.Delete(Path);
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, true);
        }
        catch (Exception ex)
        {
            // Ignore errors. Best thing we can do is print them to std err.
            Console.Error.WriteLine("Error deleting temp directory");
            Console.Error.WriteLine(ex);
        }
    }
}