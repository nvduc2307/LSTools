namespace LSTool.Tools.Beams.BeamRebar.models
{
    /// <summary>
    /// Tên các Revit shared parameter đọc thép từ FamilyInstance dầm.
    /// </summary>
    public static class BeamRebarParameterName
    {
        // ── Đai ──────────────────────────────────────────────────────────────
        public const string LS_ST_Diameter       = "LS_ST_Diameter";
        public const string LS_ST_Spacing_Start  = "LS_ST_Spacing_Start";
        public const string LS_ST_Spacing_Mid    = "LS_ST_Spacing_Mid";
        public const string LS_ST_Spacing_End    = "LS_ST_Spacing_End";

        // ── Thép trên (Top) ───────────────────────────────────────────────
        public const string LS_TOP1_Diameter     = "LS_TOP1_Diameter";
        public const string LS_TOP1_Count        = "LS_TOP1_Count";
        public const string LS_TOP2_Diameter     = "LS_TOP2_Diameter";
        public const string LS_TOP2_Count        = "LS_TOP2_Count";
        public const string LS_TOP3_Diameter     = "LS_TOP3_Diameter";
        public const string LS_TOP3_Count        = "LS_TOP3_Count";

        // ── Thép dưới (Bottom) ────────────────────────────────────────────
        public const string LS_BOT1_Diameter     = "LS_BOT1_Diameter";
        public const string LS_BOT1_Count        = "LS_BOT1_Count";
        public const string LS_BOT2_Diameter     = "LS_BOT2_Diameter";
        public const string LS_BOT2_Count        = "LS_BOT2_Count";
        public const string LS_BOT3_Diameter     = "LS_BOT3_Diameter";
        public const string LS_BOT3_Count        = "LS_BOT3_Count";

        // ── Thép hông (Side) ──────────────────────────────────────────────
        public const string LS_SIDEBAR_Diameter  = "LS_SIDEBAR_Diameter";
        public const string LS_SIDEBAR_Count     = "LS_SIDEBAR_Count";
    }
}
