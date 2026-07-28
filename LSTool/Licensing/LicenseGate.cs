using System;
using LSTool.Utils;

namespace LSTool.Licensing
{
    public static class LicenseGate
    {
        public static bool EnsureFeature(string requiredFeature)
        {
            LicenseValidationResult result;
            try
            {
                result = OnlineLicenseService.Validate(requiredFeature);
            }
            catch
            {
                result = LicenseValidationResult.Failure(
                    LicenseValidationCode.ServerRejected,
                    "Không thể xác nhận phiên sử dụng.");
            }

            if (result.IsValid)
            {
                return true;
            }

            IO.ShowWarning(GetCustomerMessage(result.Code), "LSTools");
            return false;
        }

        private static string GetCustomerMessage(
            LicenseValidationCode code)
        {
            switch (code)
            {
                case LicenseValidationCode.Expired:
                case LicenseValidationCode.LeaseExpired:
                    return
                        "Thời gian sử dụng bản thử nghiệm đã kết thúc.\n\n" +
                        "Vui lòng liên hệ nhà cung cấp để tiếp tục sử dụng.";

                case LicenseValidationCode.NetworkError:
                case LicenseValidationCode.ServerNotConfigured:
                    return
                        "Chưa thể xác nhận phiên sử dụng.\n\n" +
                        "Vui lòng kiểm tra kết nối Internet và thử lại.";

                case LicenseValidationCode.FeatureNotLicensed:
                    return
                        "Tính năng này không khả dụng trong phiên bản hiện tại.";

                default:
                    return
                        "Phiên bản LSTools này hiện không khả dụng.\n\n" +
                        "Vui lòng liên hệ nhà cung cấp.";
            }
        }
    }
}
