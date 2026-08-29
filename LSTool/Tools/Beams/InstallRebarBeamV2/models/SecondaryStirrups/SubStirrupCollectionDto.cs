using Autodesk.Revit.DB;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.MainStirrups;
using RIMT.Utils.BoundingBoxs;
using RIMT.Utils.RevRebars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups
{
    public class SubStirrupCollectionDto
    {
        public RebarBarTypeCustom RebarBarTypeCustom { get; set; }
        public BoxElementPoint BoxElementPoint { get; set; }
        public double Spacing { get; set; }
        public MainStirrupShapeEnum Shape { get; set; }
        public Element Host { get; set; }
        public XYZ Direction { get; set; }
        public CoverFootBeam CoverFootBeam { get; set; }
        public XYZ Top { get; set; }
        public XYZ Bottom { get; set; }
        public Document Document { get; set; }
        /// <summary>
        /// Sử dụng để biết hướng hook vào trong hay ra ngoài cho thép đai
        /// </summary>
        public XYZ DirectionInside { get; set; }

        /// <summary>
        /// Bật thì hai đầu móc đổi bên ở thanh chẵn và thanh lẻ. Mặc định tắt:
        /// mọi thanh cùng một chiều nên giống hệt nhau về hình học và gom được
        /// chung một rebar set bố trí Fixed Number.
        /// </summary>
        public bool StaggerHooks { get; set; } = false;
        public SubStirrupCollectionDto Copy()
        {
            return (SubStirrupCollectionDto)MemberwiseClone();
        }
    }
}


