namespace EmbeddingSearch.Cli;

/// <summary>
/// Bağımlılıksız, minimal argüman ayrıştırıcı: konumsal argümanlar + "--anahtar değer" seçenekleri.
/// Gerçek projelerde System.CommandLine veya Spectre.Console.Cli tercih edilebilir.
/// </summary>
public sealed class CliArgs
{
    private readonly Dictionary<string, string> _options = new(StringComparer.OrdinalIgnoreCase);

    public CliArgs(IEnumerable<string> args)
    {
        var list = args.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].StartsWith("--"))
            {
                string key = list[i][2..];
                bool hasValue = i + 1 < list.Count && !list[i + 1].StartsWith("--");
                _options[key] = hasValue ? list[++i] : "true";
            }
            else
            {
                Positional.Add(list[i]);
            }
        }
    }

    public List<string> Positional { get; } = new();

    public string Get(string key, string fallback) => _options.GetValueOrDefault(key, fallback);

    public int GetInt(string key, int fallback)
        => _options.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : fallback;

    public bool Has(string key) => _options.ContainsKey(key);
}
