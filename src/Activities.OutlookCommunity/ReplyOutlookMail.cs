using NetOffice.OutlookApi;
using NetOffice.OutlookApi.Enums;

namespace Activities.OutlookCommunity;

[BR.Core.Attributes.ScreenName("Ответить на письмо Outlook")]
[BR.Core.Attributes.Representation("Ответить на письмо Outlook")]
[BR.Core.Attributes.Description("Создает ответ, по умолчанию сохраняет его как черновик.")]
[BR.Core.Attributes.Path(@"Custom activities.Outlook")]
[BR.Core.Attributes.Image(typeof(ReplyToOutlookMail), "Activities.OutlookCommunity.Icon.png")]
public class ReplyToOutlookMail : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Письмо:")]
    [BR.Core.Attributes.Description("Исходное письмо")]
    [BR.Core.Attributes.IsRequired]
    public required MailItem Mail { get; set; }

    [BR.Core.Attributes.ScreenName("Текст ответа:")]
    [BR.Core.Attributes.Description("Содержимое ответа")]
    [BR.Core.Attributes.IsRequired]
    public required string Body { get; set; }

    [BR.Core.Attributes.ScreenName("Ответить всем:")]
    [BR.Core.Attributes.Description("True — ответить всем; False — только отправителю")]
    public bool ReplyAll { get; set; }

    [BR.Core.Attributes.ScreenName("Вложения:")]
    [BR.Core.Attributes.Description("Полные пути к дополнительным файлам")]
    public List<string>? Attachments { get; set; }

    [BR.Core.Attributes.ScreenName("HTML-формат:")]
    [BR.Core.Attributes.Description("Использовать HTMLBody")]
    public bool Html { get; set; }

    [BR.Core.Attributes.ScreenName("Оставить вложения:")]
    [BR.Core.Attributes.Description("Скопировать вложения исходного письма")]
    public bool KeepAttachments { get; set; }

    [BR.Core.Attributes.ScreenName("Сохранить как черновик:")]
    [BR.Core.Attributes.Description("True — сохранить черновик; False — отправить сразу")]
    public bool SaveAsDraft { get; set; } = true;

    [BR.Core.Attributes.ScreenName("Доп. получатели:")]
    [BR.Core.Attributes.Description("Дополнительные адреса в поле Кому")]
    public List<string>? AdditionalRecipients { get; set; }

    [BR.Core.Attributes.ScreenName("Доп. копия:")]
    [BR.Core.Attributes.Description("Дополнительные адреса в поле Копия")]
    public List<string>? AdditionalCcRecipients { get; set; }

    [BR.Core.Attributes.ScreenName("Доп. скрытая копия:")]
    [BR.Core.Attributes.Description("Дополнительные адреса в поле Скрытая копия")]
    public List<string>? AdditionalBccRecipients { get; set; }

    private string? _temporaryDirectory;

    public override void Execute(int? optionID)
    {
        ArgumentNullException.ThrowIfNull(Mail);
        ArgumentNullException.ThrowIfNull(Body);

        MailItem reply = ReplyAll ? Mail.ReplyAll() : Mail.Reply();
        try
        {
            if (Html)
                reply.HTMLBody = Body + reply.HTMLBody;
            else
                reply.Body = Body + reply.Body;

            if (KeepAttachments)
                CopyOriginalAttachments(Mail, reply);
            AddNewAttachments(reply);
            AddRecipients(reply, AdditionalRecipients, OlMailRecipientType.olTo);
            AddRecipients(reply, AdditionalCcRecipients, OlMailRecipientType.olCC);
            AddRecipients(reply, AdditionalBccRecipients, OlMailRecipientType.olBCC);

            if (SaveAsDraft)
                reply.Save();
            else
                reply.Send();
        }
        finally
        {
            reply.Dispose();
            TryDeleteTemporaryDirectory();
        }
    }

    private void CopyOriginalAttachments(MailItem source, MailItem target)
    {
        _temporaryDirectory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "PIX",
            "OutlookAttachments",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);

        foreach (Attachment sourceAttachment in source.Attachments)
        {
            try
            {
                string fileName = OutlookFileSafety.ValidateFileName(sourceAttachment.FileName);
                string temporaryPath = OutlookFileSafety.UniquePath(_temporaryDirectory, fileName);
                sourceAttachment.SaveAsFile(temporaryPath);
                Attachment added = target.Attachments.Add(temporaryPath);
                added.Dispose();
            }
            finally
            {
                sourceAttachment.Dispose();
            }
        }
    }

    private void AddNewAttachments(MailItem mail)
    {
        if (Attachments is null)
            return;

        foreach (string path in Attachments)
        {
            string fullPath = System.IO.Path.GetFullPath(
                string.IsNullOrWhiteSpace(path)
                    ? throw new ArgumentException("Список вложений содержит пустой путь.")
                    : path);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Вложение не найдено.", fullPath);
            Attachment added = mail.Attachments.Add(fullPath);
            added.Dispose();
        }
    }

    private static void AddRecipients(
        MailItem mail,
        IEnumerable<string>? recipients,
        OlMailRecipientType recipientType)
    {
        if (recipients is null)
            return;

        foreach (string address in recipients)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Список получателей содержит пустой адрес.");

            Recipient recipient = mail.Recipients.Add(address.Trim());
            try
            {
                recipient.Type = (int)recipientType;
                recipient.Resolve();
                if (!recipient.Resolved)
                    throw new InvalidOperationException($"Outlook не смог разрешить получателя '{address}'.");
            }
            finally
            {
                recipient.Dispose();
            }
        }
    }

    private void TryDeleteTemporaryDirectory()
    {
        if (string.IsNullOrWhiteSpace(_temporaryDirectory) || !Directory.Exists(_temporaryDirectory))
            return;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Directory.Delete(_temporaryDirectory, true);
                return;
            }
            catch when (attempt < 2)
            {
                Thread.Sleep(200);
            }
            catch
            {
                return;
            }
        }
    }
}