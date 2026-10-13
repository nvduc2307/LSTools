
namespace LSTool.Utils.Families
{
    public static class FamiliesHelper
    {
        public static void LoadFamily(
            Document document,
            string pathFamily)
        {
            var optionLoadF = new FamilyLoadOptionCustom();
            document.LoadFamily(pathFamily, optionLoadF, out Family family);
        }

        public class FamilyLoadOptionCustom : IFamilyLoadOptions
        {
            public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
            {
                bool loadFamily = true;

                if (familyInUse)
                {
                    overwriteParameterValues = true;
                }
                else
                {
                    overwriteParameterValues = true;
                }

                return loadFamily;
            }

            public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
            {
                source = FamilySource.Family;

                overwriteParameterValues = true;

                return true;
            }
        }
    }
}
