using System.Text.Json;
using BR.Core;

namespace Activities.GigaChatCommunity;

[BR.Core.Attributes.ScreenName("Получить остаток токенов GigaChat")]
[BR.Core.Attributes.Representation("Получить остаток токенов GigaChat")]
[BR.Core.Attributes.Description("Возвращает доступный остаток токенов для выбранной модели.")]
[BR.Core.Attributes.Path(@"Custom activities.Модели.GigaChat")]
[BR.Core.Attributes.Image(typeof(GetBalanceByModel), "Activities.GigaChatCommunity.GigaChat.png")]
public class GetBalanceByModel : Activity
{
    [BR.Core.Attributes.ScreenName("Access token (SecureString):")]
    [BR.Core.Attributes.Description("Токен доступа GigaChat")]
    [BR.Core.Attributes.IsRequired]
    public required System.Security.SecureString AccessToken { get; set; }

    [BR.Core.Attributes.ScreenName("Модель:")]
    [BR.Core.Attributes.Description("Модель GigaChat")]
    public Models Model { get; set; } = Models.GigaChat;

    [BR.Core.Attributes.ScreenName("Остаток:")]
    [BR.Core.Attributes.Description("Доступный остаток токенов")]
    [BR.Core.Attributes.IsOut]
    public int Balance { get; set; }

    public override void Execute(int? optionID)
    {
        Balance = 0;
        JsonElement response = GigaChatHttp.GetJson(AccessToken, "balance");
        if (!response.TryGetProperty("balance", out JsonElement items) || items.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("В ответе GigaChat отсутствует массив balance.");

        string modelName = Model.ToString().Replace("_", "-", StringComparison.Ordinal);
        foreach (JsonElement item in items.EnumerateArray())
        {
            if (item.TryGetProperty("usage", out JsonElement usage)
                && string.Equals(usage.GetString(), modelName, StringComparison.OrdinalIgnoreCase)
                && item.TryGetProperty("value", out JsonElement value)
                && value.TryGetInt32(out int balance))
            {
                Balance = balance;
                return;
            }
        }

        throw new InvalidDataException($"Модель '{modelName}' отсутствует в ответе GigaChat.");
    }
}