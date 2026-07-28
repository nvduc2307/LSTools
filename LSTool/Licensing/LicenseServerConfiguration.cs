using System;
using System.IO;
using Newtonsoft.Json;

namespace LSTool.Licensing
{
    internal sealed class LicenseServerConfiguration
    {
        private const string Placeholder = "REPLACE_WITH_DEPLOYMENT_ID";

        [JsonProperty("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        public bool IsConfigured =>
            Uri.TryCreate(Endpoint, UriKind.Absolute, out Uri? uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            Endpoint.IndexOf(
                Placeholder,
                StringComparison.OrdinalIgnoreCase) < 0;

        public static LicenseServerConfiguration Load()
        {
            string assemblyDirectory =
                Path.GetDirectoryName(
                    typeof(LicenseServerConfiguration).Assembly.Location) ??
                string.Empty;
            DeleteObsoleteConfiguration(assemblyDirectory);

            string environmentUrl =
                Environment.GetEnvironmentVariable(
                    "LSTOOLS_LICENSE_API_URL") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(environmentUrl))
            {
                return new LicenseServerConfiguration
                {
                    Endpoint = environmentUrl.Trim()
                };
            }

            string configPath = Path.Combine(
                assemblyDirectory,
                "Resources",
                "Settings",
                "ReleaseChannel.json");

            try
            {
                if (File.Exists(configPath))
                {
                    return JsonConvert.DeserializeObject<
                               LicenseServerConfiguration>(
                               File.ReadAllText(configPath)) ??
                           new LicenseServerConfiguration();
                }
            }
            catch
            {
                // The caller reports a clear configuration error.
            }

            return new LicenseServerConfiguration();
        }

        private static void DeleteObsoleteConfiguration(
            string assemblyDirectory)
        {
            try
            {
                string obsoletePath = Path.Combine(
                    assemblyDirectory,
                    "Resources",
                    "Settings",
                    "LicenseServer.json");
                if (File.Exists(obsoletePath))
                {
                    File.Delete(obsoletePath);
                }
            }
            catch
            {
                // Cleanup is best-effort and must never block LSTools startup.
            }
        }
    }
}
