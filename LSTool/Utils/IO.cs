
namespace LSTool.Utils
{
    public static class IO
    {
        public static void ShowInfo(string content, string title = "Info")
        {
            MessageBox.Show(content, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowWarning(string content, string title = "Warning")
        {
            MessageBox.Show(content, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static DialogResult ShowQuestion(string content, string title = "Question")
        {
            return MessageBox.Show(content, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }

        public static void ShowException(Exception ex, string title = "Exception")
        {
            string content = ex.Message + "\n" + ex.StackTrace.ToString();
            MessageBox.Show(content, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
