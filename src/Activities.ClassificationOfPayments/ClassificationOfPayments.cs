using System.Globalization;

namespace Activities.ClassificationOfPayments;

[BR.Core.Attributes.ScreenName("Классификация платежа по назначению")]
[BR.Core.Attributes.Representation("Классификация с использованием JSON-конфигурации")]
[BR.Core.Attributes.Description("Определяет категорию по include/exclude регулярным выражениям с ограничением времени.")]
[BR.Core.Attributes.Path("Custom activities.Classification")]
[BR.Core.Attributes.Image(typeof(ClassificationOfPayments), "Activities.ClassificationOfPayments.Icon.png")]
public class ClassificationOfPayments : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Путь к config-файлу:")]
    [BR.Core.Attributes.Description("Путь к JSON-конфигурации категорий")]
    [BR.Core.Attributes.IsFilePathChooser]
    [BR.Core.Attributes.IsRequired]
    public required string ConfigPath { get; set; } = "categories.json";

    [BR.Core.Attributes.ScreenName("Назначение платежа:")]
    [BR.Core.Attributes.Description("Текст для классификации")]
    [BR.Core.Attributes.IsRequired]
    public required string PaymentPurpose { get; set; } = string.Empty;

    [BR.Core.Attributes.ScreenName("Предсказанный класс:")]
    [BR.Core.Attributes.IsOut]
    public string? PredictedClass { get; set; }

    [BR.Core.Attributes.ScreenName("Статистика:")]
    [BR.Core.Attributes.IsOut]
    public Dictionary<string, string> Stats { get; set; } = new();

    [BR.Core.Attributes.ScreenName("Логи:")]
    [BR.Core.Attributes.Description("Диагностика без текста платежа и локальных путей")]
    [BR.Core.Attributes.IsOut]
    public string Logs { get; set; } = string.Empty;

    public override void Execute(int? optionID)
    {
        PredictedClass = null;
        Stats = new Dictionary<string, string>();
        Logs = string.Empty;

        IReadOnlyList<PaymentCategory> categories = PaymentConfiguration.Load(ConfigPath);
        ClassificationResult result = new PaymentClassifier(categories).Classify(PaymentPurpose);
        PredictedClass = result.Category;
        Stats = result.Scores.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToString(CultureInfo.InvariantCulture));
        Logs = $"Проверено категорий: {categories.Count}. " +
               (result.Category == PaymentClassifier.Undefined
                   ? "Положительных совпадений нет."
                   : "Категория определена.");
    }
}
