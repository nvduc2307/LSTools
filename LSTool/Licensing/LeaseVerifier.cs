using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace LSTool.Licensing
{
    public static class LeaseVerifier
    {
        public const string ProductName = "LSTools";
        public const int CurrentSchemaVersion = 1;

        public static LicenseValidationResult Verify(
            SignedLease? lease,
            string expectedDeviceHash,
            string requiredFeature,
            DateTimeOffset utcNow)
        {
            return Verify(
                lease,
                expectedDeviceHash,
                requiredFeature,
                utcNow,
                LeasePublicKey.CreateParameters());
        }

        public static LicenseValidationResult Verify(
            SignedLease? lease,
            string expectedDeviceHash,
            string requiredFeature,
            DateTimeOffset utcNow,
            RSAParameters publicKey)
        {
            if (lease == null ||
                string.IsNullOrWhiteSpace(lease.Payload) ||
                string.IsNullOrWhiteSpace(lease.Signature))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.InvalidFormat,
                    "Token license thiếu payload hoặc chữ ký.");
            }

            byte[] payloadBytes;
            byte[] signatureBytes;
            try
            {
                payloadBytes = Base64Url.Decode(lease.Payload);
                signatureBytes = Base64Url.Decode(lease.Signature);
            }
            catch (FormatException)
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.InvalidFormat,
                    "Token license không đúng định dạng.");
            }

            bool signatureValid;
            try
            {
                using (RSA rsa = RSA.Create())
                {
                    rsa.ImportParameters(publicKey);
                    signatureValid = rsa.VerifyData(
                        payloadBytes,
                        signatureBytes,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);
                }
            }
            catch (CryptographicException)
            {
                signatureValid = false;
            }

            if (!signatureValid)
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.InvalidSignature,
                    "Chữ ký token license không hợp lệ.");
            }

            LeasePayload? payload;
            try
            {
                payload = JsonConvert.DeserializeObject<LeasePayload>(
                    Encoding.UTF8.GetString(payloadBytes));
            }
            catch (JsonException)
            {
                payload = null;
            }

            if (payload == null ||
                payload.SchemaVersion != CurrentSchemaVersion ||
                string.IsNullOrWhiteSpace(payload.LicenseId) ||
                string.IsNullOrWhiteSpace(payload.Customer) ||
                payload.ExpiresUtc <= payload.IssuedUtc ||
                payload.LeaseExpiresUtc <= payload.IssuedUtc ||
                payload.LeaseExpiresUtc > payload.ExpiresUtc)
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.InvalidFormat,
                    "Nội dung token license không hợp lệ.");
            }

            if (!string.Equals(
                    payload.Product,
                    ProductName,
                    StringComparison.Ordinal))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.InvalidProduct,
                    "Token này không dành cho LSTools.",
                    payload);
            }

            if (!string.Equals(
                    NormalizeDeviceHash(payload.DeviceHash),
                    NormalizeDeviceHash(expectedDeviceHash),
                    StringComparison.Ordinal))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.InvalidMachine,
                    "License đã được kích hoạt trên máy tính khác.",
                    payload);
            }

            if (!string.Equals(
                    payload.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.Revoked,
                    "License đã bị khóa.",
                    payload);
            }

            if (utcNow < payload.IssuedUtc.AddMinutes(-5))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.NotYetValid,
                    "Token license chưa đến thời điểm có hiệu lực.",
                    payload);
            }

            if (utcNow >= payload.ExpiresUtc)
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.Expired,
                    "License đã hết hạn.",
                    payload);
            }

            if (utcNow >= payload.LeaseExpiresUtc)
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.LeaseExpired,
                    "Token offline đã hết hạn; cần kết nối Internet để kiểm tra lại.",
                    payload);
            }

            if (!HasFeature(payload.Features, requiredFeature))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.FeatureNotLicensed,
                    "License không bao gồm tính năng này.",
                    payload);
            }

            return LicenseValidationResult.Success(payload);
        }

        private static bool HasFeature(
            IEnumerable<string>? licensedFeatures,
            string requiredFeature)
        {
            if (string.IsNullOrWhiteSpace(requiredFeature))
            {
                return true;
            }

            if (licensedFeatures == null)
            {
                return false;
            }

            foreach (string feature in licensedFeatures)
            {
                if (string.Equals(feature, "*", StringComparison.Ordinal) ||
                    string.Equals(
                        feature,
                        requiredFeature,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeDeviceHash(string value)
        {
            return (value ?? string.Empty)
                .Replace("-", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }
    }
}
