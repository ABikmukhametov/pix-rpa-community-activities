using System.Text.Json;
using BR.Core;

namespace Activities.GigaChatCommunity;

[BR.Core.Attributes.ScreenName("Удалить файл из GigaChat")]
[BR.Core.Attributes.Representation("Удалить файл из GigaChat")]
[BR.Core.Attributes.Description("Удаляет файл по идентификатору.")]
[BR.Core.Attributes.Path(@"Custom activities.Модели.GigaChat")]
[BR.Core.Attributes.Image(typeof(DeleteFileById), "Activities.GigaChatCommunity.GigaChat.png")]
public class DeleteFileById : Activity
{
    [BR.Core.Attributes.ScreenName("Access token (SecureString):")]
    [BR.Core.Attributes.Description("Токен доступа GigaChat")]
    [BR.Core.Attributes.IsRequired]
    public required System.Security.SecureString AccessToken { get; set; }

    [BR.Core.Attributes.ScreenName("ID файла:")]
    [BR.Core.Attributes.Description("Идентификатор файла")]
    [BR.Core.Attributes.IsRequired]
    public required string FileId { get; set; }

    [BR.Core.Attributes.ScreenName("Результат:")]
    [BR.Core.Attributes.Description("JSON-ответ с результатом удаления")]
    [BR.Core.Attributes.IsOut]
    public JsonElement Response { get; set; }

    public override void Execute(int? optionID)
    {
        if (string.IsNullOrWhiteSpace(FileId))
            throw new ArgumentException("Идентификатор файла не указан.", nameof(FileId));
        Response = GigaChatHttp.PostJson(
            AccessToken,
            $"files/{Uri.EscapeDataString(FileId.Trim())}/delete",
            null);
    }
}