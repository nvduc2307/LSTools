using System.IO;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups
{
    internal class SecondaryShapeStirrup
    {
        private static RebarShape _rebarShape90_135;

        private static RebarShape _rebarShape135_135;

        private static RebarShape _rebarShape180_180;

        private static void FindOrCreateRebarShape90_135()
        {
            var rebarShape = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "secondary_90_135");

            if (rebarShape == null)
            {
                var assembly = Assembly.GetExecutingAssembly().Location;
                var folder = Path.GetDirectoryName(assembly);
                if (folder == null) return;

                folder = Path.Combine(folder, "Resources/RebarShape/");
#if R21
                folder = Path.Combine(folder, "2021");
#elif R22
                folder = Path.Combine(folder, "2022");
#else
                folder = Path.Combine(folder, "2023");
#endif
                var filePathMainRebarShape = Path.Combine(folder, "secondary_90_135.rfa");
                AC.Document.LoadFamily(filePathMainRebarShape);
            }

            _rebarShape90_135 = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "secondary_90_135");
        }

        private static void FindOrCreateRebarShape135_135()
        {
            var rebarShape = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "secondary_135_135");

            if (rebarShape == null)
            {
                var assembly = Assembly.GetExecutingAssembly().Location;
                var folder = Path.GetDirectoryName(assembly);
                if (folder == null) return;

                folder = Path.Combine(folder, "Resources/RebarShape/");
#if R21
                folder = Path.Combine(folder, "2021");
#elif R22
                folder = Path.Combine(folder, "2022");
#elif R23
                folder = Path.Combine(folder, "2023");
#else
                folder = Path.Combine(folder, "2024");
#endif
                var filePathMainRebarShape = Path.Combine(folder, "secondary_135_135.rfa");
                AC.Document.LoadFamily(filePathMainRebarShape);
            }

            _rebarShape135_135 = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "secondary_135_135");
        }

        private static void FindOrCreateRebarShape180_180()
        {
            var rebarShape = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "secondary_180_180");

            if (rebarShape == null)
            {
                var assembly = Assembly.GetExecutingAssembly().Location;
                var folder = Path.GetDirectoryName(assembly);
                if (folder == null) return;

                folder = Path.Combine(folder, "Resources/RebarShape/");
#if R21
                folder = Path.Combine(folder, "2021");
#elif R22
                folder = Path.Combine(folder, "2022");
#else
                folder = Path.Combine(folder, "2023");
#endif
                var filePathMainRebarShape = Path.Combine(folder, "secondary_180_180.rfa");
                AC.Document.LoadFamily(filePathMainRebarShape);
            }

            _rebarShape180_180 = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "secondary_180_180");
        }

        public static RebarShape GetRebarShape90_135()
        {
            if (_rebarShape90_135 == null || !_rebarShape90_135.IsValidObject || _rebarShape90_135.Document != AC.Document)
            {
                FindOrCreateRebarShape90_135();
            }

            return _rebarShape90_135;
        }

        public static RebarShape GetRebarShape135_135()
        {
            if (_rebarShape135_135 == null || !_rebarShape135_135.IsValidObject || _rebarShape135_135.Document != AC.Document)
            {
                FindOrCreateRebarShape135_135();
            }

            return _rebarShape135_135;
        }
        public static RebarShape GetRebarShape180_180()
        {
            if (_rebarShape180_180 == null || !_rebarShape180_180.IsValidObject || _rebarShape180_180.Document != AC.Document)
            {
                FindOrCreateRebarShape180_180();
            }

            return _rebarShape180_180;
        }
    }
}


