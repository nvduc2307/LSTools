using System;
using Newtonsoft.Json;

namespace LSTool.Licensing
{
    public sealed class SignedLease
    {
        [JsonProperty("payload")]
        public string Payload { get; set; } = string.Empty;

        [JsonProperty("signature")]
        public string Signature { get; set; } = string.Empty;
    }

    public sealed class LeasePayload
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonProperty("licenseId")]
        public string LicenseId { get; set; } = string.Empty;

        [JsonProperty("customer")]
        public string Customer { get; set; } = string.Empty;

        [JsonProperty("product")]
        public string Product { get; set; } = string.Empty;

        [JsonProperty("deviceHash")]
        public string DeviceHash { get; set; } = string.Empty;

        [JsonProperty("issuedUtc")]
        public DateTimeOffset IssuedUtc { get; set; }

        [JsonProperty("expiresUtc")]
        public DateTimeOffset ExpiresUtc { get; set; }

        [JsonProperty("leaseExpiresUtc")]
        public DateTimeOffset LeaseExpiresUtc { get; set; }

        [JsonProperty("features")]
        public string[] Features { get; set; } = Array.Empty<string>();

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("nonce")]
        public string Nonce { get; set; } = string.Empty;
    }

    internal sealed class LicenseServerRequest
    {
        [JsonProperty("action")]
        public string Action { get; set; } = string.Empty;

        [JsonProperty("credential")]
        public string Credential { get; set; } = string.Empty;

        [JsonProperty("deviceHash")]
        public string DeviceHash { get; set; } = string.Empty;

        [JsonProperty("product")]
        public string Product { get; set; } = string.Empty;

        [JsonProperty("requestId")]
        public string RequestId { get; set; } = string.Empty;
    }

    internal sealed class LicenseServerResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("lease")]
        public SignedLease? Lease { get; set; }

        [JsonProperty("clientCredential")]
        public string ClientCredential { get; set; } = string.Empty;
    }

    internal sealed class LicenseClientState
    {
        [JsonProperty("clientCredential")]
        public string ClientCredential { get; set; } = string.Empty;

        [JsonProperty("lease")]
        public SignedLease? Lease { get; set; }
    }

    public enum LicenseValidationCode
    {
        Valid,
        Missing,
        ServerNotConfigured,
        NetworkError,
        ServerRejected,
        InvalidFormat,
        InvalidSignature,
        InvalidProduct,
        InvalidMachine,
        DeviceLimitReached,
        DeviceRevoked,
        NotYetValid,
        Expired,
        LeaseExpired,
        FeatureNotLicensed,
        Revoked
    }

    public sealed class LicenseValidationResult
    {
        private LicenseValidationResult(
            bool isValid,
            bool isOfflineGrace,
            LicenseValidationCode code,
            string message,
            LeasePayload? payload)
        {
            IsValid = isValid;
            IsOfflineGrace = isOfflineGrace;
            Code = code;
            Message = message;
            Payload = payload;
        }

        public bool IsValid { get; }
        public bool IsOfflineGrace { get; }
        public LicenseValidationCode Code { get; }
        public string Message { get; }
        public LeasePayload? Payload { get; }

        public static LicenseValidationResult Success(
            LeasePayload payload,
            bool isOfflineGrace = false,
            string message = "License hợp lệ.")
        {
            return new LicenseValidationResult(
                true,
                isOfflineGrace,
                LicenseValidationCode.Valid,
                message,
                payload);
        }

        public static LicenseValidationResult Failure(
            LicenseValidationCode code,
            string message,
            LeasePayload? payload = null)
        {
            return new LicenseValidationResult(
                false,
                false,
                code,
                message,
                payload);
        }
    }

    public sealed class LicenseActivationResult
    {
        private LicenseActivationResult(
            bool isSuccess,
            string message,
            LeasePayload? payload)
        {
            IsSuccess = isSuccess;
            Message = message;
            Payload = payload;
        }

        public bool IsSuccess { get; }
        public string Message { get; }
        public LeasePayload? Payload { get; }

        public static LicenseActivationResult Success(
            string message,
            LeasePayload payload)
        {
            return new LicenseActivationResult(true, message, payload);
        }

        public static LicenseActivationResult Failure(string message)
        {
            return new LicenseActivationResult(false, message, null);
        }
    }
}
