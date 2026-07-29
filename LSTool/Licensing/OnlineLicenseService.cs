using System;
using System.Threading.Tasks;

namespace LSTool.Licensing
{
    public static class OnlineLicenseService
    {
        private const double RefreshIntervalHours = 24;
        private static readonly object SyncRoot = new object();

        public static void BeginSilentActivation()
        {
            _ = Task.Run(
                () =>
                {
                    try
                    {
                        Validate(string.Empty);
                    }
                    catch
                    {
                        // A protected command will retry and show a generic
                        // availability message if confirmation still fails.
                    }
                });
        }

        public static LicenseValidationResult Validate(
            string requiredFeature)
        {
            lock (SyncRoot)
            {
                return ValidateCore(requiredFeature);
            }
        }

        private static LicenseValidationResult ValidateCore(
            string requiredFeature)
        {
            string deviceHash = MachineCodeProvider.GetMachineCode();
            LicenseClientState? state = LicenseClientStateStore.Load();

            if (state == null ||
                string.IsNullOrWhiteSpace(state.ClientCredential))
            {
                LicenseActivationResult activation =
                    ActivateFromBootstrapCore(deviceHash);
                if (!activation.IsSuccess)
                {
                    return LicenseValidationResult.Failure(
                        LicenseValidationCode.Missing,
                        activation.Message);
                }

                state = LicenseClientStateStore.Load();
            }

            if (state == null ||
                string.IsNullOrWhiteSpace(state.ClientCredential))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.Missing,
                    "Không thể khởi tạo phiên sử dụng.");
            }

            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            LicenseValidationResult cached = LeaseVerifier.Verify(
                state.Lease,
                deviceHash,
                requiredFeature,
                utcNow);

            if (cached.IsValid &&
                cached.Payload != null &&
                utcNow <
                cached.Payload.IssuedUtc.AddHours(RefreshIntervalHours))
            {
                return cached;
            }

            LicenseServerConfiguration configuration =
                LicenseServerConfiguration.Load();
            if (!configuration.IsConfigured)
            {
                return UseCachedOrFail(
                    cached,
                    LicenseValidationCode.ServerNotConfigured,
                    "Không thể xác nhận phiên sử dụng.");
            }

            LicenseServerCallResult online = new LicenseServerClient(
                    configuration.Endpoint)
                .SendAsync(
                    "validate",
                    state.ClientCredential,
                    deviceHash)
                .GetAwaiter()
                .GetResult();

            if (!online.IsReachable)
            {
                return UseCachedOrFail(
                    cached,
                    LicenseValidationCode.NetworkError,
                    online.ErrorMessage);
            }

            LicenseServerResponse response =
                online.Response ?? new LicenseServerResponse();
            if (!response.Success)
            {
                if (ShouldReissueCredential(response.Code))
                {
                    LicenseActivationResult activation =
                        ActivateFromBootstrapCore(deviceHash);
                    if (activation.IsSuccess)
                    {
                        LicenseClientState? refreshedState =
                            LicenseClientStateStore.Load();
                        return LeaseVerifier.Verify(
                            refreshedState?.Lease,
                            deviceHash,
                            requiredFeature,
                            DateTimeOffset.UtcNow);
                    }
                }

                return FromServerRejection(response);
            }

            LicenseValidationResult refreshed = LeaseVerifier.Verify(
                response.Lease,
                deviceHash,
                requiredFeature,
                DateTimeOffset.UtcNow);
            if (!refreshed.IsValid)
            {
                return refreshed;
            }

            state.Lease = response.Lease;
            LicenseClientStateStore.Save(state);
            return refreshed;
        }

        private static LicenseActivationResult ActivateFromBootstrapCore(
            string deviceHash)
        {
            string activationCredential =
                BootstrapCredentialProvider.GetCredential()
                    .Trim()
                    .ToUpperInvariant();
            if (activationCredential.Length < 20)
            {
                return LicenseActivationResult.Failure(
                    "Bản cài đặt chưa được cấu hình.");
            }

            LicenseServerConfiguration configuration =
                LicenseServerConfiguration.Load();
            if (!configuration.IsConfigured)
            {
                return LicenseActivationResult.Failure(
                    "Không thể xác nhận bản cài đặt.");
            }

            LicenseServerCallResult call = new LicenseServerClient(
                    configuration.Endpoint)
                .SendAsync(
                    "activate",
                    activationCredential,
                    deviceHash)
                .GetAwaiter()
                .GetResult();

            if (!call.IsReachable)
            {
                return LicenseActivationResult.Failure(call.ErrorMessage);
            }

            LicenseServerResponse response =
                call.Response ?? new LicenseServerResponse();
            if (!response.Success ||
                string.IsNullOrWhiteSpace(response.ClientCredential))
            {
                return LicenseActivationResult.Failure(
                    string.IsNullOrWhiteSpace(response.Message)
                        ? "Không thể xác nhận bản cài đặt."
                        : response.Message);
            }

            LicenseValidationResult verified = LeaseVerifier.Verify(
                response.Lease,
                deviceHash,
                string.Empty,
                DateTimeOffset.UtcNow);
            if (!verified.IsValid || verified.Payload == null)
            {
                return LicenseActivationResult.Failure(verified.Message);
            }

            LicenseClientStateStore.Save(
                new LicenseClientState
                {
                    ClientCredential = response.ClientCredential.Trim(),
                    Lease = response.Lease
                });

            return LicenseActivationResult.Success(
                "Bản cài đặt đã sẵn sàng.",
                verified.Payload);
        }

        private static LicenseValidationResult UseCachedOrFail(
            LicenseValidationResult cached,
            LicenseValidationCode failureCode,
            string failureMessage)
        {
            if (cached.IsValid && cached.Payload != null)
            {
                return LicenseValidationResult.Success(
                    cached.Payload,
                    true,
                    "Đang sử dụng thời gian ngoại tuyến cho phép.");
            }

            return LicenseValidationResult.Failure(
                failureCode,
                failureMessage);
        }

        private static bool ShouldReissueCredential(string code)
        {
            return string.Equals(
                       code,
                       "BAD_CREDENTIAL",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       code,
                       "NOT_FOUND",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       code,
                       "EXPIRED",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       code,
                       "REVOKED",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       code,
                       "INACTIVE",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       code,
                       "DEVICE_MISMATCH",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       code,
                       "DEVICE_REVOKED",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       code,
                       "NOT_ACTIVATED",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static LicenseValidationResult FromServerRejection(
            LicenseServerResponse response)
        {
            string code = response.Code ?? string.Empty;
            string message = string.IsNullOrWhiteSpace(response.Message)
                ? "Không thể xác nhận phiên sử dụng."
                : response.Message;

            if (string.Equals(
                    code,
                    "EXPIRED",
                    StringComparison.OrdinalIgnoreCase))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.Expired,
                    message);
            }

            if (string.Equals(
                    code,
                    "REVOKED",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    code,
                    "INACTIVE",
                    StringComparison.OrdinalIgnoreCase))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.Revoked,
                    message);
            }

            if (string.Equals(
                    code,
                    "DEVICE_MISMATCH",
                    StringComparison.OrdinalIgnoreCase))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.InvalidMachine,
                    message);
            }

            if (string.Equals(
                    code,
                    "DEVICE_LIMIT_REACHED",
                    StringComparison.OrdinalIgnoreCase))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.DeviceLimitReached,
                    message);
            }

            if (string.Equals(
                    code,
                    "DEVICE_REVOKED",
                    StringComparison.OrdinalIgnoreCase))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.DeviceRevoked,
                    message);
            }

            return LicenseValidationResult.Failure(
                LicenseValidationCode.ServerRejected,
                message);
        }
    }
}
