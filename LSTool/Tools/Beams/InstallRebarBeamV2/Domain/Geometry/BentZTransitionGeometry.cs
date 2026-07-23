using System;
using System.Collections.Generic;
using System.Linq;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    /// <summary>
    /// Describes whether a Bent/Z transition is needed and can be constructed.
    /// </summary>
    public enum BentZTransitionStatus
    {
        NotApplicable = 0,
        Planned = 1,
        Unsupported = 2
    }

    /// <summary>
    /// Machine-readable reason for an unsupported transition.
    /// </summary>
    public enum BentZTransitionFailure
    {
        None = 0,
        MissingInput = 1,
        NonFiniteValue = 2,
        InvalidTolerance = 3,
        InvalidMinimumSegmentLength = 4,
        InvalidBendInset = 5,
        NonMonotonicStations = 6,
        InsufficientHorizontalRun = 7,
        InsufficientTransitionWindow = 8
    }

    /// <summary>
    /// A point in the two-dimensional station/elevation plane of a beam run.
    /// </summary>
    public sealed class BentZStationPoint
    {
        public double Station { get; }
        public double Elevation { get; }

        public BentZStationPoint(double station, double elevation)
        {
            Station = station;
            Elevation = elevation;
        }
    }

    /// <summary>
    /// Revit-independent inputs used to plan one horizontal-diagonal-horizontal
    /// Bent/Z centerline through a joint window.
    /// </summary>
    public sealed class BentZTransitionInput
    {
        public double RunStartStation { get; }
        public double JointStartStation { get; }
        public double JointEndStation { get; }
        public double RunEndStation { get; }
        public double StartElevation { get; }
        public double EndElevation { get; }
        public double BendInset { get; }
        public double MinimumSegmentLength { get; }
        public double ElevationTolerance { get; }

        public BentZTransitionInput(
            double runStartStation,
            double jointStartStation,
            double jointEndStation,
            double runEndStation,
            double startElevation,
            double endElevation,
            double bendInset,
            double minimumSegmentLength,
            double elevationTolerance)
        {
            RunStartStation = runStartStation;
            JointStartStation = jointStartStation;
            JointEndStation = jointEndStation;
            RunEndStation = runEndStation;
            StartElevation = startElevation;
            EndElevation = endElevation;
            BendInset = bendInset;
            MinimumSegmentLength = minimumSegmentLength;
            ElevationTolerance = elevationTolerance;
        }
    }

    /// <summary>
    /// Immutable outcome of planning a Bent/Z transition.
    /// </summary>
    public sealed class BentZTransitionResult
    {
        private static readonly IReadOnlyList<BentZStationPoint> NoPoints =
            Array.AsReadOnly(new BentZStationPoint[0]);

        public BentZTransitionStatus Status { get; }
        public BentZTransitionFailure Failure { get; }
        public string Message { get; }
        public IReadOnlyList<BentZStationPoint> Points { get; }

        private BentZTransitionResult(
            BentZTransitionStatus status,
            BentZTransitionFailure failure,
            string message,
            IReadOnlyList<BentZStationPoint> points)
        {
            Status = status;
            Failure = failure;
            Message = message;
            Points = points;
        }

        internal static BentZTransitionResult NotApplicable()
        {
            return new BentZTransitionResult(
                BentZTransitionStatus.NotApplicable,
                BentZTransitionFailure.None,
                "The elevation change is within tolerance.",
                NoPoints);
        }

        internal static BentZTransitionResult Unsupported(
            BentZTransitionFailure failure,
            string message)
        {
            return new BentZTransitionResult(
                BentZTransitionStatus.Unsupported,
                failure,
                message,
                NoPoints);
        }

        internal static BentZTransitionResult Planned(BentZStationPoint[] points)
        {
            return new BentZTransitionResult(
                BentZTransitionStatus.Planned,
                BentZTransitionFailure.None,
                string.Empty,
                Array.AsReadOnly(points));
        }
    }

    public enum BentZBendValidationFailure
    {
        None = 0,
        InvalidPointChain = 1,
        NonFiniteValue = 2,
        InvalidRadiusOrTolerance = 3,
        InvalidBendAngle = 4,
        InsufficientFaceInset = 5,
        InsufficientTangentLength = 6
    }

    /// <summary>
    /// Result of checking the straight-line vertices against the rounded bends
    /// that Revit will derive from the selected bar type.
    /// </summary>
    public sealed class BentZBendValidationResult
    {
        public bool IsValid { get; }
        public BentZBendValidationFailure Failure { get; }
        public string Message { get; }
        public double AngleRadians { get; }
        public double TangentSetback { get; }
        public double RemainingDiagonalStraight { get; }

        private BentZBendValidationResult(
            bool isValid,
            BentZBendValidationFailure failure,
            string message,
            double angleRadians,
            double tangentSetback,
            double remainingDiagonalStraight)
        {
            IsValid = isValid;
            Failure = failure;
            Message = message;
            AngleRadians = angleRadians;
            TangentSetback = tangentSetback;
            RemainingDiagonalStraight = remainingDiagonalStraight;
        }

        internal static BentZBendValidationResult Valid(
            double angleRadians,
            double tangentSetback,
            double remainingDiagonalStraight)
        {
            return new BentZBendValidationResult(
                true,
                BentZBendValidationFailure.None,
                string.Empty,
                angleRadians,
                tangentSetback,
                remainingDiagonalStraight);
        }

        internal static BentZBendValidationResult Unsupported(
            BentZBendValidationFailure failure,
            string message)
        {
            return new BentZBendValidationResult(
                false,
                failure,
                message,
                0.0,
                0.0,
                0.0);
        }
    }

    public sealed class BentZLanePair
    {
        public double StartCoordinate { get; }
        public double EndCoordinate { get; }

        public BentZLanePair(
            double startCoordinate,
            double endCoordinate)
        {
            StartCoordinate = startCoordinate;
            EndCoordinate = endCoordinate;
        }
    }

    public enum BentZLaneSetValidationFailure
    {
        None = 0,
        InvalidInput = 1,
        LaneCountMismatch = 2,
        TransverseLaneMismatch = 3,
        DuplicateLane = 4,
        InsufficientLaneSpacing = 5
    }

    public sealed class BentZLaneSetValidationResult
    {
        public bool IsValid { get; }
        public BentZLaneSetValidationFailure Failure { get; }
        public string Message { get; }

        private BentZLaneSetValidationResult(
            bool isValid,
            BentZLaneSetValidationFailure failure,
            string message)
        {
            IsValid = isValid;
            Failure = failure;
            Message = message;
        }

        internal static BentZLaneSetValidationResult Valid()
        {
            return new BentZLaneSetValidationResult(
                true,
                BentZLaneSetValidationFailure.None,
                string.Empty);
        }

        internal static BentZLaneSetValidationResult Unsupported(
            BentZLaneSetValidationFailure failure,
            string message)
        {
            return new BentZLaneSetValidationResult(
                false,
                failure,
                message);
        }
    }

    /// <summary>
    /// Pure geometry kernel for a Bent/Z transition. Stations may increase or
    /// decrease; returned points always follow the requested run direction.
    /// </summary>
    public static class BentZTransitionGeometry
    {
        public static BentZTransitionResult Plan(BentZTransitionInput input)
        {
            if (input == null)
            {
                return BentZTransitionResult.Unsupported(
                    BentZTransitionFailure.MissingInput,
                    "Transition input is required.");
            }

            if (!AllValuesAreFinite(input))
            {
                return BentZTransitionResult.Unsupported(
                    BentZTransitionFailure.NonFiniteValue,
                    "All transition values must be finite numbers.");
            }

            if (input.ElevationTolerance < 0.0)
            {
                return BentZTransitionResult.Unsupported(
                    BentZTransitionFailure.InvalidTolerance,
                    "Elevation tolerance cannot be negative.");
            }

            if (input.MinimumSegmentLength <= 0.0)
            {
                return BentZTransitionResult.Unsupported(
                    BentZTransitionFailure.InvalidMinimumSegmentLength,
                    "Minimum segment length must be greater than zero.");
            }

            if (input.BendInset < 0.0)
            {
                return BentZTransitionResult.Unsupported(
                    BentZTransitionFailure.InvalidBendInset,
                    "Bend inset cannot be negative.");
            }

            double elevationChange = input.EndElevation - input.StartElevation;
            if (Math.Abs(elevationChange) <= input.ElevationTolerance)
            {
                return BentZTransitionResult.NotApplicable();
            }

            double runDelta = input.RunEndStation - input.RunStartStation;
            if (runDelta == 0.0)
            {
                return BentZTransitionResult.Unsupported(
                    BentZTransitionFailure.NonMonotonicStations,
                    "Run stations must advance in one direction.");
            }

            double direction = runDelta > 0.0 ? 1.0 : -1.0;
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

            if (startToJoint <= 0.0 || jointWidth <= 0.0 || jointToEnd <= 0.0)
            {
                return BentZTransitionResult.Unsupported(
                    BentZTransitionFailure.NonMonotonicStations,
                    "Run and joint stations must be strictly monotonic.");
            }

            double entryStation =
                input.JointStartStation + direction * input.BendInset;
            double exitStation =
                input.JointEndStation - direction * input.BendInset;

            double firstRunLength = DirectedDistance(
                input.RunStartStation,
                entryStation,
                direction);
            double transitionRunLength = DirectedDistance(
                entryStation,
                exitStation,
                direction);
            double lastRunLength = DirectedDistance(
                exitStation,
                input.RunEndStation,
                direction);

            if (firstRunLength < input.MinimumSegmentLength ||
                lastRunLength < input.MinimumSegmentLength)
            {
                return BentZTransitionResult.Unsupported(
                    BentZTransitionFailure.InsufficientHorizontalRun,
                    "A horizontal run is shorter than the minimum segment length.");
            }

            // A positive station component is intentional: a vertical connector
            // is not a Bent/Z diagonal and must not be emitted as a fallback.
            if (transitionRunLength < input.MinimumSegmentLength)
            {
                return BentZTransitionResult.Unsupported(
                    BentZTransitionFailure.InsufficientTransitionWindow,
                    "The joint window is too short after applying bend insets.");
            }

            return BentZTransitionResult.Planned(
                new[]
                {
                    new BentZStationPoint(
                        input.RunStartStation,
                        input.StartElevation),
                    new BentZStationPoint(
                        entryStation,
                        input.StartElevation),
                    new BentZStationPoint(
                        exitStation,
                        input.EndElevation),
                    new BentZStationPoint(
                        input.RunEndStation,
                        input.EndElevation)
                });
        }

        public static BentZBendValidationResult ValidateRoundedBends(
            IReadOnlyList<BentZStationPoint> points,
            double bendInset,
            double centerlineClearance,
            double centerlineBendRadius,
            double minimumStraightAfterBend,
            double tolerance)
        {
            if (points == null || points.Count != 4)
            {
                return BentZBendValidationResult.Unsupported(
                    BentZBendValidationFailure.InvalidPointChain,
                    "A Bent/Z bend check requires exactly four points.");
            }
            if (!IsFinite(bendInset)
                || !IsFinite(centerlineClearance)
                || !IsFinite(centerlineBendRadius)
                || !IsFinite(minimumStraightAfterBend)
                || !IsFinite(tolerance)
                || points.Any(point =>
                    point == null
                    || !IsFinite(point.Station)
                    || !IsFinite(point.Elevation)))
            {
                return BentZBendValidationResult.Unsupported(
                    BentZBendValidationFailure.NonFiniteValue,
                    "All bend validation values must be finite.");
            }
            if (bendInset < 0.0
                || centerlineClearance < 0.0
                || centerlineBendRadius <= 0.0
                || minimumStraightAfterBend <= 0.0
                || tolerance < 0.0)
            {
                return BentZBendValidationResult.Unsupported(
                    BentZBendValidationFailure.InvalidRadiusOrTolerance,
                    "Bend radius, clearance, straight length and tolerance "
                    + "must be positive valid values.");
            }

            double firstStationDelta =
                points[1].Station - points[0].Station;
            double transitionStationDelta =
                points[2].Station - points[1].Station;
            double lastStationDelta =
                points[3].Station - points[2].Station;
            double direction = Math.Sign(firstStationDelta);
            if (Math.Abs(firstStationDelta) <= tolerance
                || transitionStationDelta * direction <= tolerance
                || lastStationDelta * direction <= tolerance
                || Math.Abs(
                    points[1].Elevation - points[0].Elevation)
                > tolerance
                || Math.Abs(
                    points[3].Elevation - points[2].Elevation)
                > tolerance)
            {
                return BentZBendValidationResult.Unsupported(
                    BentZBendValidationFailure.InvalidPointChain,
                    "Bent/Z points must be strictly monotonic with horizontal "
                    + "outer legs.");
            }

            double firstHorizontalLength =
                Math.Abs(firstStationDelta);
            double transitionHorizontalLength =
                Math.Abs(transitionStationDelta);
            double lastHorizontalLength =
                Math.Abs(lastStationDelta);
            double elevationChange =
                Math.Abs(points[2].Elevation - points[1].Elevation);
            if (transitionHorizontalLength <= tolerance
                || elevationChange <= tolerance)
            {
                return BentZBendValidationResult.Unsupported(
                    BentZBendValidationFailure.InvalidBendAngle,
                    "The diagonal must change both station and elevation.");
            }

            double angleRadians = Math.Atan2(
                elevationChange,
                transitionHorizontalLength);
            if (angleRadians <= 0.0
                || angleRadians >= Math.PI / 2.0)
            {
                return BentZBendValidationResult.Unsupported(
                    BentZBendValidationFailure.InvalidBendAngle,
                    "The bend angle must be between zero and ninety degrees.");
            }

            double tangentSetback =
                centerlineBendRadius * Math.Tan(angleRadians / 2.0);
            double requiredFaceInset =
                centerlineClearance + tangentSetback;
            if (bendInset + tolerance < requiredFaceInset)
            {
                return BentZBendValidationResult.Unsupported(
                    BentZBendValidationFailure.InsufficientFaceInset,
                    "The bend vertex is too close to the joint face after "
                    + "applying the actual tangent setback.");
            }

            double diagonalLength = Math.Sqrt(
                transitionHorizontalLength * transitionHorizontalLength
                + elevationChange * elevationChange);
            double remainingFirstStraight =
                firstHorizontalLength - tangentSetback;
            double remainingDiagonalStraight =
                diagonalLength - tangentSetback * 2.0;
            double remainingLastStraight =
                lastHorizontalLength - tangentSetback;
            if (remainingFirstStraight < minimumStraightAfterBend
                || remainingDiagonalStraight < minimumStraightAfterBend
                || remainingLastStraight < minimumStraightAfterBend)
            {
                return BentZBendValidationResult.Unsupported(
                    BentZBendValidationFailure.InsufficientTangentLength,
                    "A straight portion is too short after both bend tangencies.");
            }

            return BentZBendValidationResult.Valid(
                angleRadians,
                tangentSetback,
                remainingDiagonalStraight);
        }

        public static BentZLaneSetValidationResult ValidateLaneSet(
            IReadOnlyList<BentZLanePair> lanes,
            int expectedLaneCount,
            double tolerance,
            double minimumLaneSpacing = 0.0)
        {
            if (lanes == null
                || expectedLaneCount <= 0
                || !IsFinite(tolerance)
                || tolerance < 0.0
                || !IsFinite(minimumLaneSpacing)
                || minimumLaneSpacing < 0.0)
            {
                return BentZLaneSetValidationResult.Unsupported(
                    BentZLaneSetValidationFailure.InvalidInput,
                    "A positive expected lane count and non-negative finite "
                    + "tolerance are required.");
            }
            if (lanes.Count != expectedLaneCount)
            {
                return BentZLaneSetValidationResult.Unsupported(
                    BentZLaneSetValidationFailure.LaneCountMismatch,
                    $"Expected {expectedLaneCount} transition lanes, but "
                    + $"received {lanes.Count}.");
            }

            var normalizedCoordinates = new List<double>(lanes.Count);
            foreach (var lane in lanes)
            {
                if (lane == null
                    || !IsFinite(lane.StartCoordinate)
                    || !IsFinite(lane.EndCoordinate))
                {
                    return BentZLaneSetValidationResult.Unsupported(
                        BentZLaneSetValidationFailure.InvalidInput,
                        "Every transition lane must contain finite coordinates.");
                }
                if (Math.Abs(
                        lane.EndCoordinate - lane.StartCoordinate)
                    > tolerance)
                {
                    return BentZLaneSetValidationResult.Unsupported(
                        BentZLaneSetValidationFailure.TransverseLaneMismatch,
                        "A transition lane changes transverse position.");
                }
                normalizedCoordinates.Add(
                    (lane.StartCoordinate + lane.EndCoordinate) / 2.0);
            }

            normalizedCoordinates.Sort();
            for (int index = 1;
                 index < normalizedCoordinates.Count;
                 index++)
            {
                if (Math.Abs(
                        normalizedCoordinates[index]
                        - normalizedCoordinates[index - 1])
                    <= tolerance)
                {
                    return BentZLaneSetValidationResult.Unsupported(
                        BentZLaneSetValidationFailure.DuplicateLane,
                        "Two transition runs occupy the same transverse lane.");
                }
                if (normalizedCoordinates[index]
                    - normalizedCoordinates[index - 1]
                    < minimumLaneSpacing)
                {
                    return BentZLaneSetValidationResult.Unsupported(
                        BentZLaneSetValidationFailure
                            .InsufficientLaneSpacing,
                        "Two transition lanes are closer than the required "
                        + "centerline spacing.");
                }
            }
            return BentZLaneSetValidationResult.Valid();
        }

        private static double DirectedDistance(
            double from,
            double to,
            double direction)
        {
            return (to - from) * direction;
        }

        private static bool AllValuesAreFinite(BentZTransitionInput input)
        {
            return IsFinite(input.RunStartStation) &&
                   IsFinite(input.JointStartStation) &&
                   IsFinite(input.JointEndStation) &&
                   IsFinite(input.RunEndStation) &&
                   IsFinite(input.StartElevation) &&
                   IsFinite(input.EndElevation) &&
                   IsFinite(input.BendInset) &&
                   IsFinite(input.MinimumSegmentLength) &&
                   IsFinite(input.ElevationTolerance);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
