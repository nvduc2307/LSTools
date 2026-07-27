using Autodesk.Revit.DB.Structure;
using RIMT.Utils.RevitElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    class RebarCutHole
    {
        public Rebar rebar { get; set; }
        public List<RevBeamHole> holes { get; set; }

        public RebarCutHole(Rebar _rebar, List<RevBeamHole> _holes)
        {
            rebar = _rebar;
            holes = _holes;
        }
    }
}


