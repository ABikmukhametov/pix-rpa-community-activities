using NetOffice.OutlookApi;

namespace Activities.OutlookCommunity;

[BR.Core.Attributes.ScreenName("Получить письма Outlook с фильтрами")]
[BR.Core.Attributes.Representation("Поиск писем в Outlook по заданным критериям")]
[BR.Core.Attributes.Description("Ищет письма в явно выбранных аккаунте и папке Outlook.")]
[BR.Core.Attributes.Path(@"Custom activities.Outlook")]
[BR.Core.Attributes.Image(typeof(GetOutlookMails), "Activities.OutlookCommunity.Icon.png")]
public class GetOutlookMails : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Почтовый аккаунт:")]
    [BR.Core.Attributes.Description("Точное имя, UserName или SMTP-адрес аккаунта")]
    [BR.Core.Attributes.IsRequired]
    public required string Account { get; set; }

    [BR.Core.Attributes.ScreenName("Целевая папка:")]
    [BR.Core.Attributes.Description("Полный путь папки из активности «Получить каталоги Outlook»")]
    [BR.Core.Attributes.IsRequired]
    public required string Folder { get; set; }

    [BR.Core.Attributes.ScreenName("Только непрочитанные:")]
    [BR.Core.Attributes.Description("Искать только непрочитанные письма")]
    public bool OnlyUnread { get; set; } = true;

    [BR.Core.Attributes.ScreenName("Пометить прочитанными:")]
    [BR.Core.Attributes.Description("Пометить найденные письма как прочитанные")]
    public bool MarkAsRead { get; set; }

    [BR.Core.Attributes.ScreenName("Фильтр DASL:")]
    [BR.Core.Attributes.Description("Необязательный DASL-фильтр Outlook")]
    public string? Filter { get; set; }

    [BR.Core.Attributes.ScreenName("Сначала новые:")]
    [BR.Core.Attributes.Description("Сортировать по убыванию времени получения")]
    public bool SortNewestFirst { get; set; } = true;

    [BR.Core.Attributes.ScreenName("Результат поиска:")]
    [BR.Core.Attributes.Description("Список найденных MailItem")]
    [BR.Core.Attributes.IsOut]
    public required List<MailItem> Mails { get; set; }

    public override void Execute(int? optionID)
    {
        Mails = [];
        if (string.IsNullOrWhiteSpace(Account))
            throw new ArgumentException("Почтовый аккаунт не указан.", nameof(Account));
        if (string.IsNullOrWhiteSpace(Folder))
            throw new ArgumentException("Путь папки не указан.", nameof(Folder));

        var outlook = new Application();
        MAPIFolder targetFolder = GetTargetFolder(outlook);
        string combinedFilter = BuildCombinedFilter();

        _Items items = string.IsNullOrWhiteSpace(combinedFilter)
            ? targetFolder.Items
            : targetFolder.Items.Restrict(combinedFilter);
        items.Sort("[ReceivedTime]", SortNewestFirst);

        Mails = ProcessMailItems(items);
        if (MarkAsRead)
            MarkEmailsAsRead(Mails);
    }

    private MAPIFolder GetTargetFolder(Application outlook)
    {
        foreach (Account account in outlook.Session.Accounts)
        {
            if (!MatchesAccount(account, Account))
                continue;

            Store store = account.DeliveryStore
                ?? throw new InvalidOperationException($"У аккаунта '{Account}' отсутствует хранилище.");
            MAPIFolder root = store.GetRootFolder();
            return FindFolderByPath(root, Folder);
        }

        throw new InvalidOperationException($"Аккаунт Outlook '{Account}' не найден.");
    }

    private static bool MatchesAccount(Account account, string expected) =>
        string.Equals(account.DisplayName, expected, StringComparison.OrdinalIgnoreCase)
        || string.Equals(account.UserName, expected, StringComparison.OrdinalIgnoreCase)
        || string.Equals(account.SmtpAddress, expected, StringComparison.OrdinalIgnoreCase);

    private static MAPIFolder FindFolderByPath(MAPIFolder root, string requestedPath)
    {
        string[] segments = requestedPath
            .Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
            throw new ArgumentException("Путь папки пуст.", nameof(requestedPath));

        int index = string.Equals(segments[0], root.Name, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        MAPIFolder current = root;
        for (; index < segments.Length; index++)
        {
            MAPIFolder? next = null;
            foreach (MAPIFolder child in current.Folders)
            {
                if (string.Equals(child.Name, segments[index], StringComparison.OrdinalIgnoreCase))
                {
                    next = child;
                    break;
                }
                child.Dispose();
            }

            current = next
                ?? throw new InvalidOperationException($"Папка Outlook '{requestedPath}' не найдена.");
        }

        return current;
    }

    private string BuildCombinedFilter()
    {
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(Filter))
            filters.Add($"({Filter.Trim()})");
        else
            filters.Add(@"@SQL=""http://schemas.microsoft.com/mapi/proptag/0x001A001F"" = 'IPM.Note'");

        if (OnlyUnread)
            filters.Add(@"""urn:schemas:httpmail:read"" = 0");

        return string.Join(" AND ", filters);
    }

    private static List<MailItem> ProcessMailItems(_Items items)
    {
        var result = new List<MailItem>();
        foreach (MailItem item in items.OfType<MailItem>())
        {
            if (string.Equals(item.MessageClass, "IPM.Note", StringComparison.OrdinalIgnoreCase))
                result.Add(item);
            else
                item.Dispose();
        }
        return result;
    }

    private static void MarkEmailsAsRead(IEnumerable<MailItem> mails)
    {
        foreach (MailItem mail in mails)
        {
            if (!mail.UnRead)
                continue;
            mail.UnRead = false;
            mail.Save();
        }
    }
}
