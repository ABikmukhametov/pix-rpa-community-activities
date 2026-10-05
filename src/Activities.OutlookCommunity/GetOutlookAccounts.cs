using NetOffice.OutlookApi;

namespace Activities.OutlookCommunity;

[BR.Core.Attributes.ScreenName("Получение учетных записей Outlook")]
[BR.Core.Attributes.Representation("Возвращает список всех почтовых ящиков Outlook")]
[BR.Core.Attributes.Description("Возвращает уникальные имена аккаунтов и корневых хранилищ Outlook.")]
[BR.Core.Attributes.Path(@"Custom activities.Outlook")]
[BR.Core.Attributes.Image(typeof(GetOutlookAccounts), "Activities.OutlookCommunity.Icon.png")]
public class GetOutlookAccounts : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Список аккаунтов")]
    [BR.Core.Attributes.Description("Уникальные имена аккаунтов и хранилищ")]
    [BR.Core.Attributes.IsOut]
    public required List<string> Accounts { get; set; }

    public override void Execute(int? optionID)
    {
        Accounts = [];
        using var outlook = new Application();
        using var session = outlook.GetNamespace("MAPI");

        foreach (Account account in session.Accounts)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(account.DisplayName))
                    Accounts.Add(account.DisplayName);
            }
            finally
            {
                account.Dispose();
            }
        }

        foreach (Store store in session.Stores)
        {
            try
            {
                using MAPIFolder rootFolder = store.GetRootFolder();
                if (!string.IsNullOrWhiteSpace(rootFolder.Name))
                    Accounts.Add(rootFolder.Name);
            }
            finally
            {
                store.Dispose();
            }
        }

        Accounts = Accounts
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}