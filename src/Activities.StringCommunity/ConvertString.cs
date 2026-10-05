using BR.Core;
using BR.Core.Attributes;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security;

namespace Activities.StringCommunity
{
    [BR.Core.Attributes.ScreenName("Конвертация SecureString и string")]
    [BR.Core.Attributes.Representation("Конвертация SecureString в string и наоборот. Рекомендуется ознакомиться с описанием опций!")]
    [BR.Core.Attributes.Description("Конвертация SecureString в string и наоборот. Рекомендуется ознакомиться с описанием опций!")]
    [BR.Core.Attributes.Path(@"Custom activities.Строки")]
    [BR.Core.Attributes.OptionList(new[] { "string --> SecureString", "SecureString --> string" })]
    [BR.Core.Attributes.Image(typeof(ConvertString), "Activities.StringCommunity.Icon.png")]
    public class ConvertString : BR.Core.Activity
    {
        [BR.Core.Attributes.Options(new[] { 0 })]
        [BR.Core.Attributes.ScreenName("Исходная строка:")]
        [BR.Core.Attributes.Description("Входная строка, которая будет конвертирована в SecureString\n" +
                                        "НЕБЕЗОПАСНО\n" +
                                        "- Видна в отладчике\n" +
                                        "- Сохраняется в памяти до сборки мусора\n" +
                                        "- Может быть считана злоумышленником")]
        [BR.Core.Attributes.IsRequired]
        public required string StringToConvert { get; set; }

        [BR.Core.Attributes.Options(new[] { 0 })]
        [BR.Core.Attributes.ScreenName("Результат (SecureString):")]
        [BR.Core.Attributes.Description("Представление SecureString только для чтения. Это снижает время хранения открытого текста, но не гарантирует защиту от чтения памяти процесса.")]
        [BR.Core.Attributes.IsOut]
        public required SecureString ConvertedSecureString { get; set; }

        [BR.Core.Attributes.Options(new[] { 1 })]
        [BR.Core.Attributes.ScreenName("Исходный SecureString:")]
        [BR.Core.Attributes.Description("Зашифрованные данные для преобразования.\n" +
                     "Преобразование в строку делает защиту бессмысленной!\n" +
                     "- Данные попадут в managed-кучу\n" +
                     "- Возможна утечка через дамп памяти")]
        [BR.Core.Attributes.IsRequired]
        public required SecureString SecureStringToConvert { get; set; }

        [BR.Core.Attributes.Options(new[] { 1 })]
        [BR.Core.Attributes.ScreenName("Результат (string):")]
        [BR.Core.Attributes.Description("Временная расшифрованная строка:\n" +
                     "- Видна в отладчике\n" +
                     "- Сохраняется в памяти до сборки мусора\n" +
                     "- Может быть считана злоумышленником")]
        [BR.Core.Attributes.IsOut]
        public required string ConvertedString { get; set; }

        public override void Execute(int? optionID)
        {
            ConvertedSecureString = null!;
            ConvertedString = string.Empty;
            switch (optionID)
            {
                case 0:
                    ConvertedSecureString = ConvertToSecureString(StringToConvert);
                    break;
                case 1:
                    ConvertedString = ConvertToUnsecureString(SecureStringToConvert);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(optionID), "Неизвестный режим конвертации.");
            }
        }

        private SecureString ConvertToSecureString(string input)
        {
            var secureString = new SecureString();
            foreach (char c in input)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();
            return secureString;
        }

        private string ConvertToUnsecureString(SecureString secureString)
        {
            return new System.Net.NetworkCredential(string.Empty, secureString).Password;
        }
    }
}
