using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models.MainStirrups
{
    internal class ShapeMainStirrup
    {
        private static RebarShape _rebarShape1;

        private static RebarShape _rebarShape2;
        private static RebarShape _rebarShape3;
        private static RebarShape _rebarShape4;

        private static void FindOrCreateRebarShape1()
        {
            var rebarShape = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "main_stirrup");

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
                var filePathMainRebarShape = Path.Combine(folder, "main_stirrup.rfa");
                AC.Document.LoadFamily(filePathMainRebarShape);
            }

            _rebarShape1 = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "main_stirrup");
        }

        public static RebarShape GetRebarShape1()
        {
            if (_rebarShape1 == null || !_rebarShape1.IsValidObject || _rebarShape1.Document != AC.Document)
            {
                FindOrCreateRebarShape1();
            }

            return _rebarShape1;
        }

        private static void FindOrCreateRebarShape2()
        {
            var rebarShape = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "main_stirrup2");

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
                var filePathMainRebarShape = Path.Combine(folder, "main_stirrup2.rfa");
                AC.Document.LoadFamily(filePathMainRebarShape);
            }

            _rebarShape2 = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "main_stirrup2");
        }

        public static RebarShape GetRebarShape2()
        {
            if (_rebarShape2 == null || !_rebarShape2.IsValidObject || _rebarShape2.Document != AC.Document)
            {
                FindOrCreateRebarShape2();
            }

            return _rebarShape2;
        }

        private static void FindOrCreateRebarShape3()
        {
            var rebarShape = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "M_S3");

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
                var filePathMainRebarShape = Path.Combine(folder, "M_S3.rfa");
                AC.Document.LoadFamily(filePathMainRebarShape);
            }

            _rebarShape3 = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "M_S3");
        }

        public static RebarShape GetRebarShape3()
        {
            if (_rebarShape3 == null || !_rebarShape3.IsValidObject || _rebarShape3.Document != AC.Document)
            {
                FindOrCreateRebarShape3();
            }

            return _rebarShape3;
        }

        private static void FindOrCreateRebarShape4()
        {
            var rebarShape = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "M_S4_180");

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
                var filePathMainRebarShape = Path.Combine(folder, "M_S4_180.rfa");
                AC.Document.LoadFamily(filePathMainRebarShape);
            }

            _rebarShape4 = new FilteredElementCollector(AC.Document).WhereElementIsElementType()
                .OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name == "M_S4_180");
        }

        public static RebarShape GetRebarShape4()
        {
            if (_rebarShape4 == null || !_rebarShape4.IsValidObject || _rebarShape4.Document != AC.Document)
            {
                FindOrCreateRebarShape4();
            }

            return _rebarShape4;
        }
    }
}


