using Autodesk.Revit.DB;
using LSTool.Compatibility;

namespace RIMT.Utils.RevParameters
{
    public class RevParameter
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public Parameter Parameter { get; set; }
        public double ValueMm { get; set; }
        public RevParameter(Parameter parameter)
        {
            Id = parameter.Id.Value;
            Name = parameter.Definition.Name;
            Parameter = parameter;
            ValueMm = GetDoubleValue();
        }
        public RevParameter()
        {
            Id = -1;
            Name = "None";
            Parameter = null;
            ValueMm = 0;
        }
        private double GetDoubleValue()
        {
            try
            {
                return Parameter == null ? 0 : Parameter.AsDouble().FootToMm();
            }
            catch (Exception)
            {
            }
            return 0;
        }
    }
}
