using LSTool.Tools.Columns.ColumnRebar.geometry;
using LSTool.Tools.Columns.ColumnRebar.models;
using LSTool.Utils;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    internal static class ColumnRebarStackGrouping
    {
        private const double MaximumPlanOffsetMillimeters = 300;

        internal static List<List<ColumnConcreteModel>> Group(
            IReadOnlyList<ColumnConcreteModel> columns)
        {
            return ColumnStackGrouping.Group(
                columns,
                column => column.Center.X,
                column => column.Center.Y,
                column => column.Center.Z,
                MaximumPlanOffsetMillimeters.FromMillimeters());
        }
    }
}
