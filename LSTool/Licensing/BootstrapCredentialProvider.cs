using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace LSTool.Licensing
{
    internal static class BootstrapCredentialProvider
    {
        private const string ResourceName =
            "LSTool.Resources.Settings.ReleaseProfile.dat";

        public static string GetCredential()
        {
            string environmentCredential =
                Environment.GetEnvironmentVariable(
                    "LSTOOLS_BOOTSTRAP_CREDENTIAL") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(environmentCredential))
            {
                return environmentCredential.Trim();
            }

            try
            {
                Assembly assembly =
                    typeof(BootstrapCredentialProvider).Assembly;
                using (Stream? stream =
                       assembly.GetManifestResourceStream(ResourceName))
                {
                    if (stream == null)
                    {
                        return string.Empty;
                    }

                    using (StreamReader reader = new StreamReader(
                               stream,
                               Encoding.UTF8,
                               true))
                    {
                        string encoded = reader.ReadToEnd().Trim();
                        return Encoding.UTF8.GetString(
                            Convert.FromBase64String(encoded));
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
