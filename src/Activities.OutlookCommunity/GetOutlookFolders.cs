using NetOffice.OutlookApi;

namespace Activities.OutlookCommunity;

[BR.Core.Attributes.ScreenName("Получить каталоги Outlook")]
[BR.Core.Attributes.Representation("Получить каталоги Outlook")]
[BR.Core.Attributes.Description("Возвращает полные пути папок выбранного аккаунта Outlook.")]
[BR.Core.Attributes.Path(@"Custom activities.Outlook")]
[BR.Core.Attributes.Image(typeof(GetOutlookFolders), "Activities.OutlookCommunity.Icon.png")]
public class GetOutlookFolders : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Почтовый аккаунт:")]
    [BR.Core.Attributes.Description("Точное имя, UserName или SMTP-адрес аккаунта")]
    [BR.Core.Attributes.IsRequired]
    public required string Account { get; set; }

    [BR.Core.Attributes.ScreenName("Каталоги:")]
    [BR.Core.Attributes.Description("Полные пути найденных каталогов")]
    [BR.Core.Attributes.IsOut]
    public required List<string> MailFolders { get; set; }

    public override void Execute(int? optionID)
    {
        MailFolders = [];
        if (string.IsNullOrWhiteSpace(Account))
            throw new ArgumentException("Почтовый аккаунт не указан.", nameof(Account));

        using var outlook = new Application();
        using var session = outlook.GetNamespace("MAPI");
        bool found = false;

        foreach (Account account in session.Accounts)
        {
            try
            {
                if (!MatchesAccount(account, Account))
                    continue;
                found = true;
                using Store store = account.DeliveryStore
                    ?? throw new InvalidOperationException($"У аккаунта '{Account}' отсутствует хранилище.");
                using MAPIFolder root = store.GetRootFolder();
                AddFolders(root, root.Name);
            }
            finally
            {
                account.Dispose();
            }
        }

        if (!found)
            throw new InvalidOperationException($"Аккаунт Outlook '{Account}' не найден.");
    }

    private static bool MatchesAccount(Account account, string expected) =>
        string.Equals(account.DisplayName, expected, StringComparison.OrdinalIgnoreCase)
        || string.Equals(account.UserName, expected, StringComparison.OrdinalIgnoreCase)
        || string.Equals(account.SmtpAddress, expected, StringComparison.OrdinalIgnoreCase);

    private void AddFolders(MAPIFolder folder, string currentPath)
    {
        MailFolders.Add(currentPath);
        foreach (MAPIFolder child in folder.Folders)
        {
            try
            {
                AddFolders(child, currentPath + "\\" + child.Name);
            }
            finally
            {
                child.Dispose();
            }
        }
    }
}
