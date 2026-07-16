using Autodesk.Revit.DB;

namespace RIMT.Utils
{
    public static class IO
    {
        public static void ShowInfo(string content, string title = "Info")
            => LSTool.Utils.IO.ShowInfo(content, title);

        public static void ShowWarning(string content, string title = "Warning")
            => LSTool.Utils.IO.ShowWarning(content, title);
    }
}

namespace RIMT.Utils.SkipWarning
{
    public static class TransactionExtensions
    {
        public static void SkipAllWarnings(this Transaction transaction)
        {
            var options = transaction.GetFailureHandlingOptions();
            options.SetFailuresPreprocessor(new DeleteWarningsPreprocessor());
            transaction.SetFailureHandlingOptions(options);
        }

        private sealed class DeleteWarningsPreprocessor : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                failuresAccessor.DeleteAllWarnings();
                return FailureProcessingResult.Continue;
            }
        }
    }
}
