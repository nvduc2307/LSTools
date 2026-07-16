using System.Text.RegularExpressions;

namespace LSTool.Utils
{
    public static class NumberHelper
    {
        public static bool IsNumber(this string text)
        {
            try
            {
                Regex regex = new Regex(@"^[-+]?[0-9]*\.?[0-9]+$");
                return regex.IsMatch(text);
            }
            catch (Exception)
            {
            }
            return false;
        }
        public static int GetIntegerFromText(this string text)
        {
            var resultString = Regex.Match(text, @"\d+").Value;
            return !string.IsNullOrEmpty(resultString) ? int.Parse(resultString) : 0;
        }
        public static int DivInterger(this double num1, double num2, out double du)
        {
            du = num1 % num2;
            var n = int.Parse(Math.Round((num1 - du) / num2, 0).ToString(), System.Globalization.NumberStyles.Integer) + 1;
            return n;
        }
        public static int SolveNumber(this double num, double spacing)
        {
            var d = num % spacing;
            var per = d * 100 / spacing;
            var n = int.Parse($"{Math.Round((num - d) / spacing, 0)}");
            return per > 20 ? n + 1 : n;
        }
        public static int SolveNumber(this int num, int spacing)
        {
            var d = num % spacing;
            var per = d * 100 / spacing;
            var n = (num - d) / spacing;
            return d > 0 ? n + 1 : n;
        }
        public static int FindInterger(this string t)
        {
            var re = new Regex(@"\d+");
            var vl = re.Match(t);
            return string.IsNullOrEmpty(vl.Value) ? 0 : int.Parse(vl.Value);
        }
    }
}
