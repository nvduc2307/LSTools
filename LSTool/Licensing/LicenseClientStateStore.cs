using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace LSTool.Licensing
{
    internal static class LicenseClientStateStore
    {
        public static string StateDirectory
        {
            get
            {
                string overrideDirectory =
                    Environment.GetEnvironmentVariable(
                        "LSTOOLS_STATE_DIRECTORY") ?? string.Empty;
                return !string.IsNullOrWhiteSpace(overrideDirectory)
                    ? overrideDirectory.Trim()
                    : Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData),
                        "LSTools");
            }
        }

        public static string StateFilePath =>
            Path.Combine(StateDirectory, "runtime-state.dat");

        private static string LegacyStateFilePath =>
            Path.Combine(StateDirectory, "license-client.json");

        private static readonly byte[] Entropy =
            Encoding.UTF8.GetBytes("LSTools.RuntimeState.v2");

        public static LicenseClientState? Load()
        {
            try
            {
                if (!File.Exists(StateFilePath))
                {
                    return null;
                }

                byte[] protectedBytes = File.ReadAllBytes(StateFilePath);
                byte[] plainBytes = ProtectedData.Unprotect(
                    protectedBytes,
                    Entropy,
                    DataProtectionScope.CurrentUser);
                return JsonConvert.DeserializeObject<LicenseClientState>(
                    Encoding.UTF8.GetString(plainBytes));
            }
            catch
            {
                return null;
            }
        }

        public static void Save(LicenseClientState state)
        {
            Directory.CreateDirectory(StateDirectory);
            string temporaryPath = StateFilePath + ".tmp";
            byte[] plainBytes = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(state, Formatting.None));
            byte[] protectedBytes = ProtectedData.Protect(
                plainBytes,
                Entropy,
                DataProtectionScope.CurrentUser);
            File.WriteAllBytes(
                temporaryPath,
                protectedBytes);

            if (File.Exists(StateFilePath))
            {
                File.Replace(temporaryPath, StateFilePath, null);
            }
            else
            {
                File.Move(temporaryPath, StateFilePath);
            }

            DeleteFileIfPresent(LegacyStateFilePath);
        }

        public static void Delete()
        {
            DeleteFileIfPresent(StateFilePath);
            DeleteFileIfPresent(LegacyStateFilePath);
        }

        private static void DeleteFileIfPresent(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // A later validation will retry the silent activation.
            }
        }
    }
}
