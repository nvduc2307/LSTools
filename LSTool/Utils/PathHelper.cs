using System.IO;
using System.Reflection;

namespace LSTool.Utils
{
    public class PathHelper
    {
        public static string Parameters
        {
            get => $"{AssemblyDirectory}\\Resources\\Parameters";
        }
        public static string Families
        {
            get => $"{AssemblyDirectory}\\Resources\\Families";
        }
        public static string Templates
        {
            get => $"{AssemblyDirectory}\\Resources\\Templates";
        }
        public static string Datas
        {
            get => $"{AssemblyDirectory}\\Resources\\Datas";
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
