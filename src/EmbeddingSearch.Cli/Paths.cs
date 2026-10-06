namespace EmbeddingSearch.Cli;

public static class Paths
{
    /// <summary>
    /// "data/..." gibi depo köküne göre verilen bir yolu bulur. Uygulama bin/Debug/... altından
    /// çalıştığında da örnek veriyi bulabilmek için üst klasörlere doğru arar.
    /// </summary>
    public static string Resolve(string relativePath)
    {
        if (Path.Exists(relativePath))
            return relativePath;

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (Path.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"'{relativePath}' bulunamadı.");
    }
}
