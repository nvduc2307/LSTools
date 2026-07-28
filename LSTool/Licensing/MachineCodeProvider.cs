using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace LSTool.Licensing
{
    public static class MachineCodeProvider
    {
        private const string ProductSalt = "LSTools.DeviceHash.v1";

        public static string GetMachineCode()
        {
            string machineIdentity = ReadWindowsMachineGuid();
            if (string.IsNullOrWhiteSpace(machineIdentity))
            {
                machineIdentity =
                    Environment.MachineName + "|" +
                    Environment.OSVersion.VersionString;
            }

            byte[] input = Encoding.UTF8.GetBytes(
                ProductSalt + "|" + machineIdentity.Trim());
            byte[] digest;
            using (SHA256 sha256 = SHA256.Create())
            {
                digest = sha256.ComputeHash(input);
            }

            string compact = ToHex(digest).Substring(0, 24);
            return AddGroups(compact);
        }

        private static string ReadWindowsMachineGuid()
        {
            try
            {
                using (RegistryKey localMachine = RegistryKey.OpenBaseKey(
                           RegistryHive.LocalMachine,
                           RegistryView.Registry64))
                using (RegistryKey? key = localMachine.OpenSubKey(
                           @"SOFTWARE\Microsoft\Cryptography",
                           false))
                {
                    return key?.GetValue("MachineGuid") as string ??
                           string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ToHex(byte[] value)
        {
            StringBuilder builder = new StringBuilder(value.Length * 2);
            foreach (byte item in value)
            {
                builder.Append(item.ToString("X2"));
            }

            return builder.ToString();
        }

        private static string AddGroups(string compact)
        {
            StringBuilder builder = new StringBuilder(compact.Length + 5);
            for (int index = 0; index < compact.Length; index++)
            {
                if (index > 0 && index % 4 == 0)
                {
                    builder.Append('-');
                }

                builder.Append(compact[index]);
            }

            return builder.ToString();
        }
    }
}
