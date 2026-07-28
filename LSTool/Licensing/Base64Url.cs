using System;
using System.Text;

namespace LSTool.Licensing
{
    internal static class Base64Url
    {
        public static string Encode(byte[] value)
        {
            return Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static byte[] Decode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new FormatException("Base64Url value is empty.");
            }

            string padded = value.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 0:
                    break;
                case 2:
                    padded += "==";
                    break;
                case 3:
                    padded += "=";
                    break;
                default:
                    throw new FormatException("Base64Url value has an invalid length.");
            }

            return Convert.FromBase64String(padded);
        }

        public static string DecodeUtf8(string value)
        {
            return Encoding.UTF8.GetString(Decode(value));
        }
    }
}
