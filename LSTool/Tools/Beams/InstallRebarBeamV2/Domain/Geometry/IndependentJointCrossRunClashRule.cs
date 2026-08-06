namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    public enum IndependentJointAnchorRunFamily
    {
        Other = 0,
        StraightThroughAnchor = 1,
        BentJointAnchor = 2
    }

    /// <summary>
    /// Detailing policy for the two independent anchor families belonging to
    /// the same joint stage. Their physical intersection is accepted. This
    /// does not permit overlap within either family or with any other rebar.
    /// </summary>
    public static class IndependentJointCrossRunClashRule
    {
        public static bool AllowsPhysicalOverlap(
            IndependentJointAnchorRunFamily first,
            IndependentJointAnchorRunFamily second)
        {
            return first
                    == IndependentJointAnchorRunFamily
                        .StraightThroughAnchor
                && second
                    == IndependentJointAnchorRunFamily.BentJointAnchor
                || first
                    == IndependentJointAnchorRunFamily.BentJointAnchor
                && second
                    == IndependentJointAnchorRunFamily
                        .StraightThroughAnchor;
        }
    }
}
