using System.Text.Json;
using System.Text.Json.Serialization;
using BR.Core;

namespace Activities.GigaChatCommunity;

public sealed class TokenCountResult
{
    [JsonPropertyName("object")]
    public string ObjectType { get; set; } = "tokens";

    [JsonPropertyName("tokens")]
    public int Tokens { get; set; }

    [JsonPropertyName("characters")]
    public int Characters { get; set; }
}

[BR.Core.Attributes.ScreenName("Подсчитать количество токенов в запросе GigaChat")]
[BR.Core.Attributes.Representation("Подсчитать количество токенов в запросе GigaChat")]
[BR.Core.Attributes.Description("Подсчитывает суммарное число токенов в списке строк.")]
[BR.Core.Attributes.Path(@"Custom activities.Модели.GigaChat")]
[BR.Core.Attributes.Image(typeof(GetTokensCountByModel), "Activities.GigaChatCommunity.GigaChat.png")]
public class GetTokensCountByModel : Activity
{
    [BR.Core.Attributes.ScreenName("Access token (SecureString):")]
    [BR.Core.Attributes.Description("Токен доступа GigaChat")]
    [BR.Core.Attributes.IsRequired]
    public required System.Security.SecureString AccessToken { get; set; }

    [BR.Core.Attributes.ScreenName("Модель:")]
    [BR.Core.Attributes.Description("Модель GigaChat")]
    public Models Model { get; set; } = Models.GigaChat;

    [BR.Core.Attributes.ScreenName("Список строк:")]
    [BR.Core.Attributes.Description("Строки для подсчета")]
    [BR.Core.Attributes.IsRequired]
    public required List<string> Input { get; set; }

    [BR.Core.Attributes.ScreenName("Количество токенов:")]
    [BR.Core.Attributes.Description("Суммарное количество токенов")]
    [BR.Core.Attributes.IsOut]
    public int TokensCount { get; set; }

    public override void Execute(int? optionID)
    {
        TokensCount = 0;
        if (Input is null || Input.Count == 0)
            throw new ArgumentException("Список строк пуст.", nameof(Input));
        if (Input.Any(value => value is null))
            throw new ArgumentException("Список строк содержит null.", nameof(Input));

        JsonElement response = GigaChatHttp.PostJson(
            AccessToken,
            "tokens/count",
            new
            {
                model = Model.ToString().Replace("_", "-", StringComparison.Ordinal),
                input = Input
            });

        if (response.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("GigaChat вернул неожиданный формат подсчета токенов.");

        foreach (JsonElement item in response.EnumerateArray())
        {
            if (!item.TryGetProperty("tokens", out JsonElement tokens) || !tokens.TryGetInt32(out int value))
                throw new InvalidDataException("В ответе GigaChat отсутствует поле tokens.");
            checked { TokensCount += value; }
        }
    }
}