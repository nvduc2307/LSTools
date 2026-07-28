using System;
using System.Security.Cryptography;

namespace LSTool.Licensing
{
    internal static class LeasePublicKey
    {
        // Public half of the key used by Google Apps Script to sign short-lived
        // lease tokens. The private key belongs only in Apps Script Properties
        // and the developer's secure backup.
        private const string ModulusBase64 =
            "0uek5oS+NRxsg/NU1reBTdKLYpi4R0ICyi9FYl3c+U3a74zO6kFael0HqN1uQ1vx" +
            "ET0Yv2ezfy6+MNzaqOTZ7nNgO44vXiAJniam8PgB+mB6gNTqJJjJS4an/M6aY813" +
            "FakF6oqrHqK/cr5CTS/HErlZ/kZe5UoSzwRtQqcGgHe3XdfYYju9p8hpwWcbnafHu" +
            "WUMJFWClaAz0tc7a5GhowuNC8S3m+JKR5OxTqi/+8AZ5soL4TU+HmTrCELfPL9dp1" +
            "wYzbrwxqiqHmiwCwEgM4MweCERGcnIjgUjr2LtxfjFZhLhUSyC2gYTfS/GVNYlyp5" +
            "LcWAtggliUFzC6x5RfeuSbDpmbKmIxa5G3yTd+u/TrMzgfY24wn98jn6Dmga+XOz8" +
            "6C5j7ij3dY2FK54jC5B7LpD7jGoa1QnXlSHKNHp3TxYEBKYoC3miaayngtyzjxBOU" +
            "KnHYAXGWpSV8yG+SRWOcSt2Lihzk8+RLHMEn7GNQz9XxwTNLHufMjqktEJN";
        private const string ExponentBase64 = "AQAB";

        public static RSAParameters CreateParameters()
        {
            return new RSAParameters
            {
                Modulus = Convert.FromBase64String(ModulusBase64),
                Exponent = Convert.FromBase64String(ExponentBase64)
            };
        }
    }
}
