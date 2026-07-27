using System;
using System.Collections.Generic;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry;

internal static class Program
{
    private const double Epsilon = 1e-9;

    private static int Main()
    {
        var tests = new List<Action>
        {
            FlatRunIsNotApplicable,
            PositiveAndNegativeChangesAreMirrors,
            ReversingTheRunReversesThePoints,
            PlannedShapeHasFourPointsAndThreeSegments,
            BendVerticesAreInsideAdjacentBeams,
            Project11BentZPlacementAndRoundedBendsAreValid,
            InsufficientJointWindowIsUnsupported,
            NonFiniteInputIsUnsupported,
            NonMonotonicStationsAreUnsupported,
            ShortHorizontalRunIsUnsupported,
            ElevationToleranceIsApplied,
            InvalidToleranceIsUnsupported,
            RoundedBendsAcceptConservativeInset,
            RoundedBendsRejectInsufficientFaceInset,
            RoundedBendsRejectInsufficientTangentLength,
            RoundedBendValidationIsDirectionIndependent,
            RoundedBendsRejectZigZagStations,
            RoundedBendsRejectNonHorizontalOuterLeg,
            LaneSetAcceptsUniqueLanesInAnyOrder,
            LaneSetRejectsDuplicateLanes,
            LaneSetRejectsMissingLane,
            LaneSetRejectsTransverseDrift,
            LaneSetRejectsInsufficientBarSpacing,
            TransitionPolicyTreatsAlignedDeltaAsLegacy,
            TransitionPolicyUsesBentZAtTwoHundredMillimeters,
            TransitionPolicyUsesIndependentAboveTwoHundredMillimeters,
            TransitionPolicyUsesAbsoluteDelta,
            TransitionPolicyRejectsMixedLanePolicies,
            TransitionPolicyRejectsInvalidThreshold,
            TemporaryRuleReturnsThirtyFiveDiameters,
            TemporaryRuleRejectsInvalidDiameter,
            TemporaryClearanceUsesModeledValueFirst,
            TemporaryClearanceFallsBackToConfiguredBeamValue,
            TemporaryClearanceRejectsMissingValues,
            ColumnEnvelopeAcceptsExactBoundary,
            ColumnEnvelopeAcceptsVerificationBudget,
            ColumnEnvelopeRejectsBeyondVerificationBudget,
            ColumnEnvelopeReportsEveryFailedSide,
            ColumnEnvelopeRejectsInvalidOrEmptyInput,
            IndependentAnchorageUsesCallerSuppliedThirtyFiveDiameters,
            IndependentAnchorageMirrorsBeamOrder,
            IndependentAnchorageSupportsOppositeVerticalDirection,
            IndependentAnchorageRejectsNonFiniteInput,
            IndependentAnchorageRejectsNonMonotonicStations,
            IndependentAnchorageRejectsMissingStraightAvailability,
            IndependentAnchorageMustCrossTheJoint,
            IndependentAnchorageRejectsMissingVerticalAvailability,
            IndependentAnchorageRejectsInsufficientFaceInset,
            IndependentAnchorageRejectsInsufficientRoundedBendLeg,
            IndependentAnchorageValidationRejectsShortenedStraightRun,
            IndependentAnchorageValidationRejectsDiagonalBentRun,
            IndependentAnchorageValidationAcceptsLongerRuns,
            LaneStaggerKeepsAlreadySafeLanes,
            LaneStaggerMovesToTheOnlyAvailableSide,
            LaneStaggerIsInputOrderIndependent,
            LaneStaggerMirrorsOneSidedLayout,
            LaneStaggerEnforcesBentLaneSpacing,
            LaneStaggerRejectsInsufficientWidth,
            LaneStaggerPlansSymmetricThreeLaneLayout,
            LaneStaggerPlansCoverBoundedThreeLaneLayout,
            LaneStaggerMirrorsSymmetricLayoutWithOppositePreference,
            LaneStaggerValidationRejectsStraightLaneClash,
            LaneStaggerValidationRejectsBentLaneClash,
            LaneStaggerValidationRejectsWrongCount
        };

        try
        {
            foreach (Action test in tests)
            {
                test();
                Console.WriteLine("PASS " + test.Method.Name);
            }

            Console.WriteLine(
                "All " + tests.Count + " reinforcement geometry tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FAIL " + exception.Message);
            return 1;
        }
    }

    private static void TransitionPolicyTreatsAlignedDeltaAsLegacy()
    {
        MainBarTransitionClassification result =
            MainBarTransitionPolicyClassifier.Classify(
                new[] { -1.0, 0.0, 1.0 },
                1.0,
                200.0);

        Equal(true, result.IsValid, "aligned policy validity");
        Equal(
            MainBarTransitionPolicy.LegacyAligned,
            result.Policy,
            "aligned policy");
    }

    private static void TransitionPolicyUsesBentZAtTwoHundredMillimeters()
    {
        MainBarTransitionClassification result =
            MainBarTransitionPolicyClassifier.Classify(
                new[] { 200.0, 200.0 },
                1.0,
                200.0);

        Equal(true, result.IsValid, "200 mm policy validity");
        Equal(
            MainBarTransitionPolicy.BentZContinuous,
            result.Policy,
            "200 mm policy");
    }

    private static void TransitionPolicyUsesIndependentAboveTwoHundredMillimeters()
    {
        MainBarTransitionClassification result =
            MainBarTransitionPolicyClassifier.Classify(
                new[] { 200.001, 200.001 },
                1.0,
                200.0);

        Equal(true, result.IsValid, "above 200 mm policy validity");
        Equal(
            MainBarTransitionPolicy.IndependentAnchorage35D,
            result.Policy,
            "above 200 mm policy");
    }

    private static void TransitionPolicyUsesAbsoluteDelta()
    {
        MainBarTransitionClassification positive =
            MainBarTransitionPolicyClassifier.Classify(
                new[] { 125.0 },
                1.0,
                200.0);
        MainBarTransitionClassification negative =
            MainBarTransitionPolicyClassifier.Classify(
                new[] { -125.0 },
                1.0,
                200.0);

        Equal(true, positive.IsValid, "positive delta validity");
        Equal(true, negative.IsValid, "negative delta validity");
        Equal(positive.Policy, negative.Policy, "signed delta policy");
        Equal(
            MainBarTransitionPolicy.BentZContinuous,
            negative.Policy,
            "negative Bent/Z policy");
    }

    private static void TransitionPolicyRejectsMixedLanePolicies()
    {
        MainBarTransitionClassification result =
            MainBarTransitionPolicyClassifier.Classify(
                new[] { 100.0, 250.0 },
                1.0,
                200.0);

        Equal(false, result.IsValid, "mixed lane policy validity");
        Equal(
            MainBarTransitionClassificationFailure.InconsistentLanePolicy,
            result.Failure,
            "mixed lane failure");
    }

    private static void TransitionPolicyRejectsInvalidThreshold()
    {
        MainBarTransitionClassification result =
            MainBarTransitionPolicyClassifier.Classify(
                new[] { 100.0 },
                200.0,
                200.0);

        Equal(false, result.IsValid, "invalid threshold validity");
        Equal(
            MainBarTransitionClassificationFailure.InvalidThreshold,
            result.Failure,
            "invalid threshold failure");
    }

    private static void TemporaryRuleReturnsThirtyFiveDiameters()
    {
        Near(
            35.0 * 0.019,
            TemporaryIndependentJointAnchorageRule
                .GetRequiredLength(0.019),
            "temporary 35D rule");
    }

    private static void TemporaryRuleRejectsInvalidDiameter()
    {
        bool rejected = false;
        try
        {
            TemporaryIndependentJointAnchorageRule
                .GetRequiredLength(0.0);
        }
        catch (ArgumentOutOfRangeException)
        {
            rejected = true;
        }

        Equal(true, rejected, "temporary invalid diameter rejection");
    }

    private static void TemporaryClearanceUsesModeledValueFirst()
    {
        TemporaryJointStirrupSelection selection =
            TemporaryJointStirrupFallbackRule.Resolve(
                0.032,
                0.010,
                "stirrup diameter");

        Near(0.032, selection.Value, "modeled clearance value");
        Equal(
            false,
            selection.UsedConfiguredBeamFallback,
            "modeled clearance source");
    }

    private static void TemporaryClearanceFallsBackToConfiguredBeamValue()
    {
        TemporaryJointStirrupSelection selection =
            TemporaryJointStirrupFallbackRule.Resolve(
                0.0,
                0.010,
                "stirrup diameter");

        Near(0.010, selection.Value, "configured clearance fallback");
        Equal(
            true,
            selection.UsedConfiguredBeamFallback,
            "configured clearance source");
    }

    private static void TemporaryClearanceRejectsMissingValues()
    {
        bool rejected = false;
        try
        {
            TemporaryJointStirrupFallbackRule.Resolve(
                0.0,
                double.NaN,
                "stirrup diameter");
        }
        catch (ArgumentOutOfRangeException)
        {
            rejected = true;
        }

        Equal(true, rejected, "missing clearance rejection");
    }

    private static void ColumnEnvelopeAcceptsExactBoundary()
    {
        BentZColumnEnvelopeResult result =
            BentZColumnEnvelopeRule.Evaluate(
                2.0,
                2.0,
                8.0,
                0.0,
                10.0,
                0.0,
                10.0,
                2.0,
                0.01);

        Equal(true, result.Fits, "exact envelope boundary");
        Equal(
            BentZColumnEnvelopeViolation.None,
            result.Violations,
            "exact boundary violations");
        Near(0.0, result.MinimumYMargin, "exact minimum Y margin");
        Near(0.0, result.MinimumZMargin, "exact minimum Z margin");
        Near(0.0, result.MaximumZMargin, "exact maximum Z margin");
    }

    private static void ColumnEnvelopeAcceptsVerificationBudget()
    {
        BentZColumnEnvelopeResult result =
            BentZColumnEnvelopeRule.Evaluate(
                1.99,
                1.99,
                8.01,
                0.0,
                10.0,
                0.0,
                10.0,
                2.0,
                0.01);

        Equal(true, result.Fits, "verification-budget envelope");
        Equal(
            BentZColumnEnvelopeViolation.None,
            result.Violations,
            "verification-budget violations");
        Near(-0.01, result.MinimumYMargin, "budget minimum Y margin");
        Near(-0.01, result.MinimumZMargin, "budget minimum Z margin");
        Near(-0.01, result.MaximumZMargin, "budget maximum Z margin");
    }

    private static void ColumnEnvelopeRejectsBeyondVerificationBudget()
    {
        BentZColumnEnvelopeResult result =
            BentZColumnEnvelopeRule.Evaluate(
                1.989,
                2.0,
                8.0,
                0.0,
                10.0,
                0.0,
                10.0,
                2.0,
                0.01);

        Equal(false, result.Fits, "outside-budget envelope");
        Equal(
            BentZColumnEnvelopeViolation.MinimumY,
            result.Violations,
            "outside-budget violation");
    }

    private static void ColumnEnvelopeReportsEveryFailedSide()
    {
        BentZColumnEnvelopeResult result =
            BentZColumnEnvelopeRule.Evaluate(
                9.0,
                1.0,
                9.0,
                0.0,
                10.0,
                0.0,
                10.0,
                2.0,
                0.01);

        Equal(false, result.Fits, "multi-side envelope");
        Equal(
            BentZColumnEnvelopeViolation.MaximumY
            | BentZColumnEnvelopeViolation.MinimumZ
            | BentZColumnEnvelopeViolation.MaximumZ,
            result.Violations,
            "multi-side violations");
    }

    private static void ColumnEnvelopeRejectsInvalidOrEmptyInput()
    {
        BentZColumnEnvelopeResult invalid =
            BentZColumnEnvelopeRule.Evaluate(
                double.NaN,
                2.0,
                8.0,
                0.0,
                10.0,
                0.0,
                10.0,
                2.0,
                0.01);
        BentZColumnEnvelopeResult empty =
            BentZColumnEnvelopeRule.Evaluate(
                5.0,
                5.0,
                5.0,
                0.0,
                3.0,
                0.0,
                3.0,
                2.0,
                0.01);

        Equal(false, invalid.Fits, "invalid envelope");
        Equal(
            BentZColumnEnvelopeViolation.InvalidInput,
            invalid.Violations,
            "invalid envelope violation");
        Equal(false, empty.Fits, "empty envelope");
        Equal(
            BentZColumnEnvelopeViolation.EmptyEnvelope,
            empty.Violations,
            "empty envelope violation");
    }

    private static void FlatRunIsNotApplicable()
    {
        BentZTransitionResult result = Plan(2.0, 2.0);

        Equal(BentZTransitionStatus.NotApplicable, result.Status, "status");
        Equal(BentZTransitionFailure.None, result.Failure, "failure");
        Equal(0, result.Points.Count, "point count");
    }

    private static void PositiveAndNegativeChangesAreMirrors()
    {
        BentZTransitionResult rising = Plan(1.5, 4.25);
        BentZTransitionResult falling = Plan(-1.5, -4.25);

        Equal(BentZTransitionStatus.Planned, rising.Status, "rising status");
        Equal(BentZTransitionStatus.Planned, falling.Status, "falling status");
        Equal(rising.Points.Count, falling.Points.Count, "mirror point count");

        for (int index = 0; index < rising.Points.Count; index++)
        {
            Near(
                rising.Points[index].Station,
                falling.Points[index].Station,
                "mirror station " + index);
            Near(
                rising.Points[index].Elevation,
                -falling.Points[index].Elevation,
                "mirror elevation " + index);
        }
    }

    private static void ReversingTheRunReversesThePoints()
    {
        BentZTransitionResult forward = Plan(1.0, 4.0);
        BentZTransitionResult reverse = BentZTransitionGeometry.Plan(
            new BentZTransitionInput(
                12.0,
                8.0,
                4.0,
                0.0,
                4.0,
                1.0,
                0.75,
                0.25,
                0.01));

        Equal(BentZTransitionStatus.Planned, reverse.Status, "reverse status");
        Equal(forward.Points.Count, reverse.Points.Count, "reverse point count");

        for (int index = 0; index < forward.Points.Count; index++)
        {
            BentZStationPoint expected =
                forward.Points[forward.Points.Count - 1 - index];
            BentZStationPoint actual = reverse.Points[index];
            Near(expected.Station, actual.Station, "reverse station " + index);
            Near(expected.Elevation, actual.Elevation, "reverse elevation " + index);
        }
    }

    private static void PlannedShapeHasFourPointsAndThreeSegments()
    {
        BentZTransitionResult result = Plan(0.0, 3.0);

        Equal(BentZTransitionStatus.Planned, result.Status, "status");
        Equal(4, result.Points.Count, "point count");
        Equal(3, result.Points.Count - 1, "segment count");
        Near(result.Points[0].Elevation, result.Points[1].Elevation, "first run");
        Near(result.Points[2].Elevation, result.Points[3].Elevation, "last run");

        if (Math.Abs(result.Points[2].Elevation - result.Points[1].Elevation) <= Epsilon)
        {
            throw new InvalidOperationException("Diagonal segment has no elevation change.");
        }

        for (int index = 1; index < result.Points.Count; index++)
        {
            if (result.Points[index].Station <= result.Points[index - 1].Station)
            {
                throw new InvalidOperationException(
                    "Planned stations are not strictly ordered.");
            }
        }
    }

    private static void BendVerticesAreInsideAdjacentBeams()
    {
        BentZTransitionResult result = Plan(0.0, 3.0);

        Equal(BentZTransitionStatus.Planned, result.Status, "status");
        Near(3.25, result.Points[1].Station, "entering-beam bend");
        Near(8.75, result.Points[2].Station, "leaving-beam bend");
        if (result.Points[1].Station >= 4.0
            || result.Points[2].Station <= 8.0)
        {
            throw new InvalidOperationException(
                "Bent/Z vertices must be outside the joint and inside the "
                + "adjacent beams.");
        }
    }

    private static void Project11BentZPlacementAndRoundedBendsAreValid()
    {
        BentZTransitionResult result = BentZTransitionGeometry.Plan(
            new BentZTransitionInput(
                3215.047,
                -7743.18,
                -8343.18,
                -18288.582,
                2848.5,
                2948.5,
                102.01,
                21.0,
                1.0));

        Equal(BentZTransitionStatus.Planned, result.Status, "Project11 plan");
        Near(-7641.17, result.Points[1].Station, "Project11 entering bend");
        Near(-8445.19, result.Points[2].Station, "Project11 leaving bend");
        if (result.Points[1].Station <= -7743.18
            || result.Points[2].Station >= -8343.18)
        {
            throw new InvalidOperationException(
                "Project11 bend vertices did not move into the two beams.");
        }

        BentZBendValidationResult validation =
            BentZTransitionGeometry.ValidateRoundedBends(
                result.Points,
                102.01,
                51.51,
                50.5,
                21.0,
                1.0);
        Equal(true, validation.IsValid, "Project11 rounded bends");
        if (validation.RemainingDiagonalStraight <= 0.0)
        {
            throw new InvalidOperationException(
                "Project11 diagonal has no straight portion after bend arcs.");
        }
    }

    private static void InsufficientJointWindowIsUnsupported()
    {
        BentZTransitionResult result = BentZTransitionGeometry.Plan(
            new BentZTransitionInput(
                0.0,
                4.0,
                4.1,
                10.0,
                0.0,
                2.0,
                0.0,
                0.25,
                0.01));

        Equal(BentZTransitionStatus.Unsupported, result.Status, "status");
        Equal(
            BentZTransitionFailure.InsufficientTransitionWindow,
            result.Failure,
            "failure");
        Equal(0, result.Points.Count, "point count");
    }

    private static void NonFiniteInputIsUnsupported()
    {
        BentZTransitionResult result = BentZTransitionGeometry.Plan(
            new BentZTransitionInput(
                0.0,
                4.0,
                double.PositiveInfinity,
                12.0,
                0.0,
                3.0,
                0.75,
                0.25,
                0.01));

        Equal(BentZTransitionStatus.Unsupported, result.Status, "status");
        Equal(BentZTransitionFailure.NonFiniteValue, result.Failure, "failure");
    }

    private static void NonMonotonicStationsAreUnsupported()
    {
        BentZTransitionResult result = BentZTransitionGeometry.Plan(
            new BentZTransitionInput(
                0.0,
                7.0,
                4.0,
                12.0,
                0.0,
                3.0,
                0.75,
                0.25,
                0.01));

        Equal(BentZTransitionStatus.Unsupported, result.Status, "status");
        Equal(
            BentZTransitionFailure.NonMonotonicStations,
            result.Failure,
            "failure");
    }

    private static void ShortHorizontalRunIsUnsupported()
    {
        BentZTransitionResult result = BentZTransitionGeometry.Plan(
            new BentZTransitionInput(
                0.0,
                0.5,
                4.0,
                8.0,
                0.0,
                3.0,
                0.4,
                0.25,
                0.01));

        Equal(BentZTransitionStatus.Unsupported, result.Status, "status");
        Equal(
            BentZTransitionFailure.InsufficientHorizontalRun,
            result.Failure,
            "failure");
    }

    private static void ElevationToleranceIsApplied()
    {
        BentZTransitionResult result = BentZTransitionGeometry.Plan(
            new BentZTransitionInput(
                0.0,
                4.0,
                8.0,
                12.0,
                2.0,
                2.009,
                0.75,
                0.25,
                0.01));

        Equal(BentZTransitionStatus.NotApplicable, result.Status, "status");
    }

    private static void InvalidToleranceIsUnsupported()
    {
        BentZTransitionResult result = BentZTransitionGeometry.Plan(
            new BentZTransitionInput(
                0.0,
                4.0,
                8.0,
                12.0,
                0.0,
                3.0,
                0.75,
                0.25,
                -0.01));

        Equal(BentZTransitionStatus.Unsupported, result.Status, "status");
        Equal(BentZTransitionFailure.InvalidTolerance, result.Failure, "failure");
    }

    private static void RoundedBendsAcceptConservativeInset()
    {
        BentZTransitionResult transition = Plan(0.0, 3.0);
        BentZBendValidationResult result =
            BentZTransitionGeometry.ValidateRoundedBends(
                transition.Points,
                0.75,
                0.25,
                0.5,
                0.25,
                0.01);

        Equal(true, result.IsValid, "rounded bend validity");
        Equal(
            BentZBendValidationFailure.None,
            result.Failure,
            "rounded bend failure");
        if (result.TangentSetback <= 0.0
            || result.RemainingDiagonalStraight <= 0.0)
        {
            throw new InvalidOperationException(
                "Rounded bend metrics must remain positive.");
        }
        double expectedAngle = Math.Atan2(3.0, 5.5);
        Near(expectedAngle, result.AngleRadians, "rounded bend angle");
        Near(
            0.5 * Math.Tan(expectedAngle / 2.0),
            result.TangentSetback,
            "rounded bend tangent");
    }

    private static void RoundedBendsRejectInsufficientFaceInset()
    {
        BentZTransitionResult transition = Plan(0.0, 3.0);
        BentZBendValidationResult result =
            BentZTransitionGeometry.ValidateRoundedBends(
                transition.Points,
                0.35,
                0.25,
                0.5,
                0.25,
                0.01);

        Equal(false, result.IsValid, "rounded bend validity");
        Equal(
            BentZBendValidationFailure.InsufficientFaceInset,
            result.Failure,
            "rounded bend failure");
    }

    private static void RoundedBendsRejectInsufficientTangentLength()
    {
        var points = new List<BentZStationPoint>
        {
            new BentZStationPoint(0.0, 0.0),
            new BentZStationPoint(1.0, 0.0),
            new BentZStationPoint(1.2, 0.2),
            new BentZStationPoint(2.2, 0.2)
        };
        BentZBendValidationResult result =
            BentZTransitionGeometry.ValidateRoundedBends(
                points,
                0.5,
                0.1,
                0.5,
                0.1,
                0.001);

        Equal(false, result.IsValid, "rounded bend validity");
        Equal(
            BentZBendValidationFailure.InsufficientTangentLength,
            result.Failure,
            "rounded bend failure");
    }

    private static void RoundedBendValidationIsDirectionIndependent()
    {
        BentZTransitionResult forward = Plan(0.0, 3.0);
        BentZTransitionResult reverse = BentZTransitionGeometry.Plan(
            new BentZTransitionInput(
                12.0,
                8.0,
                4.0,
                0.0,
                3.0,
                0.0,
                0.75,
                0.25,
                0.01));
        BentZBendValidationResult forwardResult =
            BentZTransitionGeometry.ValidateRoundedBends(
                forward.Points,
                0.75,
                0.25,
                0.5,
                0.25,
                0.01);
        BentZBendValidationResult reverseResult =
            BentZTransitionGeometry.ValidateRoundedBends(
                reverse.Points,
                0.75,
                0.25,
                0.5,
                0.25,
                0.01);

        Equal(true, forwardResult.IsValid, "forward rounded bend");
        Equal(true, reverseResult.IsValid, "reverse rounded bend");
        Near(
            forwardResult.TangentSetback,
            reverseResult.TangentSetback,
            "direction-independent tangent setback");
    }

    private static void RoundedBendsRejectZigZagStations()
    {
        var points = new List<BentZStationPoint>
        {
            new BentZStationPoint(0.0, 0.0),
            new BentZStationPoint(2.0, 0.0),
            new BentZStationPoint(1.0, 1.0),
            new BentZStationPoint(3.0, 1.0)
        };
        BentZBendValidationResult result =
            BentZTransitionGeometry.ValidateRoundedBends(
                points,
                1.0,
                0.1,
                0.5,
                0.1,
                0.001);

        Equal(false, result.IsValid, "zig-zag bend validity");
        Equal(
            BentZBendValidationFailure.InvalidPointChain,
            result.Failure,
            "zig-zag bend failure");
    }

    private static void RoundedBendsRejectNonHorizontalOuterLeg()
    {
        var points = new List<BentZStationPoint>
        {
            new BentZStationPoint(0.0, 0.0),
            new BentZStationPoint(2.0, 0.1),
            new BentZStationPoint(4.0, 1.0),
            new BentZStationPoint(6.0, 1.0)
        };
        BentZBendValidationResult result =
            BentZTransitionGeometry.ValidateRoundedBends(
                points,
                1.0,
                0.1,
                0.5,
                0.1,
                0.001);

        Equal(false, result.IsValid, "outer-leg bend validity");
        Equal(
            BentZBendValidationFailure.InvalidPointChain,
            result.Failure,
            "outer-leg bend failure");
    }

    private static void LaneSetAcceptsUniqueLanesInAnyOrder()
    {
        var lanes = new List<BentZLanePair>
        {
            new BentZLanePair(2.0, 2.0001),
            new BentZLanePair(0.0, 0.0001),
            new BentZLanePair(1.0, 1.0001)
        };
        BentZLaneSetValidationResult result =
            BentZTransitionGeometry.ValidateLaneSet(
                lanes,
                3,
                0.001);

        Equal(true, result.IsValid, "lane set validity");
        Equal(
            BentZLaneSetValidationFailure.None,
            result.Failure,
            "lane set failure");
    }

    private static void LaneSetRejectsDuplicateLanes()
    {
        var lanes = new List<BentZLanePair>
        {
            new BentZLanePair(0.0, 0.0),
            new BentZLanePair(0.0005, 0.0005)
        };
        BentZLaneSetValidationResult result =
            BentZTransitionGeometry.ValidateLaneSet(
                lanes,
                2,
                0.001);

        Equal(false, result.IsValid, "lane set validity");
        Equal(
            BentZLaneSetValidationFailure.DuplicateLane,
            result.Failure,
            "lane set failure");
    }

    private static void LaneSetRejectsMissingLane()
    {
        BentZLaneSetValidationResult result =
            BentZTransitionGeometry.ValidateLaneSet(
                new[]
                {
                    new BentZLanePair(0.0, 0.0),
                    new BentZLanePair(1.0, 1.0)
                },
                3,
                0.001);

        Equal(false, result.IsValid, "lane set validity");
        Equal(
            BentZLaneSetValidationFailure.LaneCountMismatch,
            result.Failure,
            "lane set failure");
    }

    private static void LaneSetRejectsTransverseDrift()
    {
        BentZLaneSetValidationResult result =
            BentZTransitionGeometry.ValidateLaneSet(
                new[]
                {
                    new BentZLanePair(0.0, 0.01)
                },
                1,
                0.001);

        Equal(false, result.IsValid, "lane set validity");
        Equal(
            BentZLaneSetValidationFailure.TransverseLaneMismatch,
            result.Failure,
            "lane set failure");
    }

    private static void LaneSetRejectsInsufficientBarSpacing()
    {
        BentZLaneSetValidationResult result =
            BentZTransitionGeometry.ValidateLaneSet(
                new[]
                {
                    new BentZLanePair(0.0, 0.0),
                    new BentZLanePair(0.1, 0.1)
                },
                2,
                0.001,
                0.5);

        Equal(false, result.IsValid, "lane set validity");
        Equal(
            BentZLaneSetValidationFailure.InsufficientLaneSpacing,
            result.Failure,
            "lane set failure");
    }

    private static void IndependentAnchorageUsesCallerSuppliedThirtyFiveDiameters()
    {
        const double barDiameter = 0.2;
        double requiredLength = 35.0 * barDiameter;
        IndependentJointAnchorageResult result =
            IndependentJointAnchorageGeometry.Plan(
                IndependentInput(
                    requiredAnchorageLength: requiredLength));

        Equal(
            IndependentJointAnchorageStatus.Planned,
            result.Status,
            "independent status");
        Equal(
            IndependentJointAnchorageFailure.None,
            result.Failure,
            "independent failure");
        Equal(2, result.StraightThroughPoints.Count, "straight point count");
        Equal(3, result.BentVerticalPoints.Count, "bent point count");
        Near(
            requiredLength,
            result.StraightProvidedAnchorageLength,
            "straight 35D");
        Near(
            requiredLength,
            result.BentProvidedAnchorageLength,
            "bent 35D");

        Near(
            16.0,
            result.StraightThroughPoints[0].Station,
            "straight shallow start");
        Near(
            3.0,
            result.StraightThroughPoints[1].Station,
            "straight deep anchor end");
        Near(
            9.0,
            result.BentVerticalPoints[1].Station,
            "bent station");
        Near(
            7.0,
            result.BentVerticalPoints[2].Elevation,
            "bent vertical end");
        Near(0.5, result.TangentSetback, "90-degree tangent setback");
    }

    private static void IndependentAnchorageMirrorsBeamOrder()
    {
        IndependentJointAnchorageResult forward =
            IndependentJointAnchorageGeometry.Plan(
                IndependentInput());
        IndependentJointAnchorageResult reverse =
            IndependentJointAnchorageGeometry.Plan(
                new IndependentJointAnchorageInput(
                    16.0,
                    10.0,
                    6.0,
                    0.0,
                    0.0,
                    3.0,
                    10.0,
                    7.0,
                    1.0,
                    0.25,
                    0.5,
                    0.25,
                    0.001));

        Equal(
            IndependentJointAnchorageStatus.Planned,
            forward.Status,
            "forward status");
        Equal(
            IndependentJointAnchorageStatus.Planned,
            reverse.Status,
            "reverse status");

        for (int index = 0;
            index < forward.StraightThroughPoints.Count;
            index++)
        {
            Near(
                16.0 - forward.StraightThroughPoints[index].Station,
                reverse.StraightThroughPoints[index].Station,
                "mirrored straight station " + index);
            Near(
                forward.StraightThroughPoints[index].Elevation,
                reverse.StraightThroughPoints[index].Elevation,
                "mirrored straight elevation " + index);
        }
        for (int index = 0;
            index < forward.BentVerticalPoints.Count;
            index++)
        {
            Near(
                16.0 - forward.BentVerticalPoints[index].Station,
                reverse.BentVerticalPoints[index].Station,
                "mirrored bent station " + index);
            Near(
                forward.BentVerticalPoints[index].Elevation,
                reverse.BentVerticalPoints[index].Elevation,
                "mirrored bent elevation " + index);
        }
    }

    private static void IndependentAnchorageSupportsOppositeVerticalDirection()
    {
        IndependentJointAnchorageResult result =
            IndependentJointAnchorageGeometry.Plan(
                new IndependentJointAnchorageInput(
                    0.0,
                    6.0,
                    10.0,
                    16.0,
                    5.0,
                    2.0,
                    -5.0,
                    7.0,
                    1.0,
                    0.25,
                    0.5,
                    0.25,
                    0.001));

        Equal(
            IndependentJointAnchorageStatus.Planned,
            result.Status,
            "downward status");
        Near(
            -2.0,
            result.BentVerticalPoints[2].Elevation,
            "downward bent endpoint");
    }

    private static void IndependentAnchorageRejectsNonFiniteInput()
    {
        IndependentJointAnchorageResult result =
            IndependentJointAnchorageGeometry.Plan(
                IndependentInput(
                    requiredAnchorageLength: double.NaN));

        Equal(
            IndependentJointAnchorageStatus.Unsupported,
            result.Status,
            "non-finite status");
        Equal(
            IndependentJointAnchorageFailure.NonFiniteValue,
            result.Failure,
            "non-finite failure");
    }

    private static void IndependentAnchorageRejectsNonMonotonicStations()
    {
        IndependentJointAnchorageResult result =
            IndependentJointAnchorageGeometry.Plan(
                new IndependentJointAnchorageInput(
                    0.0,
                    10.0,
                    6.0,
                    16.0,
                    0.0,
                    3.0,
                    10.0,
                    7.0,
                    1.0,
                    0.25,
                    0.5,
                    0.25,
                    0.001));

        Equal(
            IndependentJointAnchorageStatus.Unsupported,
            result.Status,
            "non-monotonic status");
        Equal(
            IndependentJointAnchorageFailure.NonMonotonicStations,
            result.Failure,
            "non-monotonic failure");
    }

    private static void IndependentAnchorageRejectsMissingStraightAvailability()
    {
        IndependentJointAnchorageResult result =
            IndependentJointAnchorageGeometry.Plan(
                IndependentInput(requiredAnchorageLength: 11.0));

        Equal(
            IndependentJointAnchorageStatus.Unsupported,
            result.Status,
            "straight availability status");
        Equal(
            IndependentJointAnchorageFailure
                .InsufficientStraightAnchorAvailability,
            result.Failure,
            "straight availability failure");
    }

    private static void IndependentAnchorageMustCrossTheJoint()
    {
        IndependentJointAnchorageResult result =
            IndependentJointAnchorageGeometry.Plan(
                IndependentInput(requiredAnchorageLength: 4.0));

        Equal(
            IndependentJointAnchorageStatus.Unsupported,
            result.Status,
            "joint-crossing status");
        Equal(
            IndependentJointAnchorageFailure
                .StraightAnchorDoesNotCrossJoint,
            result.Failure,
            "joint-crossing failure");
    }

    private static void IndependentAnchorageRejectsMissingVerticalAvailability()
    {
        IndependentJointAnchorageResult result =
            IndependentJointAnchorageGeometry.Plan(
                IndependentInput(bentVerticalLimitElevation: 6.0));

        Equal(
            IndependentJointAnchorageStatus.Unsupported,
            result.Status,
            "vertical availability status");
        Equal(
            IndependentJointAnchorageFailure
                .InsufficientBentAnchorAvailability,
            result.Failure,
            "vertical availability failure");
    }

    private static void IndependentAnchorageRejectsInsufficientFaceInset()
    {
        IndependentJointAnchorageResult result =
            IndependentJointAnchorageGeometry.Plan(
                IndependentInput(bendInsetFromShallowFace: 0.5));

        Equal(
            IndependentJointAnchorageStatus.Unsupported,
            result.Status,
            "face-inset status");
        Equal(
            IndependentJointAnchorageFailure.InsufficientBendFaceInset,
            result.Failure,
            "face-inset failure");
    }

    private static void IndependentAnchorageRejectsInsufficientRoundedBendLeg()
    {
        IndependentJointAnchorageResult result =
            IndependentJointAnchorageGeometry.Plan(
                new IndependentJointAnchorageInput(
                    0.0,
                    6.0,
                    10.0,
                    16.0,
                    0.0,
                    3.0,
                    10.0,
                    7.0,
                    1.0,
                    0.25,
                    0.5,
                    7.0,
                    0.001));

        Equal(
            IndependentJointAnchorageStatus.Unsupported,
            result.Status,
            "rounded-leg status");
        Equal(
            IndependentJointAnchorageFailure.InsufficientTangentLength,
            result.Failure,
            "rounded-leg failure");
    }

    private static void IndependentAnchorageValidationRejectsShortenedStraightRun()
    {
        IndependentJointAnchorageInput input = IndependentInput();
        IndependentJointAnchorageResult plan =
            IndependentJointAnchorageGeometry.Plan(input);
        var shortenedStraight = new[]
        {
            plan.StraightThroughPoints[0],
            new BentZStationPoint(4.0, 3.0)
        };
        IndependentJointAnchorageValidationResult validation =
            IndependentJointAnchorageGeometry.Validate(
                input,
                shortenedStraight,
                plan.BentVerticalPoints);

        Equal(false, validation.IsValid, "shortened straight validity");
        Equal(
            IndependentJointAnchorageFailure
                .InsufficientProvidedAnchorage,
            validation.Failure,
            "shortened straight failure");
    }

    private static void IndependentAnchorageValidationRejectsDiagonalBentRun()
    {
        IndependentJointAnchorageInput input = IndependentInput();
        IndependentJointAnchorageResult plan =
            IndependentJointAnchorageGeometry.Plan(input);
        var diagonalBent = new[]
        {
            plan.BentVerticalPoints[0],
            new BentZStationPoint(9.0, 0.1),
            plan.BentVerticalPoints[2]
        };
        IndependentJointAnchorageValidationResult validation =
            IndependentJointAnchorageGeometry.Validate(
                input,
                plan.StraightThroughPoints,
                diagonalBent);

        Equal(false, validation.IsValid, "diagonal bent validity");
        Equal(
            IndependentJointAnchorageFailure.InvalidPointChain,
            validation.Failure,
            "diagonal bent failure");
    }

    private static void IndependentAnchorageValidationAcceptsLongerRuns()
    {
        IndependentJointAnchorageInput input = IndependentInput();
        var longerStraight = new[]
        {
            new BentZStationPoint(16.0, 3.0),
            new BentZStationPoint(2.0, 3.0)
        };
        var longerBent = new[]
        {
            new BentZStationPoint(0.0, 0.0),
            new BentZStationPoint(9.0, 0.0),
            new BentZStationPoint(9.0, 8.0)
        };
        IndependentJointAnchorageValidationResult validation =
            IndependentJointAnchorageGeometry.Validate(
                input,
                longerStraight,
                longerBent);

        Equal(true, validation.IsValid, "longer-run validity");
        Near(8.0, validation.StraightProvidedAnchorageLength, "long straight");
        Near(8.0, validation.BentProvidedAnchorageLength, "long bent");
    }

    private static void LaneStaggerKeepsAlreadySafeLanes()
    {
        IndependentJointLaneStaggerResult result =
            IndependentJointLaneStaggerGeometry.Plan(
                LaneStaggerInput(
                    new[] { -2.0, 2.0 },
                    new[] { 0.0 },
                    -3.0,
                    3.0,
                    1.0));

        Equal(
            IndependentJointLaneStaggerStatus.Planned,
            result.Status,
            "safe lane status");
        Near(-2.0, result.ShiftedBentLaneYs[0], "safe lane zero");
        Near(2.0, result.ShiftedBentLaneYs[1], "safe lane one");
        Near(0.0, result.TotalAbsoluteDisplacement, "safe lane movement");
    }

    private static void LaneStaggerMovesToTheOnlyAvailableSide()
    {
        IndependentJointLaneStaggerResult result =
            IndependentJointLaneStaggerGeometry.Plan(
                LaneStaggerInput(
                    new[] { 0.0 },
                    new[] { 0.0 },
                    0.0,
                    3.0,
                    1.0));

        Equal(
            IndependentJointLaneStaggerStatus.Planned,
            result.Status,
            "one-sided status");
        Near(1.0, result.ShiftedBentLaneYs[0], "one-sided lane");
        Near(1.0, result.TotalAbsoluteDisplacement, "one-sided movement");
        Near(1.0, result.MaximumAbsoluteDisplacement, "one-sided maximum");
    }

    private static void LaneStaggerIsInputOrderIndependent()
    {
        IndependentJointLaneStaggerResult first =
            IndependentJointLaneStaggerGeometry.Plan(
                LaneStaggerInput(
                    new[] { 2.0, 0.0 },
                    new[] { 0.0 },
                    0.0,
                    5.0,
                    1.0));
        IndependentJointLaneStaggerResult second =
            IndependentJointLaneStaggerGeometry.Plan(
                LaneStaggerInput(
                    new[] { 0.0, 2.0 },
                    new[] { 0.0 },
                    0.0,
                    5.0,
                    1.0));

        Equal(
            IndependentJointLaneStaggerStatus.Planned,
            first.Status,
            "first order status");
        Equal(
            IndependentJointLaneStaggerStatus.Planned,
            second.Status,
            "second order status");
        Near(2.0, first.ShiftedBentLaneYs[0], "first order lane zero");
        Near(1.0, first.ShiftedBentLaneYs[1], "first order lane one");
        Near(1.0, second.ShiftedBentLaneYs[0], "second order lane zero");
        Near(2.0, second.ShiftedBentLaneYs[1], "second order lane one");
    }

    private static void LaneStaggerMirrorsOneSidedLayout()
    {
        IndependentJointLaneStaggerResult forward =
            IndependentJointLaneStaggerGeometry.Plan(
                LaneStaggerInput(
                    new[] { 0.0, 2.0 },
                    new[] { 0.0 },
                    0.0,
                    5.0,
                    1.0));
        IndependentJointLaneStaggerResult mirrored =
            IndependentJointLaneStaggerGeometry.Plan(
                LaneStaggerInput(
                    new[] { 0.0, -2.0 },
                    new[] { 0.0 },
                    -5.0,
                    0.0,
                    1.0));

        Equal(
            IndependentJointLaneStaggerStatus.Planned,
            mirrored.Status,
            "mirrored status");
        Near(
            -forward.ShiftedBentLaneYs[0],
            mirrored.ShiftedBentLaneYs[0],
            "mirrored lane zero");
        Near(
            -forward.ShiftedBentLaneYs[1],
            mirrored.ShiftedBentLaneYs[1],
            "mirrored lane one");
    }

    private static void LaneStaggerEnforcesBentLaneSpacing()
    {
        IndependentJointLaneStaggerResult result =
            IndependentJointLaneStaggerGeometry.Plan(
                LaneStaggerInput(
                    new[] { 0.0, 0.2 },
                    new double[0],
                    0.0,
                    3.0,
                    1.0));

        Equal(
            IndependentJointLaneStaggerStatus.Planned,
            result.Status,
            "bent spacing status");
        Near(0.0, result.ShiftedBentLaneYs[0], "bent spacing first");
        Near(1.0, result.ShiftedBentLaneYs[1], "bent spacing second");
    }

    private static void LaneStaggerRejectsInsufficientWidth()
    {
        IndependentJointLaneStaggerResult result =
            IndependentJointLaneStaggerGeometry.Plan(
                LaneStaggerInput(
                    new[] { 0.0, 0.2 },
                    new double[0],
                    0.0,
                    0.5,
                    1.0));

        Equal(
            IndependentJointLaneStaggerStatus.Unsupported,
            result.Status,
            "insufficient width status");
        Equal(
            IndependentJointLaneStaggerFailure.NoFeasibleLayout,
            result.Failure,
            "insufficient width failure");
    }

    private static void LaneStaggerPlansSymmetricThreeLaneLayout()
    {
        var originalLanes = new[] { -2.0, 0.0, 2.0 };
        IndependentJointLaneStaggerResult result =
            IndependentJointLaneStaggerGeometry.Plan(
                LaneStaggerInput(
                    originalLanes,
                    originalLanes,
                    -3.0,
                    3.0,
                    0.25,
                    1.0));

        Equal(
            IndependentJointLaneStaggerStatus.Planned,
            result.Status,
            "symmetric status");
        for (int index = 0; index < originalLanes.Length; index++)
        {
            Near(
                originalLanes[index] + 0.25,
                result.ShiftedBentLaneYs[index],
                "symmetric shifted lane " + index);
        }

        IndependentJointLaneStaggerValidationResult validation =
            IndependentJointLaneStaggerGeometry.Validate(
                LaneStaggerInput(
                    originalLanes,
                    originalLanes,
                    -3.0,
                    3.0,
                    0.25,
                    1.0),
                result.ShiftedBentLaneYs);
        Equal(true, validation.IsValid, "symmetric validation");
    }

    private static void LaneStaggerPlansCoverBoundedThreeLaneLayout()
    {
        var lanes = new[] { 0.0, 143.25, 286.5 };
        IndependentJointLaneStaggerResult result =
            IndependentJointLaneStaggerGeometry.Plan(
                LaneStaggerInput(
                    lanes,
                    lanes,
                    0.0,
                    286.5,
                    19.2,
                    1.0));

        Equal(
            IndependentJointLaneStaggerStatus.Planned,
            result.Status,
            "cover-bounded status");
        IndependentJointLaneStaggerValidationResult validation =
            IndependentJointLaneStaggerGeometry.Validate(
                LaneStaggerInput(
                    lanes,
                    lanes,
                    0.0,
                    286.5,
                    19.2,
                    1.0),
                result.ShiftedBentLaneYs);
        Equal(true, validation.IsValid, "cover-bounded validation");
    }

    private static void
        LaneStaggerMirrorsSymmetricLayoutWithOppositePreference()
    {
        var lanes = new[] { -2.0, 0.0, 2.0 };
        IndependentJointLaneStaggerResult positive =
            IndependentJointLaneStaggerGeometry.Plan(
                LaneStaggerInput(
                    lanes,
                    lanes,
                    -3.0,
                    3.0,
                    0.25,
                    1.0));
        IndependentJointLaneStaggerResult negative =
            IndependentJointLaneStaggerGeometry.Plan(
                LaneStaggerInput(
                    lanes,
                    lanes,
                    -3.0,
                    3.0,
                    0.25,
                    -1.0));

        Equal(
            IndependentJointLaneStaggerStatus.Planned,
            negative.Status,
            "negative-preference status");
        for (int index = 0; index < lanes.Length; index++)
        {
            int mirroredIndex = lanes.Length - 1 - index;
            Near(
                -positive.ShiftedBentLaneYs[index],
                negative.ShiftedBentLaneYs[mirroredIndex],
                "preferred mirror lane " + index);
        }
    }

    private static void LaneStaggerValidationRejectsStraightLaneClash()
    {
        IndependentJointLaneStaggerInput input = LaneStaggerInput(
            new[] { 0.0 },
            new[] { 0.0 },
            -2.0,
            2.0,
            1.0);
        IndependentJointLaneStaggerValidationResult validation =
            IndependentJointLaneStaggerGeometry.Validate(
                input,
                new[] { 0.5 });

        Equal(false, validation.IsValid, "straight clash validity");
        Equal(
            IndependentJointLaneStaggerFailure
                .InsufficientStraightLaneSeparation,
            validation.Failure,
            "straight clash failure");
    }

    private static void LaneStaggerValidationRejectsBentLaneClash()
    {
        IndependentJointLaneStaggerInput input = LaneStaggerInput(
            new[] { -0.5, 0.5 },
            new double[0],
            -2.0,
            2.0,
            1.0);
        IndependentJointLaneStaggerValidationResult validation =
            IndependentJointLaneStaggerGeometry.Validate(
                input,
                new[] { 0.0, 0.5 });

        Equal(false, validation.IsValid, "bent clash validity");
        Equal(
            IndependentJointLaneStaggerFailure
                .InsufficientBentLaneSeparation,
            validation.Failure,
            "bent clash failure");
    }

    private static void LaneStaggerValidationRejectsWrongCount()
    {
        IndependentJointLaneStaggerInput input = LaneStaggerInput(
            new[] { -0.5, 0.5 },
            new double[0],
            -2.0,
            2.0,
            1.0);
        IndependentJointLaneStaggerValidationResult validation =
            IndependentJointLaneStaggerGeometry.Validate(
                input,
                new[] { -0.5 });

        Equal(false, validation.IsValid, "wrong count validity");
        Equal(
            IndependentJointLaneStaggerFailure.OutputCountMismatch,
            validation.Failure,
            "wrong count failure");
    }

    private static IndependentJointLaneStaggerInput LaneStaggerInput(
        IReadOnlyList<double> bentLaneYs,
        IReadOnlyList<double> straightLaneYs,
        double minAllowedY,
        double maxAllowedY,
        double requiredSeparation,
        double preferredShiftDirection = 1.0)
    {
        return new IndependentJointLaneStaggerInput(
            bentLaneYs,
            straightLaneYs,
            minAllowedY,
            maxAllowedY,
            requiredSeparation,
            preferredShiftDirection,
            0.001);
    }

    private static IndependentJointAnchorageInput IndependentInput(
        double requiredAnchorageLength = 7.0,
        double bentVerticalLimitElevation = 10.0,
        double bendInsetFromShallowFace = 1.0)
    {
        return new IndependentJointAnchorageInput(
            0.0,
            6.0,
            10.0,
            16.0,
            0.0,
            3.0,
            bentVerticalLimitElevation,
            requiredAnchorageLength,
            bendInsetFromShallowFace,
            0.25,
            0.5,
            0.25,
            0.001);
    }

    private static BentZTransitionResult Plan(
        double startElevation,
        double endElevation)
    {
        return BentZTransitionGeometry.Plan(
            new BentZTransitionInput(
                0.0,
                4.0,
                8.0,
                12.0,
                startElevation,
                endElevation,
                0.75,
                0.25,
                0.01));
    }

    private static void Near(double expected, double actual, string label)
    {
        if (Math.Abs(expected - actual) > Epsilon)
        {
            throw new InvalidOperationException(
                label + ": expected " + expected + ", actual " + actual + ".");
        }
    }

    private static void Equal<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
