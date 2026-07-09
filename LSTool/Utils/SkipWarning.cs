namespace LSTool.Utils
{
    public static class SkipWarning
    {
        public static void SkipAllWarnings(this Transaction ts)
        {
            var op = ts.GetFailureHandlingOptions();
            var preproccessor = new SkipAllWarning();
            op.SetFailuresPreprocessor(preproccessor);
            ts.SetFailureHandlingOptions(op);
        }
        private class SkipAllWarning : IFailuresPreprocessor
        {
            FailureProcessingResult IFailuresPreprocessor.PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                var failMessage = failuresAccessor.GetFailureMessages();
                var failToList = failMessage.ToList();

                if (failToList.Count > 0)
                {
                    failuresAccessor.DeleteAllWarnings();
                    return FailureProcessingResult.ProceedWithCommit;
                }
                return FailureProcessingResult.Continue;
            }
        }
    }
}
