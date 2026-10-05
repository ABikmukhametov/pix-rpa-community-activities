using System.Security;
using System.Text.Json;

namespace Activities.OpenAICommunity;

[BR.Core.Attributes.ScreenName("Получить список моделей")]
[BR.Core.Attributes.Representation("Получить список доступных моделей OpenAI")]
[BR.Core.Attributes.Description("Выполняет GET-запрос и возвращает идентификаторы моделей.")]
[BR.Core.Attributes.Image(typeof(GetModels), "Activities.OpenAICommunity.Icon.png")]
[BR.Core.Attributes.Path("Custom activities.Модели.OpenAI")]
public class GetModels : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Токен авторизации:")]
    [BR.Core.Attributes.Description("API-токен OpenAI или совместимого HTTPS-шлюза")]
    [BR.Core.Attributes.IsRequired]
    public required SecureString Token { get; set; }

    [BR.Core.Attributes.ScreenName("Endpoint:")]
    [BR.Core.Attributes.Description("HTTPS endpoint списка моделей")]
    [BR.Core.Attributes.IsRequired]
    public required string Endpoint { get; set; } = "https://api.openai.com/v1/models";

    [BR.Core.Attributes.ScreenName("JSON-ответ:")]
    [BR.Core.Attributes.Description("JSON-ответ API")]
    [BR.Core.Attributes.IsOut]
    public JsonElement JsonData { get; set; }

    [BR.Core.Attributes.ScreenName("Список моделей:")]
    [BR.Core.Attributes.Description("Идентификаторы моделей")]
    [BR.Core.Attributes.IsOut]
    public List<string> ModelList { get; set; } = new();

    public override void Execute(int? optionID)
    {
        JsonData = default;
        ModelList = new List<string>();
        using JsonDocument document = OpenAiHttp.Send(HttpMethod.Get, Endpoint, Token);
        JsonData = document.RootElement.Clone();

        if (!document.RootElement.TryGetProperty("data", out JsonElement data) ||
            data.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("Ответ OpenAI не содержит массив data.");
        }

        foreach (JsonElement model in data.EnumerateArray())
        {
            if (model.TryGetProperty("id", out JsonElement id) &&
                id.ValueKind == JsonValueKind.String &&
                id.GetString() is string value)
            {
                ModelList.Add(value);
            }
        }
    }
}
