using System.Text.Json;
using BR.Core;

namespace Activities.GigaChatCommunity;

[BR.Core.Attributes.ScreenName("Получить список доступных файлов в GigaChat")]
[BR.Core.Attributes.Representation("Получить список доступных файлов в GigaChat")]
[BR.Core.Attributes.Description("Возвращает JSON со списком доступных файлов.")]
[BR.Core.Attributes.Path(@"Custom activities.Модели.GigaChat")]
[BR.Core.Attributes.Image(typeof(GetAvailableFiles), "Activities.GigaChatCommunity.GigaChat.png")]
public class GetAvailableFiles : Activity
{
    [BR.Core.Attributes.ScreenName("Access token (SecureString):")]
    [BR.Core.Attributes.Description("Токен доступа GigaChat")]
    [BR.Core.Attributes.IsRequired]
    public required System.Security.SecureString AccessToken { get; set; }

    [BR.Core.Attributes.ScreenName("Результат:")]
    [BR.Core.Attributes.Description("JSON-ответ с информацией о файлах")]
    [BR.Core.Attributes.IsOut]
    public JsonElement Response { get; set; }

    public override void Execute(int? optionID)
    {
        Response = GigaChatHttp.GetJson(AccessToken, "files");
    }
}