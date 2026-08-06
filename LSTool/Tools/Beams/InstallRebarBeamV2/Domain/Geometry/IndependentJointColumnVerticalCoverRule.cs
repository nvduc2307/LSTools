using System;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    public enum IndependentJointColumnVerticalCoverFailure
    {
        None = 0,
        InvalidInput = 1,
        EmptyCoverReducedColumn = 2,
        BentSourceOutsideColumn = 3,
        StraightSourceOutsideColumn = 4,
        BentTailEndOutsideColumn = 5
    }

    /// <summary>
    /// Checks the three independent-anchor elevations against the
    /// cover-reduced column height.
    ///
    /// The bent-tail endpoint is intentionally not required to enter the
    /// common depth of both adjacent beams. An hMin tail may be shorter than
    /// the beam centerline-elevation step while still being fully contained
    /// by the column and by the bent-side vertical limit checked during
    /// anchorage planning.
    /// </summary>
    public static class IndependentJointColumnVerticalCoverRule
    {
        public static IndependentJointColumnVerticalCoverFailure Evaluate(
            double bentSourceElevation,
            double straightSourceElevation,
            double bentTailEndElevation,
            double columnBottomElevation,
            double columnTopElevation,
            double centerlineClearance,
            double tolerance)
        {
            double[] values =
            {
                bentSourceElevation,
                straightSourceElevation,
                bentTailEndElevation,
                columnBottomElevation,
                columnTopElevation,
                centerlineClearance,
                tolerance
            };
            foreach (double value in values)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    return IndependentJointColumnVerticalCoverFailure
                        .InvalidInput;
            }
            if (centerlineClearance < 0.0 || tolerance <= 0.0)
            {
                return IndependentJointColumnVerticalCoverFailure
                    .InvalidInput;
            }

            double minimum = columnBottomElevation + centerlineClearance;
            double maximum = columnTopElevation - centerlineClearance;
            if (maximum < minimum - tolerance)
            {
                return IndependentJointColumnVerticalCoverFailure
                    .EmptyCoverReducedColumn;
            }
            if (!IsInside(
                    bentSourceElevation,
                    minimum,
                    maximum,
                    tolerance))
            {
                return IndependentJointColumnVerticalCoverFailure
                    .BentSourceOutsideColumn;
            }
            if (!IsInside(
                    straightSourceElevation,
                    minimum,
                    maximum,
                    tolerance))
            {
                return IndependentJointColumnVerticalCoverFailure
                    .StraightSourceOutsideColumn;
            }
            if (!IsInside(
                    bentTailEndElevation,
                    minimum,
                    maximum,
                    tolerance))
            {
                return IndependentJointColumnVerticalCoverFailure
                    .BentTailEndOutsideColumn;
            }
            return IndependentJointColumnVerticalCoverFailure.None;
        }

        private static bool IsInside(
            double value,
            double minimum,
            double maximum,
            double tolerance)
        {
            return value >= minimum - tolerance
                && value <= maximum + tolerance;
        }
    }
}
