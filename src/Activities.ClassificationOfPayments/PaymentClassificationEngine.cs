using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Activities.ClassificationOfPayments;

internal static class PaymentConfiguration
{
    public static IReadOnlyList<PaymentCategory> Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Путь к конфигурации не указан.", nameof(path));
        }

        string fullPath = System.IO.Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Конфигурационный файл не найден.", fullPath);
        }

        List<PaymentCategoryConfig>? config;
        try
        {
            config = JsonSerializer.Deserialize<List<PaymentCategoryConfig>>(
                File.ReadAllText(fullPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = false });
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Конфигурационный JSON повреждён.", ex);
        }

        if (config is null || config.Count == 0)
        {
            throw new InvalidDataException("Конфигурация не содержит категорий.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var categories = new List<PaymentCategory>(config.Count);
        foreach (PaymentCategoryConfig? item in config)
        {
            string name = item?.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                throw new InvalidDataException("Категория без имени недопустима.");
            }
            if (!names.Add(name))
            {
                throw new InvalidDataException("Имена категорий должны быть уникальными.");
            }

            IReadOnlyList<WeightedPattern> include = Compile(item!.IncludePatterns, name, "include");
            IReadOnlyList<WeightedPattern> exclude = Compile(item.ExcludePatterns, name, "exclude");
            if (include.Count == 0)
            {
                throw new InvalidDataException($"Категория '{name}' не содержит include_patterns.");
            }
            categories.Add(new PaymentCategory(name, include, exclude));
        }
        return categories;
    }

    private static IReadOnlyList<WeightedPattern> Compile(
        List<PatternConfig>? patterns,
        string category,
        string group)
    {
        var result = new List<WeightedPattern>();
        foreach (PatternConfig? item in patterns ?? new List<PatternConfig>())
        {
            if (string.IsNullOrWhiteSpace(item?.Pattern))
            {
                throw new InvalidDataException($"Категория '{category}' содержит пустой {group}-шаблон.");
            }
            if (item.Weight <= 0)
            {
                throw new InvalidDataException($"Вес {group}-шаблона категории '{category}' должен быть положительным.");
            }

            try
            {
                result.Add(new WeightedPattern(
                    new Regex(
                        item.Pattern,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                        TimeSpan.FromSeconds(1)),
                    item.Weight));
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException($"Категория '{category}' содержит некорректный {group}-шаблон.", ex);
            }
        }
        return result;
    }
}

internal sealed class PaymentClassifier
{
    public const string Undefined = "undefined";
    private readonly IReadOnlyList<PaymentCategory> _categories;

    public PaymentClassifier(IReadOnlyList<PaymentCategory> categories)
    {
        _categories = categories;
    }

    public ClassificationResult Classify(string? paymentPurpose)
    {
        string text = paymentPurpose ?? string.Empty;
        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (PaymentCategory category in _categories)
            {
                int score = category.Include
                    .Where(pattern => pattern.Regex.IsMatch(text))
                    .Sum(pattern => pattern.Weight);
                score -= category.Exclude
                    .Where(pattern => pattern.Regex.IsMatch(text))
                    .Sum(pattern => pattern.Weight);
                scores.Add(category.Name, score);
            }
        }
        catch (RegexMatchTimeoutException ex)
        {
            throw new TimeoutException("Превышено время обработки регулярного выражения.", ex);
        }

        int bestScore = scores.Values.Max();
        if (bestScore <= 0)
        {
            return new ClassificationResult(Undefined, scores);
        }

        string[] winners = scores
            .Where(pair => pair.Value == bestScore)
            .Select(pair => pair.Key)
            .ToArray();
        if (winners.Length != 1)
        {
            throw new InvalidOperationException("Несколько категорий получили одинаковый максимальный вес.");
        }
        return new ClassificationResult(winners[0], scores);
    }
}

internal sealed record PaymentCategory(
    string Name,
    IReadOnlyList<WeightedPattern> Include,
    IReadOnlyList<WeightedPattern> Exclude);

internal sealed record WeightedPattern(Regex Regex, int Weight);

internal sealed record ClassificationResult(
    string Category,
    IReadOnlyDictionary<string, int> Scores);

internal sealed class PatternConfig
{
    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    [JsonPropertyName("weight")]
    public int Weight { get; set; }
}

internal sealed class PaymentCategoryConfig
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("include_patterns")]
    public List<PatternConfig>? IncludePatterns { get; set; }

    [JsonPropertyName("exclude_patterns")]
    public List<PatternConfig>? ExcludePatterns { get; set; }
}
