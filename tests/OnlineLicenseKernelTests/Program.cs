using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using LSTool.Licensing;
using Newtonsoft.Json;

internal static class Program
{
    private const string DeviceHash = "AAAA-BBBB-CCCC-DDDD-EEEE-FFFF";
    private static readonly DateTimeOffset Now =
        new(2030, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private const string ProductionPayload =
        "eyJzY2hlbWFWZXJzaW9uIjoxLCJsaWNlbnNlSWQiOiJJTlRFUk5BTC1PTkxJTkUtU0VMRi1URVNULTAwMSIsImN1c3RvbWVyIjoiSW50ZXJuYWwgT25saW5lIExlYXNlIFNlbGYgVGVzdCIsInByb2R1Y3QiOiJMU1Rvb2xzIiwiZGV2aWNlSGFzaCI6IkFBQUEtQkJCQi1DQ0NDLUREREQtRUVFRS1GRkZGIiwiaXNzdWVkVXRjIjoiMjAyNi0wNy0yOFQwMjoxNzo1MC41MTIzNTY4KzAwOjAwIiwiZXhwaXJlc1V0YyI6IjIwMzYtMDctMjVUMDI6MTc6NTAuNTEyMzU2OCswMDowMCIsImxlYXNlRXhwaXJlc1V0YyI6IjIwMzYtMDctMjVUMDI6MTc6NTAuNTEyMzU2OCswMDowMCIsImZlYXR1cmVzIjpbIkxpY2Vuc2VTZWxmVGVzdCJdLCJzdGF0dXMiOiJBY3RpdmUiLCJub25jZSI6ImE1Y2UwODRiNmJhMDQ0MzU4ZmFmMDg4YjFjZTE0ZmNkIn0";

    private const string ProductionSignature =
        "reshGDq2qAHgfIxjfb6ubQ96FTpLCmflIy8M3CRsQY0zb4EH8VWB3_ejUWHpEFZ2q2kNcJWSIDdekcofLgNcEmSofzg0XmNwuAMrY_HkTgtKF-QzwQ54fdSgJZ74KR9vwD-k7hQHY9i9DF_6PQUPDT_CWzIpU63jGRTgVRX34fXo_x_PzGnqCGvQ6MFWXlg3fTWSg3VePi0CWnl80eP7FocCTTsI43D5an7vyDSts_1YbOrQdbJIlEj0iuGGxJe8ibBn5Cwsjxw3gPg5sX3AFN-2c3pCrN0YtIYyT-iy8aWqOQe_A09Buc2CVPcxMpl0JkzuvF4EjUtnyM7jFjOESXTwxJuOvitZ2laGoHqpp3NY61TDRHOmPe9lYkvcT0fIq0At6FqT5Hy8lt7ElfIiONVdm1usWnFu7G4aTJQwwVLhm5xsuqjD2HShI7Kb2tkOfkjTtJcCoPCAV-XYADPi8bOePhM-DoLJE-cXSGKoyyOSmFvsnqSC1edATwrxZZG0";

    private static int Main(string[] args)
    {
        if (args.Any(
                item => string.Equals(
                    item,
                    "--integration",
                    StringComparison.OrdinalIgnoreCase)))
        {
            return RunSilentActivationIntegration();
        }

        int failures = 0;
        failures += Run(
            "embedded production public key verifies fixture",
            EmbeddedProductionKeyVerifiesFixture);
        failures += Run(
            "embedded bootstrap credential is available",
            EmbeddedBootstrapCredentialIsAvailable);
        failures += Run(
            "client state is protected with Windows DPAPI",
            ClientStateIsProtected);
        failures += Run("valid signed lease", ValidSignedLease);
        failures += Run("wildcard feature", WildcardFeature);
        failures += Run("normalized device hash", NormalizedDeviceHash);
        failures += Run("wrong device rejected", WrongDeviceRejected);
        failures += Run("missing feature rejected", MissingFeatureRejected);
        failures += Run("wrong product rejected", WrongProductRejected);
        failures += Run("revoked lease rejected", RevokedLeaseRejected);
        failures += Run("future lease rejected", FutureLeaseRejected);
        failures += Run("expired license rejected", ExpiredLicenseRejected);
        failures += Run("expired cached lease rejected", ExpiredLeaseRejected);
        failures += Run("invalid lifetime rejected", InvalidLifetimeRejected);
        failures += Run("payload tampering rejected", PayloadTamperingRejected);
        failures += Run("wrong signing key rejected", WrongSigningKeyRejected);
        failures += Run("malformed envelope rejected", MalformedEnvelopeRejected);

        Console.WriteLine();
        Console.WriteLine(
            failures == 0
                ? "All 17 online license kernel tests passed."
                : failures + " online license kernel test(s) failed.");
        return failures == 0 ? 0 : 1;
    }

    private static int RunSilentActivationIntegration()
    {
        string endpoint =
            Environment.GetEnvironmentVariable(
                "LSTOOLS_LICENSE_API_URL") ?? string.Empty;
        string deviceHash =
            Environment.GetEnvironmentVariable(
                "LSTOOLS_INTEGRATION_DEVICE") ??
            "A4E1-3660-EACB-F9C3-A600-D535";
        string bootstrapCredential =
            BootstrapCredentialProvider.GetCredential();

        if (string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(bootstrapCredential))
        {
            Console.WriteLine(
                "FAIL silent activation integration: missing configuration");
            return 1;
        }

        Stopwatch watch = Stopwatch.StartNew();
        LicenseServerCallResult activation =
            new LicenseServerClient(endpoint)
                .SendAsync(
                    "activate",
                    bootstrapCredential,
                    deviceHash)
                .GetAwaiter()
                .GetResult();
        long activationMilliseconds = watch.ElapsedMilliseconds;
        LicenseServerResponse? activationResponse = activation.Response;
        if (!activation.IsReachable ||
            activationResponse == null ||
            !activationResponse.Success ||
            string.IsNullOrWhiteSpace(
                activationResponse.ClientCredential))
        {
            Console.WriteLine(
                "FAIL silent activation integration: " +
                (activationResponse?.Code ?? activation.ErrorMessage));
            return 1;
        }

        watch.Restart();
        LicenseServerCallResult validation =
            new LicenseServerClient(endpoint)
                .SendAsync(
                    "validate",
                    activationResponse.ClientCredential,
                    deviceHash)
                .GetAwaiter()
                .GetResult();
        long validationMilliseconds = watch.ElapsedMilliseconds;

        LicenseServerCallResult invalidCredential =
            new LicenseServerClient(endpoint)
                .SendAsync(
                    "validate",
                    bootstrapCredential,
                    deviceHash)
                .GetAwaiter()
                .GetResult();

        bool passed =
            validation.IsReachable &&
            validation.Response?.Success == true &&
            validation.Response.Lease != null &&
            invalidCredential.IsReachable &&
            invalidCredential.Response?.Success == false &&
            string.Equals(
                invalidCredential.Response.Code,
                "BAD_CREDENTIAL",
                StringComparison.OrdinalIgnoreCase);

        Console.WriteLine(
            (passed ? "PASS " : "FAIL ") +
            "silent activation integration " +
            "(activate " + activationMilliseconds +
            " ms, validate " + validationMilliseconds + " ms; " +
            "validation=" +
            (validation.Response?.Code ?? validation.ErrorMessage) +
            ", bootstrap-as-client=" +
            (invalidCredential.Response?.Code ??
             invalidCredential.ErrorMessage) + ")");
        return passed ? 0 : 1;
    }

    private static bool EmbeddedBootstrapCredentialIsAvailable()
    {
        string credential = BootstrapCredentialProvider.GetCredential();
        return credential.StartsWith(
                   "LST-",
                   StringComparison.Ordinal) &&
               credential.Length >= 20;
    }

    private static bool ClientStateIsProtected()
    {
        const string credential =
            "LSTC.TEST-CREDENTIAL-MUST-NOT-APPEAR-IN-FILE";
        string variableName = "LSTOOLS_STATE_DIRECTORY";
        string? previousDirectory =
            Environment.GetEnvironmentVariable(variableName);
        string testDirectory = Path.Combine(
            Path.GetTempPath(),
            "lstools-state-test-" + Guid.NewGuid().ToString("N"));

        try
        {
            Environment.SetEnvironmentVariable(
                variableName,
                testDirectory);
            Directory.CreateDirectory(testDirectory);
            File.WriteAllText(
                Path.Combine(testDirectory, "license-client.json"),
                "legacy");

            LicenseClientStateStore.Save(
                new LicenseClientState
                {
                    ClientCredential = credential
                });

            byte[] storedBytes = File.ReadAllBytes(
                LicenseClientStateStore.StateFilePath);
            string storedText = Encoding.UTF8.GetString(storedBytes);
            LicenseClientState? loaded = LicenseClientStateStore.Load();

            return loaded?.ClientCredential == credential &&
                   !storedText.Contains(credential) &&
                   !File.Exists(
                       Path.Combine(
                           testDirectory,
                           "license-client.json"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                variableName,
                previousDirectory);
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    private static bool EmbeddedProductionKeyVerifiesFixture()
    {
        LicenseValidationResult result = LeaseVerifier.Verify(
            new SignedLease
            {
                Payload = ProductionPayload,
                Signature = ProductionSignature
            },
            DeviceHash,
            "LicenseSelfTest",
            new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        return result.IsValid &&
               result.Payload?.LicenseId == "INTERNAL-ONLINE-SELF-TEST-001";
    }

    private static bool ValidSignedLease()
    {
        using RSA rsa = CreateRsa();
        return Verify(rsa, CreatePayload()).IsValid;
    }

    private static bool WildcardFeature()
    {
        using RSA rsa = CreateRsa();
        LeasePayload payload = CreatePayload();
        payload.Features = new[] { "*" };
        return Verify(rsa, payload, "AnyFeature").IsValid;
    }

    private static bool NormalizedDeviceHash()
    {
        using RSA rsa = CreateRsa();
        LeasePayload payload = CreatePayload();
        payload.DeviceHash = "aaaabbbbccccddddeeeeffff";
        return Verify(rsa, payload).IsValid;
    }

    private static bool WrongDeviceRejected()
    {
        using RSA rsa = CreateRsa();
        LicenseValidationResult result = Verify(
            rsa,
            CreatePayload(),
            "BeamRebar",
            "1111-2222-3333-4444");
        return !result.IsValid &&
               result.Code == LicenseValidationCode.InvalidMachine;
    }

    private static bool MissingFeatureRejected()
    {
        using RSA rsa = CreateRsa();
        LicenseValidationResult result = Verify(
            rsa,
            CreatePayload(),
            "ColumnRebar");
        return !result.IsValid &&
               result.Code == LicenseValidationCode.FeatureNotLicensed;
    }

    private static bool WrongProductRejected()
    {
        using RSA rsa = CreateRsa();
        LeasePayload payload = CreatePayload();
        payload.Product = "OtherProduct";
        LicenseValidationResult result = Verify(rsa, payload);
        return !result.IsValid &&
               result.Code == LicenseValidationCode.InvalidProduct;
    }

    private static bool RevokedLeaseRejected()
    {
        using RSA rsa = CreateRsa();
        LeasePayload payload = CreatePayload();
        payload.Status = "Revoked";
        LicenseValidationResult result = Verify(rsa, payload);
        return !result.IsValid &&
               result.Code == LicenseValidationCode.Revoked;
    }

    private static bool FutureLeaseRejected()
    {
        using RSA rsa = CreateRsa();
        LeasePayload payload = CreatePayload();
        payload.IssuedUtc = Now.AddMinutes(6);
        payload.LeaseExpiresUtc = Now.AddDays(1);
        LicenseValidationResult result = Verify(rsa, payload);
        return !result.IsValid &&
               result.Code == LicenseValidationCode.NotYetValid;
    }

    private static bool ExpiredLicenseRejected()
    {
        using RSA rsa = CreateRsa();
        LeasePayload payload = CreatePayload();
        payload.ExpiresUtc = Now.AddMinutes(-1);
        payload.LeaseExpiresUtc = payload.ExpiresUtc;
        LicenseValidationResult result = Verify(rsa, payload);
        return !result.IsValid &&
               result.Code == LicenseValidationCode.Expired;
    }

    private static bool ExpiredLeaseRejected()
    {
        using RSA rsa = CreateRsa();
        LeasePayload payload = CreatePayload();
        payload.LeaseExpiresUtc = Now.AddMinutes(-1);
        LicenseValidationResult result = Verify(rsa, payload);
        return !result.IsValid &&
               result.Code == LicenseValidationCode.LeaseExpired;
    }

    private static bool InvalidLifetimeRejected()
    {
        using RSA rsa = CreateRsa();
        LeasePayload payload = CreatePayload();
        payload.LeaseExpiresUtc = payload.ExpiresUtc.AddMinutes(1);
        LicenseValidationResult result = Verify(rsa, payload);
        return !result.IsValid &&
               result.Code == LicenseValidationCode.InvalidFormat;
    }

    private static bool PayloadTamperingRejected()
    {
        using RSA rsa = CreateRsa();
        SignedLease lease = Issue(rsa, CreatePayload());
        char replacement = lease.Payload[lease.Payload.Length - 1] == 'A'
            ? 'B'
            : 'A';
        lease.Payload =
            lease.Payload.Substring(0, lease.Payload.Length - 1) + replacement;

        LicenseValidationResult result = LeaseVerifier.Verify(
            lease,
            DeviceHash,
            "BeamRebar",
            Now,
            rsa.ExportParameters(false));
        return !result.IsValid &&
               result.Code == LicenseValidationCode.InvalidSignature;
    }

    private static bool WrongSigningKeyRejected()
    {
        using RSA signer = CreateRsa();
        using RSA verifier = CreateRsa();
        SignedLease lease = Issue(signer, CreatePayload());
        LicenseValidationResult result = LeaseVerifier.Verify(
            lease,
            DeviceHash,
            "BeamRebar",
            Now,
            verifier.ExportParameters(false));
        return !result.IsValid &&
               result.Code == LicenseValidationCode.InvalidSignature;
    }

    private static bool MalformedEnvelopeRejected()
    {
        using RSA rsa = CreateRsa();
        LicenseValidationResult result = LeaseVerifier.Verify(
            new SignedLease
            {
                Payload = "not_base64!",
                Signature = "also_not_base64!"
            },
            DeviceHash,
            "BeamRebar",
            Now,
            rsa.ExportParameters(false));
        return !result.IsValid &&
               result.Code == LicenseValidationCode.InvalidFormat;
    }

    private static LeasePayload CreatePayload()
    {
        return new LeasePayload
        {
            SchemaVersion = LeaseVerifier.CurrentSchemaVersion,
            LicenseId = "TEST-001",
            Customer = "Kernel Test",
            Product = LeaseVerifier.ProductName,
            DeviceHash = DeviceHash,
            IssuedUtc = Now.AddHours(-1),
            ExpiresUtc = Now.AddDays(30),
            LeaseExpiresUtc = Now.AddDays(3),
            Features = new[] { "BeamRebar" },
            Status = "Active",
            Nonce = "abc123"
        };
    }

    private static LicenseValidationResult Verify(
        RSA rsa,
        LeasePayload payload,
        string feature = "BeamRebar",
        string deviceHash = DeviceHash)
    {
        return LeaseVerifier.Verify(
            Issue(rsa, payload),
            deviceHash,
            feature,
            Now,
            rsa.ExportParameters(false));
    }

    private static SignedLease Issue(RSA rsa, LeasePayload payload)
    {
        byte[] payloadBytes = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(payload, Formatting.None));
        byte[] signature = rsa.SignData(
            payloadBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return new SignedLease
        {
            Payload = Base64Url.Encode(payloadBytes),
            Signature = Base64Url.Encode(signature)
        };
    }

    private static RSA CreateRsa()
    {
        RSA rsa = RSA.Create();
        rsa.KeySize = 2048;
        return rsa;
    }

    private static int Run(string name, Func<bool> test)
    {
        try
        {
            bool passed = test();
            Console.WriteLine((passed ? "PASS " : "FAIL ") + name);
            return passed ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.WriteLine(
                "FAIL " + name + ": " + exception.GetType().Name +
                " - " + exception.Message);
            return 1;
        }
    }
}
