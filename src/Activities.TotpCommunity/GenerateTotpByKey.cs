using BR.Core;
using BR.Core.Attributes;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security;

namespace Activities.TotpCommunity
{
    [BR.Core.Attributes.ScreenName("Генерация TOTP")]
    [BR.Core.Attributes.Representation("Генерация TOTP")]
    [BR.Core.Attributes.Description("Генерация Time-Based One-Time Password (TOTP)")]
    [BR.Core.Attributes.Path(@"Custom activities.TOTP")]
    [BR.Core.Attributes.Image(typeof(GenerateTotpByKey), "Activities.TotpCommunity.Icon.png")]
    public class GenerateTotpByKey : BR.Core.Activity
    {
        [BR.Core.Attributes.ScreenName("Секретный ключ (SecureString):")]
        [BR.Core.Attributes.Description("Секретный ключ в формате Base32 (без пробелов и в верхнем регистре)")]
        [BR.Core.Attributes.IsRequired]
        public required SecureString Key { get; set; }

        [BR.Core.Attributes.Options(new[] { 1 })]
        [BR.Core.Attributes.ScreenName("TOTP-код:")]
        [BR.Core.Attributes.Description("Сгенерированный 6-значный одноразовый пароль")]
        [BR.Core.Attributes.IsOut]
        public required string Code { get; set; }

        public override void Execute(int? optionID)
        {
            byte[] secretKey = Base32ToBytes(new System.Net.NetworkCredential(string.Empty, Key).Password);
            Code = GenerateTotp(secretKey);
        }

        static byte[] Base32ToBytes(string base32)
        {
            string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            base32 = base32.TrimEnd('=').ToUpper(); // Убираем `=` и приводим к верхнему регистру
            byte[] bytes = new byte[base32.Length * 5 / 8];
            int buffer = 0, bitsLeft = 0, index = 0;

            foreach (char c in base32)
            {
                int value = Base32Chars.IndexOf(c);
                if (value < 0) throw new ArgumentException("Invalid Base32 character");

                buffer <<= 5;
                buffer |= value;
                bitsLeft += 5;

                if (bitsLeft >= 8)
                {
                    bytes[index++] = (byte)(buffer >> (bitsLeft - 8));
                    bitsLeft -= 8;
                }
            }
            return bytes;
        }
        static string GenerateTotp(byte[] secretKey)
        {
            long timeStep = 30;
            int digits = 6;
            long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // Получаем текущее время в UNIX-формате
            long counter = unixTime / timeStep; // Разбиваем время на интервалы (по умолчанию 30 сек)

            byte[] counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes); // Приводим к Big Endian

            using (System.Security.Cryptography.HMACSHA1 hmac = new System.Security.Cryptography.HMACSHA1(secretKey)) // HMAC-SHA1
            {
                byte[] hash = hmac.ComputeHash(counterBytes);

                int offset = hash[^1] & 0x0F; // Последний байт & 0x0F -> 4 младших бита -> индекс
                int binary = ((hash[offset] & 0x7F) << 24) |
                             ((hash[offset + 1] & 0xFF) << 16) |
                             ((hash[offset + 2] & 0xFF) << 8) |
                             (hash[offset + 3] & 0xFF);

                int totp = binary % (int)Math.Pow(10, digits); // Берем 6 последних цифр

                // Преобразуем в строку и добавляем ведущие нули
                return totp.ToString().PadLeft(digits, '0');
            }
        }
    }
}
