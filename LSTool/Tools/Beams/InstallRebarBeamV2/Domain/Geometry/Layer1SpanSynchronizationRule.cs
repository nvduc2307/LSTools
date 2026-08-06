namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    public enum Layer1BarFace
    {
        Top = 0,
        Bottom = 1
    }

    public enum Layer1SectionSlot
    {
        Start = 0,
        Mid = 1,
        End = 2
    }

    public enum Layer1SynchronizedValue
    {
        Diameter = 0,
        Quantity = 1
    }

    public static class Layer1SpanSynchronizationRule
    {
        public static bool IncludesTarget(
            Layer1BarFace sourceFace,
            Layer1SectionSlot sourceSection,
            Layer1BarFace targetFace,
            Layer1SectionSlot targetSection,
            Layer1SynchronizedValue value)
        {
            if (sourceFace != targetFace)
                return false;

            switch (value)
            {
                case Layer1SynchronizedValue.Diameter:
                    return true;
                case Layer1SynchronizedValue.Quantity:
                    return sourceSection == targetSection;
                default:
                    return false;
            }
        }
    }
}
