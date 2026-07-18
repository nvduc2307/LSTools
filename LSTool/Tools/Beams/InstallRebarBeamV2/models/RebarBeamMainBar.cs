using CommunityToolkit.Mvvm.ComponentModel;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public partial class RebarBeamMainBar : RebarBaseInfo
    {
        [ObservableProperty]
        private int _rebarGroupType;
        [ObservableProperty]
        private int _rebarLevelType;
    }
    public partial class RebarBeamTop : ObservableObject
    {
        private RebarBeamMainBarGroup _rebarGroupTypeActive;
        public List<RebarBeamMainBarGroup> RebarBeamMainBarGroups { get; set; }
        public RebarBeamMainBarGroup RebarGroupTypeActive
        {
            get => _rebarGroupTypeActive;
            set
            {
                _rebarGroupTypeActive = value;
                OnPropertyChanged();
                RebarGroupTypeChange?.Invoke();
            }
        }
        public RebarBeamMainBar RebarBeamTopLevel1 { get; set; }
        public RebarBeamMainBar RebarBeamTopLevel2 { get; set; }
        public RebarBeamMainBar RebarBeamTopLevel3 { get; set; }
        [ObservableProperty]
        private RebarBeamMainBar _rebarBeamTopLevelActive;
        public Action RebarGroupTypeChange { get; set; }
        public static void TopRebarGroupTypeChangeFunc(RebarBeamTop rebarBeam)
        {
            switch ((RebarBeamMainBarGroupType)rebarBeam.RebarGroupTypeActive.Id)
            {
                case RebarBeamMainBarGroupType.GroupLevel1:
                    rebarBeam.RebarBeamTopLevelActive = rebarBeam.RebarBeamTopLevel1;
                    break;
                case RebarBeamMainBarGroupType.GroupLevel2:
                    rebarBeam.RebarBeamTopLevelActive = rebarBeam.RebarBeamTopLevel2;
                    break;
                case RebarBeamMainBarGroupType.GroupLevel3:
                    rebarBeam.RebarBeamTopLevelActive = rebarBeam.RebarBeamTopLevel3;
                    break;
            }
        }
        public static void TopRebarGroupTypeChangeFunc(
            RebarBeamSection rebarBeamSectionTarget, 
            RebarBeam rebarBeam)
        {
            try
            {
                TopRebarGroupTypeChangeFunc(rebarBeamSectionTarget.RebarBeamTop);
                var rebarGroupTypeActive = rebarBeamSectionTarget.RebarBeamTop.RebarGroupTypeActive.Id;
                var rebarSectionType = (RebarBeamSectionType)rebarBeamSectionTarget.RebarBeamSectionType;
                var rebarBeamSections = GetRebarBeamTop(rebarSectionType, rebarBeam);
                foreach (var rebarBeamSection in rebarBeamSections)
                { 
                    rebarBeamSection.RebarBeamTop.RebarGroupTypeChange = null;
                    rebarBeamSection.RebarBeamTop.RebarGroupTypeActive =
                        rebarBeamSection.RebarBeamTop.RebarBeamMainBarGroups.FirstOrDefault(x => x.Id == rebarGroupTypeActive);
                    TopRebarGroupTypeChangeFunc(rebarBeamSection.RebarBeamTop);
                    rebarBeamSection.RebarBeamTop.RebarGroupTypeChange = () =>
                    {
                        TopRebarGroupTypeChangeFunc(rebarBeamSection, rebarBeam);
                    };
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to synchronize the selected top-bar group across beam sections.", ex);
            }
            List<RebarBeamSection> GetRebarBeamTop(
                RebarBeamSectionType rebarBeamSectionType, 
                RebarBeam rebarBeam)
            {
                try
                {
                    var results = new List<RebarBeamSection>();
                    switch (rebarBeamSectionType)
                    {
                        case RebarBeamSectionType.SectionStart:
                            results.Add(rebarBeam.RebarBeamSectionMid);
                            results.Add(rebarBeam.RebarBeamSectionEnd);
                            break;
                        case RebarBeamSectionType.SectionMid:
                            results.Add(rebarBeam.RebarBeamSectionStart);
                            results.Add(rebarBeam.RebarBeamSectionEnd);
                            break;
                        case RebarBeamSectionType.SectionEnd:
                            results.Add(rebarBeam.RebarBeamSectionStart);
                            results.Add(rebarBeam.RebarBeamSectionMid);
                            break;
                    }
                    return results;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "Failed to resolve the remaining beam sections for top-bar synchronization.", ex);
                }
            }
        }
    }
    public partial class RebarBeamBot : ObservableObject
    {
        private RebarBeamMainBarGroup _rebarGroupTypeActive;
        public List<RebarBeamMainBarGroup> RebarBeamMainBarGroups { get; set; }
        public RebarBeamMainBarGroup RebarGroupTypeActive
        {
            get => _rebarGroupTypeActive;
            set
            {
                _rebarGroupTypeActive = value;
                OnPropertyChanged();
                RebarGroupTypeChange?.Invoke();
            }
        }
        public RebarBeamMainBar RebarBeamBotLevel1 { get; set; }
        public RebarBeamMainBar RebarBeamBotLevel2 { get; set; }
        public RebarBeamMainBar RebarBeamBotLevel3 { get; set; }
        [ObservableProperty]
        private RebarBeamMainBar _rebarBeamBotLevelActive;
        public Action RebarGroupTypeChange { get; set; }
        public static void BotRebarGroupTypeChangeFunc(RebarBeamBot rebarBeam)
        {
            switch ((RebarBeamMainBarGroupType)rebarBeam.RebarGroupTypeActive.Id)
            {
                case RebarBeamMainBarGroupType.GroupLevel1:
                    rebarBeam.RebarBeamBotLevelActive = rebarBeam.RebarBeamBotLevel1;
                    break;
                case RebarBeamMainBarGroupType.GroupLevel2:
                    rebarBeam.RebarBeamBotLevelActive = rebarBeam.RebarBeamBotLevel2;
                    break;
                case RebarBeamMainBarGroupType.GroupLevel3:
                    rebarBeam.RebarBeamBotLevelActive = rebarBeam.RebarBeamBotLevel3;
                    break;
            }
        }
        public static void BotRebarGroupTypeChangeFunc(
            RebarBeamSection rebarBeamSectionTarget,
            RebarBeam rebarBeam)
        {
            try
            {
                BotRebarGroupTypeChangeFunc(rebarBeamSectionTarget.RebarBeamBot);
                var rebarGroupTypeActive = rebarBeamSectionTarget.RebarBeamBot.RebarGroupTypeActive.Id;
                var rebarSectionType = (RebarBeamSectionType)rebarBeamSectionTarget.RebarBeamSectionType;
                var rebarBeamSections = GetRebarBeamBot(rebarSectionType, rebarBeam);
                foreach (var rebarBeamSection in rebarBeamSections)
                {
                    rebarBeamSection.RebarBeamBot.RebarGroupTypeChange = null;
                    rebarBeamSection.RebarBeamBot.RebarGroupTypeActive =
                        rebarBeamSection.RebarBeamBot.RebarBeamMainBarGroups.FirstOrDefault(x => x.Id == rebarGroupTypeActive);
                    BotRebarGroupTypeChangeFunc(rebarBeamSection.RebarBeamBot);
                    rebarBeamSection.RebarBeamBot.RebarGroupTypeChange = () =>
                    {
                        BotRebarGroupTypeChangeFunc(rebarBeamSection, rebarBeam);
                    };
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to synchronize the selected bottom-bar group across beam sections.", ex);
            }
            List<RebarBeamSection> GetRebarBeamBot(
                RebarBeamSectionType rebarBeamSectionType,
                RebarBeam rebarBeam)
            {
                try
                {
                    var results = new List<RebarBeamSection>();
                    switch (rebarBeamSectionType)
                    {
                        case RebarBeamSectionType.SectionStart:
                            results.Add(rebarBeam.RebarBeamSectionMid);
                            results.Add(rebarBeam.RebarBeamSectionEnd);
                            break;
                        case RebarBeamSectionType.SectionMid:
                            results.Add(rebarBeam.RebarBeamSectionStart);
                            results.Add(rebarBeam.RebarBeamSectionEnd);
                            break;
                        case RebarBeamSectionType.SectionEnd:
                            results.Add(rebarBeam.RebarBeamSectionStart);
                            results.Add(rebarBeam.RebarBeamSectionMid);
                            break;
                    }
                    return results;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "Failed to resolve the remaining beam sections for bottom-bar synchronization.", ex);
                }
            }
        }
    }
}


