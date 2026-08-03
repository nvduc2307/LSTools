using System;
using System.Collections.Generic;
using System.Linq;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    /// <summary>
    /// Describes whether two independent main-bar anchors can be planned
    /// across a joint between bars at different centerline elevations.
    /// </summary>
    public enum IndependentJointAnchorageStatus
    {
        Planned = 0,
        Unsupported = 1
    }

    /// <summary>
    /// Machine-readable reason why independent anchorage cannot be planned or
    /// why a supplied centerline does not satisfy the plan.
    /// </summary>
    public enum IndependentJointAnchorageFailure
    {
        None = 0,
        MissingInput = 1,
        NonFiniteValue = 2,
        InvalidTolerance = 3,
        InvalidLength = 4,
        InvalidBendRadius = 5,
        NoDepthStep = 6,
        NonMonotonicStations = 7,
        InsufficientStraightAnchorAvailability = 8,
        StraightAnchorDoesNotCrossJoint = 9,
        InsufficientBentAnchorAvailability = 10,
        BendOutsideJoint = 11,
        InsufficientBendFaceInset = 12,
        InsufficientTangentLength = 13,
        InvalidPointChain = 14,
        InsufficientProvidedAnchorage = 15
    }

    /// <summary>
    /// Revit-independent inputs for the two main-bar runs.
    ///
    /// Stations are ordered from the bent-side beam to the straight-side
    /// beam. They may increase or decrease, which makes the same policy work
    /// when the beam order is mirrored. The historical Deep/Shallow property
    /// names now represent bent/straight source elevations respectively.
    /// RequiredAnchorageLength is measured from the bent-side joint face into
    /// the bent-side beam. The joint width is crossed in addition to this
    /// development length. The temporary 35-diameter rule belongs to the
    /// caller rather than this geometry kernel.
    /// </summary>
    public sealed class IndependentJointAnchorageInput
    {
        public double RunStartStation { get; }
        public double JointStartStation { get; }
        public double JointEndStation { get; }
        public double RunEndStation { get; }
        public double DeepBarElevation { get; }
        public double ShallowBarElevation { get; }
        public double BentVerticalLimitElevation { get; }
        public double RequiredAnchorageLength { get; }
        public double BendInsetFromShallowFace { get; }
        public double CenterlineClearance { get; }
        public double CenterlineBendRadius { get; }
        public double MinimumStraightLength { get; }
        public double Tolerance { get; }

        public IndependentJointAnchorageInput(
            double runStartStation,
            double jointStartStation,
            double jointEndStation,
            double runEndStation,
            double deepBarElevation,
            double shallowBarElevation,
            double bentVerticalLimitElevation,
            double requiredAnchorageLength,
            double bendInsetFromShallowFace,
            double centerlineClearance,
            double centerlineBendRadius,
            double minimumStraightLength,
            double tolerance)
        {
            RunStartStation = runStartStation;
            JointStartStation = jointStartStation;
            JointEndStation = jointEndStation;
            RunEndStation = runEndStation;
            DeepBarElevation = deepBarElevation;
            ShallowBarElevation = shallowBarElevation;
            BentVerticalLimitElevation = bentVerticalLimitElevation;
            RequiredAnchorageLength = requiredAnchorageLength;
            BendInsetFromShallowFace = bendInsetFromShallowFace;
            CenterlineClearance = centerlineClearance;
            CenterlineBendRadius = centerlineBendRadius;
            MinimumStraightLength = minimumStraightLength;
            Tolerance = tolerance;
        }
    }

    /// <summary>
    /// Immutable geometry and measured lengths for one successful policy plan.
    /// StraightThroughPoints are ordered from the straight-side beam towards
    /// the bent-side beam. BentVerticalPoints are ordered from the bent-side
    /// beam towards the joint and then vertically towards the straight run.
    /// </summary>
    public sealed class IndependentJointAnchorageResult
    {
        private static readonly IReadOnlyList<BentZStationPoint> NoPoints =
            Array.AsReadOnly(new BentZStationPoint[0]);

        public IndependentJointAnchorageStatus Status { get; }
        public IndependentJointAnchorageFailure Failure { get; }
        public string Message { get; }
        public IReadOnlyList<BentZStationPoint> StraightThroughPoints { get; }
        public IReadOnlyList<BentZStationPoint> BentVerticalPoints { get; }
        public double StraightProvidedAnchorageLength { get; }
        public double BentProvidedAnchorageLength { get; }
        public double TangentSetback { get; }
        public double RemainingHorizontalStraight { get; }
        public double RemainingVerticalStraight { get; }

        private IndependentJointAnchorageResult(
            IndependentJointAnchorageStatus status,
            IndependentJointAnchorageFailure failure,
            string message,
            IReadOnlyList<BentZStationPoint> straightThroughPoints,
            IReadOnlyList<BentZStationPoint> bentVerticalPoints,
            double straightProvidedAnchorageLength,
            double bentProvidedAnchorageLength,
            double tangentSetback,
            double remainingHorizontalStraight,
            double remainingVerticalStraight)
        {
            Status = status;
            Failure = failure;
            Message = message;
            StraightThroughPoints = straightThroughPoints;
            BentVerticalPoints = bentVerticalPoints;
            StraightProvidedAnchorageLength =
                straightProvidedAnchorageLength;
            BentProvidedAnchorageLength = bentProvidedAnchorageLength;
            TangentSetback = tangentSetback;
            RemainingHorizontalStraight = remainingHorizontalStraight;
            RemainingVerticalStraight = remainingVerticalStraight;
        }

        internal static IndependentJointAnchorageResult Unsupported(
            IndependentJointAnchorageFailure failure,
            string message)
        {
            return new IndependentJointAnchorageResult(
                IndependentJointAnchorageStatus.Unsupported,
                failure,
                message,
                NoPoints,
                NoPoints,
                0.0,
                0.0,
                0.0,
                0.0,
                0.0);
        }

        internal static IndependentJointAnchorageResult Planned(
            BentZStationPoint[] straightThroughPoints,
            BentZStationPoint[] bentVerticalPoints,
            double straightProvidedAnchorageLength,
            double bentProvidedAnchorageLength,
            double tangentSetback,
            double remainingHorizontalStraight,
            double remainingVerticalStraight)
        {
            return new IndependentJointAnchorageResult(
                IndependentJointAnchorageStatus.Planned,
                IndependentJointAnchorageFailure.None,
                string.Empty,
                Array.AsReadOnly(straightThroughPoints),
                Array.AsReadOnly(bentVerticalPoints),
                straightProvidedAnchorageLength,
                bentProvidedAnchorageLength,
                tangentSetback,
                remainingHorizontalStraight,
                remainingVerticalStraight);
        }
    }

    public sealed class IndependentJointAnchorageValidationResult
    {
        public bool IsValid { get; }
        public IndependentJointAnchorageFailure Failure { get; }
        public string Message { get; }
        public double StraightProvidedAnchorageLength { get; }
        public double BentProvidedAnchorageLength { get; }
        public double TangentSetback { get; }

        private IndependentJointAnchorageValidationResult(
            bool isValid,
            IndependentJointAnchorageFailure failure,
            string message,
            double straightProvidedAnchorageLength,
            double bentProvidedAnchorageLength,
            double tangentSetback)
        {
            IsValid = isValid;
            Failure = failure;
            Message = message;
            StraightProvidedAnchorageLength =
                straightProvidedAnchorageLength;
            BentProvidedAnchorageLength = bentProvidedAnchorageLength;
            TangentSetback = tangentSetback;
        }

        internal static IndependentJointAnchorageValidationResult Valid(
            double straightProvidedAnchorageLength,
            double bentProvidedAnchorageLength,
            double tangentSetback)
        {
            return new IndependentJointAnchorageValidationResult(
                true,
                IndependentJointAnchorageFailure.None,
                string.Empty,
                straightProvidedAnchorageLength,
                bentProvidedAnchorageLength,
                tangentSetback);
        }

        internal static IndependentJointAnchorageValidationResult Unsupported(
            IndependentJointAnchorageFailure failure,
            string message)
        {
            return new IndependentJointAnchorageValidationResult(
                false,
                failure,
                message,
                0.0,
                0.0,
                0.0);
        }
    }

    /// <summary>
    /// Pure geometry policy for two main bars at different elevations:
    /// 1) the straight-side bar remains horizontal and is extended from its
    ///    joint face, through the joint, into the bent-side beam;
    /// 2) the bent-side bar enters the joint and turns vertically towards the
    ///    straight-side bar elevation.
    ///
    /// The vertical direction may be positive or negative, so the same kernel
    /// supports bottom reinforcement and its mirrored top-reinforcement case.
    ///
    /// The two outputs are independent runs. This class never joins them and
    /// never substitutes a Bent/Z transition.
    /// </summary>
    public static class IndependentJointAnchorageGeometry
    {
        public static IndependentJointAnchorageResult Plan(
            IndependentJointAnchorageInput input)
        {
            IndependentJointAnchorageResult? inputFailure =
                ValidateInput(input);
            if (inputFailure != null)
            {
                return inputFailure;
            }

            double direction = Math.Sign(
                input.RunEndStation - input.RunStartStation);
            double verticalDirection = Math.Sign(
                input.ShallowBarElevation - input.DeepBarElevation);
            double straightAvailable = DirectedDistance(
                input.RunStartStation,
                input.JointStartStation,
                direction);

            if (input.RequiredAnchorageLength >
                straightAvailable + input.Tolerance)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure
                        .InsufficientStraightAnchorAvailability,
                    "The bent-side beam concrete envelope is shorter than "
                    + "the required straight-through development length "
                    + "measured from the bent-side joint face.");
            }

            double availableVertical = (
                input.BentVerticalLimitElevation
                - input.DeepBarElevation) * verticalDirection;
            if (availableVertical + input.Tolerance <
                input.RequiredAnchorageLength)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure
                        .InsufficientBentAnchorAvailability,
                    "The joint has insufficient vertical room for the "
                    + "required bent anchorage.");
            }

            double bendStation =
                input.JointEndStation
                - direction * input.BendInsetFromShallowFace;
            double bendFromDeepFace = DirectedDistance(
                input.JointStartStation,
                bendStation,
                direction);
            double bendFromShallowFace = DirectedDistance(
                bendStation,
                input.JointEndStation,
                direction);
            if (bendFromDeepFace <= input.Tolerance
                || bendFromShallowFace <= input.Tolerance)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure.BendOutsideJoint,
                    "The bend vertex must be strictly inside the joint.");
            }

            // A horizontal-to-vertical fillet is a 90-degree bend, therefore
            // R * tan(theta / 2) equals R.
            double tangentSetback = input.CenterlineBendRadius;
            double requiredFaceInset =
                input.CenterlineClearance + tangentSetback;
            if (bendFromDeepFace + input.Tolerance < requiredFaceInset
                || bendFromShallowFace + input.Tolerance <
                    requiredFaceInset)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure
                        .InsufficientBendFaceInset,
                    "The rounded bend cannot maintain its centerline "
                    + "clearance from both joint faces.");
            }

            double horizontalLength = DirectedDistance(
                input.RunStartStation,
                bendStation,
                direction);
            double remainingHorizontalStraight =
                horizontalLength - tangentSetback;
            double remainingVerticalStraight =
                input.RequiredAnchorageLength - tangentSetback;
            if (remainingHorizontalStraight + input.Tolerance <
                    input.MinimumStraightLength
                || remainingVerticalStraight + input.Tolerance <
                    input.MinimumStraightLength)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure
                        .InsufficientTangentLength,
                    "A straight leg remaining after the rounded bend is "
                    + "shorter than the required minimum.");
            }

            double straightAnchorEndStation =
                input.JointStartStation
                - direction * input.RequiredAnchorageLength;
            double bentEndElevation =
                input.DeepBarElevation
                + verticalDirection * input.RequiredAnchorageLength;

            var straightThroughPoints = new[]
            {
                new BentZStationPoint(
                    input.RunEndStation,
                    input.ShallowBarElevation),
                new BentZStationPoint(
                    straightAnchorEndStation,
                    input.ShallowBarElevation)
            };
            var bentVerticalPoints = new[]
            {
                new BentZStationPoint(
                    input.RunStartStation,
                    input.DeepBarElevation),
                new BentZStationPoint(
                    bendStation,
                    input.DeepBarElevation),
                new BentZStationPoint(
                    bendStation,
                    bentEndElevation)
            };

            IndependentJointAnchorageValidationResult validation =
                Validate(
                    input,
                    straightThroughPoints,
                    bentVerticalPoints);
            if (!validation.IsValid)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    validation.Failure,
                    validation.Message);
            }

            return IndependentJointAnchorageResult.Planned(
                straightThroughPoints,
                bentVerticalPoints,
                validation.StraightProvidedAnchorageLength,
                validation.BentProvidedAnchorageLength,
                validation.TangentSetback,
                remainingHorizontalStraight,
                remainingVerticalStraight);
        }

        /// <summary>
        /// Validates caller-supplied centerline vertices against the same
        /// independent-anchorage policy. Longer-than-required anchors are
        /// accepted; shortened, connected, diagonal or wrongly directed runs
        /// fail closed.
        /// </summary>
        public static IndependentJointAnchorageValidationResult Validate(
            IndependentJointAnchorageInput input,
            IReadOnlyList<BentZStationPoint> straightThroughPoints,
            IReadOnlyList<BentZStationPoint> bentVerticalPoints)
        {
            IndependentJointAnchorageResult? inputFailure =
                ValidateInput(input);
            if (inputFailure != null)
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    inputFailure.Failure,
                    inputFailure.Message);
            }
            if (straightThroughPoints == null
                || straightThroughPoints.Count != 2
                || bentVerticalPoints == null
                || bentVerticalPoints.Count != 3)
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    IndependentJointAnchorageFailure.InvalidPointChain,
                    "Independent anchorage requires a two-point straight run "
                    + "and a three-point horizontal-vertical run.");
            }
            if (straightThroughPoints.Any(point => !IsFinitePoint(point))
                || bentVerticalPoints.Any(point => !IsFinitePoint(point)))
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    IndependentJointAnchorageFailure.NonFiniteValue,
                    "All independent-anchorage points must be finite.");
            }

            double tolerance = input.Tolerance;
            double direction = Math.Sign(
                input.RunEndStation - input.RunStartStation);
            double verticalDirection = Math.Sign(
                input.ShallowBarElevation - input.DeepBarElevation);

            BentZStationPoint straightStart = straightThroughPoints[0];
            BentZStationPoint straightEnd = straightThroughPoints[1];
            if (!Near(
                    straightStart.Station,
                    input.RunEndStation,
                    tolerance)
                || !Near(
                    straightStart.Elevation,
                    input.ShallowBarElevation,
                    tolerance)
                || !Near(
                    straightEnd.Elevation,
                    input.ShallowBarElevation,
                    tolerance)
                || DirectedDistance(
                    straightEnd.Station,
                    straightStart.Station,
                    direction) <= tolerance)
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    IndependentJointAnchorageFailure.InvalidPointChain,
                    "The straight-through run must remain horizontal and be "
                    + "ordered from the straight-side beam towards the "
                    + "bent-side beam.");
            }

            double straightProvided = DirectedDistance(
                straightEnd.Station,
                input.JointStartStation,
                direction);
            if (straightProvided + tolerance <
                input.RequiredAnchorageLength)
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    IndependentJointAnchorageFailure
                        .InsufficientProvidedAnchorage,
                    "The straight-through run is shorter than the required "
                    + "anchorage length.");
            }
            if (DirectedDistance(
                    straightEnd.Station,
                    input.JointStartStation,
                    direction) <= tolerance)
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    IndependentJointAnchorageFailure
                        .StraightAnchorDoesNotCrossJoint,
                    "The straight-through run does not enter the bent-side "
                    + "beam.");
            }
            if (DirectedDistance(
                    input.RunStartStation,
                    straightEnd.Station,
                    direction) < -tolerance)
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    IndependentJointAnchorageFailure
                        .InsufficientStraightAnchorAvailability,
                    "The straight-through run leaves the available bent-side "
                    + "concrete envelope.");
            }

            BentZStationPoint bentStart = bentVerticalPoints[0];
            BentZStationPoint bendVertex = bentVerticalPoints[1];
            BentZStationPoint bentEnd = bentVerticalPoints[2];
            if (!Near(
                    bentStart.Station,
                    input.RunStartStation,
                    tolerance)
                || !Near(
                    bentStart.Elevation,
                    input.DeepBarElevation,
                    tolerance)
                || !Near(
                    bendVertex.Elevation,
                    input.DeepBarElevation,
                    tolerance)
                || !Near(
                    bentEnd.Station,
                    bendVertex.Station,
                    tolerance)
                || DirectedDistance(
                    bentStart.Station,
                    bendVertex.Station,
                    direction) <= tolerance
                || (bentEnd.Elevation - bendVertex.Elevation)
                    * verticalDirection <= tolerance)
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    IndependentJointAnchorageFailure.InvalidPointChain,
                    "The bent run must be horizontal into the joint and then "
                    + "vertical towards the straight-side elevation.");
            }

            double bendFromDeepFace = DirectedDistance(
                input.JointStartStation,
                bendVertex.Station,
                direction);
            double bendFromShallowFace = DirectedDistance(
                bendVertex.Station,
                input.JointEndStation,
                direction);
            if (bendFromDeepFace <= tolerance
                || bendFromShallowFace <= tolerance)
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    IndependentJointAnchorageFailure.BendOutsideJoint,
                    "The bend vertex is outside the joint.");
            }

            double tangentSetback = input.CenterlineBendRadius;
            double requiredFaceInset =
                input.CenterlineClearance + tangentSetback;
            if (bendFromDeepFace + tolerance < requiredFaceInset
                || bendFromShallowFace + tolerance < requiredFaceInset)
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    IndependentJointAnchorageFailure
                        .InsufficientBendFaceInset,
                    "The rounded bend cannot maintain its centerline "
                    + "clearance from both joint faces.");
            }

            double horizontalLength = DirectedDistance(
                bentStart.Station,
                bendVertex.Station,
                direction);
            double bentProvided = Math.Abs(
                bentEnd.Elevation - bendVertex.Elevation);
            if (bentProvided + tolerance <
                input.RequiredAnchorageLength)
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    IndependentJointAnchorageFailure
                        .InsufficientProvidedAnchorage,
                    "The vertical leg is shorter than the required bent "
                    + "anchorage.");
            }
            if (horizontalLength - tangentSetback + tolerance <
                    input.MinimumStraightLength
                || bentProvided - tangentSetback + tolerance <
                    input.MinimumStraightLength)
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    IndependentJointAnchorageFailure
                        .InsufficientTangentLength,
                    "A straight leg remaining after the rounded bend is "
                    + "shorter than the required minimum.");
            }

            double availableVertical = (
                input.BentVerticalLimitElevation
                - input.DeepBarElevation) * verticalDirection;
            if (bentProvided > availableVertical + tolerance)
            {
                return IndependentJointAnchorageValidationResult.Unsupported(
                    IndependentJointAnchorageFailure
                        .InsufficientBentAnchorAvailability,
                    "The bent run leaves the available vertical concrete "
                    + "envelope.");
            }

            return IndependentJointAnchorageValidationResult.Valid(
                straightProvided,
                bentProvided,
                tangentSetback);
        }

        private static IndependentJointAnchorageResult? ValidateInput(
            IndependentJointAnchorageInput input)
        {
            if (input == null)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure.MissingInput,
                    "Independent-anchorage input is required.");
            }
            if (!AllValuesAreFinite(input))
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure.NonFiniteValue,
                    "All independent-anchorage values must be finite.");
            }
            if (input.Tolerance < 0.0)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure.InvalidTolerance,
                    "Tolerance cannot be negative.");
            }
            if (input.RequiredAnchorageLength <= 0.0
                || input.MinimumStraightLength <= 0.0
                || input.BendInsetFromShallowFace < 0.0
                || input.CenterlineClearance < 0.0)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure.InvalidLength,
                    "Anchorage, inset, clearance and minimum straight "
                    + "lengths must be valid positive values.");
            }
            if (input.CenterlineBendRadius <= 0.0)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure.InvalidBendRadius,
                    "Centerline bend radius must be greater than zero.");
            }

            double elevationStep =
                input.ShallowBarElevation - input.DeepBarElevation;
            if (Math.Abs(elevationStep) <= input.Tolerance)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure.NoDepthStep,
                    "Independent anchorage requires different main-bar "
                    + "elevations.");
            }

            double runDelta =
                input.RunEndStation - input.RunStartStation;
            if (Math.Abs(runDelta) <= input.Tolerance)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure.NonMonotonicStations,
                    "Run stations must advance in one direction.");
            }
            double direction = Math.Sign(runDelta);
            double startToJoint = DirectedDistance(
                input.RunStartStation,
                input.JointStartStation,
                direction);
            double jointWidth = DirectedDistance(
                input.JointStartStation,
                input.JointEndStation,
                direction);
            double jointToEnd = DirectedDistance(
                input.JointEndStation,
                input.RunEndStation,
                direction);
            if (startToJoint <= input.Tolerance
                || jointWidth <= input.Tolerance
                || jointToEnd <= input.Tolerance)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure.NonMonotonicStations,
                    "Run and joint stations must be strictly monotonic from "
                    + "the bent-side beam to the straight-side beam.");
            }

            double verticalDirection = Math.Sign(elevationStep);
            double verticalAvailability = (
                input.BentVerticalLimitElevation
                - input.DeepBarElevation) * verticalDirection;
            if (verticalAvailability <= input.Tolerance)
            {
                return IndependentJointAnchorageResult.Unsupported(
                    IndependentJointAnchorageFailure
                        .InsufficientBentAnchorAvailability,
                    "The bent-anchor limit must lie from the bent-side bar "
                    + "towards the straight-side elevation.");
            }

            return null;
        }

        private static bool AllValuesAreFinite(
            IndependentJointAnchorageInput input)
        {
            return IsFinite(input.RunStartStation)
                && IsFinite(input.JointStartStation)
                && IsFinite(input.JointEndStation)
                && IsFinite(input.RunEndStation)
                && IsFinite(input.DeepBarElevation)
                && IsFinite(input.ShallowBarElevation)
                && IsFinite(input.BentVerticalLimitElevation)
                && IsFinite(input.RequiredAnchorageLength)
                && IsFinite(input.BendInsetFromShallowFace)
                && IsFinite(input.CenterlineClearance)
                && IsFinite(input.CenterlineBendRadius)
                && IsFinite(input.MinimumStraightLength)
                && IsFinite(input.Tolerance);
        }

        private static bool IsFinitePoint(BentZStationPoint point)
        {
            return point != null
                && IsFinite(point.Station)
                && IsFinite(point.Elevation);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value);
        }

        private static bool Near(
            double first,
            double second,
            double tolerance)
        {
            return Math.Abs(first - second) <= tolerance;
        }

        private static double DirectedDistance(
            double from,
            double to,
            double direction)
        {
            return (to - from) * direction;
        }
    }
}
