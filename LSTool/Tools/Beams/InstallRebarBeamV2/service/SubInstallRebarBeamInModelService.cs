using Autodesk.Revit.DB;
using LSTool.Compatibility;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using RIMT.Utils;
using RIMT.Utils.BoundingBoxs;
using RIMT.Utils.Compares;
using RIMT.Utils.Geometries;
using RIMT.Utils.RevPoints;
using RIMT.Utils.Solids;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service
{
    public partial class SubInstallRebarBeamInModelService : ISubInstallRebarBeamInModelService
    {
        public List<RebarBeamMainBar> GetRebarBeamAllSection(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            try
            {
                var rebarTop1s = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel1);
                var rebarTop2s = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel2);
                var rebarTop3s = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel3);

                var rebarBot1s = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel1);
                var rebarBot2s = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel2);
                var rebarBot3s = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel3);

                var rebarTop1m = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel1);
                var rebarTop2m = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel2);
                var rebarTop3m = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel3);

                var rebarBot1m = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel1);
                var rebarBot2m = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel2);
                var rebarBot3m = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel3);

                var rebarTop1e = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel1);
                var rebarTop2e = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel2);
                var rebarTop3e = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel3);

                var rebarBot1e = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel1);
                var rebarBot2e = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel2);
                var rebarBot3e = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel3);

                var rebarBeamMains = new List<RebarBeamMainBar>();
                if (rebarTop1s.Any()) rebarBeamMains.AddRange(rebarTop1s);
                if (rebarTop2s.Any()) rebarBeamMains.AddRange(rebarTop2s);
                if (rebarTop3s.Any()) rebarBeamMains.AddRange(rebarTop3s);
                if (rebarBot1s.Any()) rebarBeamMains.AddRange(rebarBot1s);
                if (rebarBot2s.Any()) rebarBeamMains.AddRange(rebarBot2s);
                if (rebarBot3s.Any()) rebarBeamMains.AddRange(rebarBot3s);

                if (rebarTop1m.Any()) rebarBeamMains.AddRange(rebarTop1m);
                if (rebarTop2m.Any()) rebarBeamMains.AddRange(rebarTop2m);
                if (rebarTop3m.Any()) rebarBeamMains.AddRange(rebarTop3m);
                if (rebarBot1m.Any()) rebarBeamMains.AddRange(rebarBot1m);
                if (rebarBot2m.Any()) rebarBeamMains.AddRange(rebarBot2m);
                if (rebarBot3m.Any()) rebarBeamMains.AddRange(rebarBot3m);

                if (rebarTop1e.Any()) rebarBeamMains.AddRange(rebarTop1e);
                if (rebarTop2e.Any()) rebarBeamMains.AddRange(rebarTop2e);
                if (rebarTop3e.Any()) rebarBeamMains.AddRange(rebarTop3e);
                if (rebarBot1e.Any()) rebarBeamMains.AddRange(rebarBot1e);
                if (rebarBot2e.Any()) rebarBeamMains.AddRange(rebarBot2e);
                if (rebarBot3e.Any()) rebarBeamMains.AddRange(rebarBot3e);
                return rebarBeamMains;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to collect main-bar section data.", ex);
            }
        }
        public List<RebarBeamSectionStart> GetRebarBeamSectionStart(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamSectionType sectionType)
        {
            var result = new List<RebarBeamSectionStart>();
            try
            {
                switch (sectionType)
                {
                    case RebarBeamSectionType.SectionStart:
                        result = installRebarBeamV2ViewModel
                            .ElementInstances
                            .RebarBeams
                            .Select(x => x.RebarBeamSectionStart)
                            .ToList();
                        break;
                    case RebarBeamSectionType.SectionMid:
                        result = null;
                        break;
                    case RebarBeamSectionType.SectionEnd:
                        result = null;
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get start-section reinforcement data.", ex);
            }
            return result;
        }
        public List<RebarBeamSectionMid> GetRebarBeamSectionMid(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamSectionType sectionType)
        {
            var result = new List<RebarBeamSectionMid>();
            try
            {
                switch (sectionType)
                {
                    case RebarBeamSectionType.SectionStart:
                        result = null;
                        break;
                    case RebarBeamSectionType.SectionMid:
                        result = installRebarBeamV2ViewModel
                            .ElementInstances
                            .RebarBeams
                            .Select(x => x.RebarBeamSectionMid)
                            .ToList();
                        break;
                    case RebarBeamSectionType.SectionEnd:
                        result = null;
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get middle-section reinforcement data.", ex);
            }
            return result;
        }

        public List<RebarBeamSectionEnd> GetRebarBeamSectionEnd(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamSectionType sectionType)
        {
            var result = new List<RebarBeamSectionEnd>();
            try
            {
                switch (sectionType)
                {
                    case RebarBeamSectionType.SectionStart:
                        result = null;
                        break;
                    case RebarBeamSectionType.SectionMid:
                        result = null;
                        break;
                    case RebarBeamSectionType.SectionEnd:
                        result = installRebarBeamV2ViewModel
                            .ElementInstances
                            .RebarBeams
                            .Select(x => x.RebarBeamSectionEnd)
                            .ToList();
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get end-section reinforcement data.", ex);
            }
            return result;
        }

        public List<RebarBeamMainBar> GetRebarBeamGroupInfo(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamSectionType sectionType,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType)
        {
            var result = new List<RebarBeamMainBar>();
            try
            {
                switch (rebarBeamMainBarLevelType)
                {
                    case RebarBeamMainBarLevelType.RebarTop:
                        switch (rebarBeamMainBarGroupType)
                        {
                            case RebarBeamMainBarGroupType.GroupLevel1:
                                switch (sectionType)
                                {
                                    case RebarBeamSectionType.SectionStart:
                                        var rebarBeamSectionsStart = GetRebarBeamSectionStart(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsStart.Select(x => x.RebarBeamTop.RebarBeamTopLevel1).ToList();
                                        break;
                                    case RebarBeamSectionType.SectionMid:
                                        var rebarBeamSectionsMid = GetRebarBeamSectionMid(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsMid.Select(x => x.RebarBeamTop.RebarBeamTopLevel1).ToList();
                                        break;
                                    case RebarBeamSectionType.SectionEnd:
                                        var rebarBeamSectionsEnd = GetRebarBeamSectionEnd(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsEnd.Select(x => x.RebarBeamTop.RebarBeamTopLevel1).ToList();
                                        break;
                                }
                                break;
                            case RebarBeamMainBarGroupType.GroupLevel2:
                                switch (sectionType)
                                {
                                    case RebarBeamSectionType.SectionStart:
                                        var rebarBeamSectionsStart = GetRebarBeamSectionStart(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsStart.Select(x => x.RebarBeamTop.RebarBeamTopLevel2).ToList();
                                        break;
                                    case RebarBeamSectionType.SectionMid:
                                        var rebarBeamSectionsMid = GetRebarBeamSectionMid(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsMid.Select(x => x.RebarBeamTop.RebarBeamTopLevel2).ToList();
                                        break;
                                    case RebarBeamSectionType.SectionEnd:
                                        var rebarBeamSectionsEnd = GetRebarBeamSectionEnd(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsEnd.Select(x => x.RebarBeamTop.RebarBeamTopLevel2).ToList();
                                        break;
                                }
                                break;
                            case RebarBeamMainBarGroupType.GroupLevel3:
                                switch (sectionType)
                                {
                                    case RebarBeamSectionType.SectionStart:
                                        var rebarBeamSectionsStart = GetRebarBeamSectionStart(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsStart.Select(x => x.RebarBeamTop.RebarBeamTopLevel3).ToList();
                                        break;
                                    case RebarBeamSectionType.SectionMid:
                                        var rebarBeamSectionsMid = GetRebarBeamSectionMid(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsMid.Select(x => x.RebarBeamTop.RebarBeamTopLevel3).ToList();
                                        break;
                                    case RebarBeamSectionType.SectionEnd:
                                        var rebarBeamSectionsEnd = GetRebarBeamSectionEnd(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsEnd.Select(x => x.RebarBeamTop.RebarBeamTopLevel3).ToList();
                                        break;
                                }
                                break;
                        }
                        break;
                    case RebarBeamMainBarLevelType.RebarBot:
                        switch (rebarBeamMainBarGroupType)
                        {
                            case RebarBeamMainBarGroupType.GroupLevel1:
                                switch (sectionType)
                                {
                                    case RebarBeamSectionType.SectionStart:
                                        var rebarBeamSectionsStart = GetRebarBeamSectionStart(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsStart.Select(x => x.RebarBeamBot.RebarBeamBotLevel1).ToList();
                                        break;
                                    case RebarBeamSectionType.SectionMid:
                                        var rebarBeamSectionsMid = GetRebarBeamSectionMid(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsMid.Select(x => x.RebarBeamBot.RebarBeamBotLevel1).ToList();
                                        break;
                                    case RebarBeamSectionType.SectionEnd:
                                        var rebarBeamSectionsEnd = GetRebarBeamSectionEnd(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsEnd.Select(x => x.RebarBeamBot.RebarBeamBotLevel1).ToList();
                                        break;
                                }
                                break;
                            case RebarBeamMainBarGroupType.GroupLevel2:
                                switch (sectionType)
                                {
                                    case RebarBeamSectionType.SectionStart:
                                        var rebarBeamSectionsStart = GetRebarBeamSectionStart(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsStart.Select(x => x.RebarBeamBot.RebarBeamBotLevel2).ToList();
                                        break;
                                    case RebarBeamSectionType.SectionMid:
                                        var rebarBeamSectionsMid = GetRebarBeamSectionMid(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsMid.Select(x => x.RebarBeamBot.RebarBeamBotLevel2).ToList();
                                        break;
                                    case RebarBeamSectionType.SectionEnd:
                                        var rebarBeamSectionsEnd = GetRebarBeamSectionEnd(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsEnd.Select(x => x.RebarBeamBot.RebarBeamBotLevel2).ToList();
                                        break;
                                }
                                break;
                            case RebarBeamMainBarGroupType.GroupLevel3:
                                switch (sectionType)
                                {
                                    case RebarBeamSectionType.SectionStart:
                                        var rebarBeamSectionsStart = GetRebarBeamSectionStart(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsStart.Select(x => x.RebarBeamBot.RebarBeamBotLevel3).ToList();
                                        break;
                                    case RebarBeamSectionType.SectionMid:
                                        var rebarBeamSectionsMid = GetRebarBeamSectionMid(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsMid.Select(x => x.RebarBeamBot.RebarBeamBotLevel3).ToList();
                                        break;
                                    case RebarBeamSectionType.SectionEnd:
                                        var rebarBeamSectionsEnd = GetRebarBeamSectionEnd(installRebarBeamV2ViewModel, sectionType);
                                        result = rebarBeamSectionsEnd.Select(x => x.RebarBeamBot.RebarBeamBotLevel3).ToList();
                                        break;
                                }
                                break;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get main-bar group data.", ex);
            }
            return result;
        }

        public List<RebarBeamMainBar> GetRebarBeamGroupLevelInfo(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType)
        {
            var result = new List<RebarBeamMainBar>();
            try
            {
                switch (rebarBeamMainBarLevelType)
                {
                    case RebarBeamMainBarLevelType.RebarTop:
                        switch (rebarBeamMainBarGroupType)
                        {
                            case RebarBeamMainBarGroupType.GroupLevel1:
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel1)
                                    .ToList());
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel1)
                                    .ToList());
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel1)
                                    .ToList());
                                break;
                            case RebarBeamMainBarGroupType.GroupLevel2:
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel2)
                                    .ToList());
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel2)
                                    .ToList());
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel2)
                                    .ToList());
                                break;
                            case RebarBeamMainBarGroupType.GroupLevel3:
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel3)
                                    .ToList());
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel3)
                                    .ToList());
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel3)
                                    .ToList());
                                break;
                        }
                        break;
                    case RebarBeamMainBarLevelType.RebarBot:
                        switch (rebarBeamMainBarGroupType)
                        {
                            case RebarBeamMainBarGroupType.GroupLevel1:
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel1)
                                    .ToList());
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel1)
                                    .ToList());
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel1)
                                    .ToList());
                                break;
                            case RebarBeamMainBarGroupType.GroupLevel2:
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel2)
                                    .ToList());
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel2)
                                    .ToList());
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel2)
                                    .ToList());
                                break;
                            case RebarBeamMainBarGroupType.GroupLevel3:
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel3)
                                    .ToList());
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel3)
                                    .ToList());
                                result.AddRange(
                                    installRebarBeamV2ViewModel
                                    .ElementInstances.RebarBeams
                                    .Select(x => x.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel3)
                                    .ToList());
                                break;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get main-bar level data.", ex);
            }
            return result;
        }

        public List<RebarBeamStirrup> GetStirrupGroupInfo(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamSectionType sectionType)
        {
            var result = new List<RebarBeamStirrup>();
            try
            {
                switch (sectionType)
                {
                    case RebarBeamSectionType.SectionStart:
                        var rebarBeamSectionsStart = GetRebarBeamSectionStart(installRebarBeamV2ViewModel, sectionType);
                        result = rebarBeamSectionsStart.Select(x => x.RebarBeamStirrup).ToList();
                        break;
                    case RebarBeamSectionType.SectionMid:
                        var rebarBeamSectionsMid = GetRebarBeamSectionMid(installRebarBeamV2ViewModel, sectionType);
                        result = rebarBeamSectionsMid.Select(x => x.RebarBeamStirrup).ToList();
                        break;
                    case RebarBeamSectionType.SectionEnd:
                        var rebarBeamSectionsEnd = GetRebarBeamSectionEnd(installRebarBeamV2ViewModel, sectionType);
                        result = rebarBeamSectionsEnd.Select(x => x.RebarBeamStirrup).ToList();
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get stirrup group data.", ex);
            }
            return result;
        }

        public static bool CheckIndexRebarMain(int i, int qty, int maxQty, double spacingCurrent, double spacingMin)
        {
            if (qty <= 0 || maxQty <= 0) return false;
            if (maxQty == 1) return i == 0;
            if (qty == 1) return i == (maxQty - 1) / 2;
            if (i == 0 || i == maxQty - 1) return true;

            if (qty > 2)
            {
                if (qty == maxQty) return true;
                var rate = spacingCurrent / spacingMin;
                var dm = Math.Round(maxQty / rate, 0);
                var num = maxQty - qty;
                var iSub = 1;
                if (num == 1)
                {
                    if (i != qty / 2) return true;
                }
                else
                {
                    for (int j = 1; j < dm; j++)
                    {
                        var dmm = (i - j * rate) / i;
                        if (Math.Abs(dmm) <= 0.25)
                        {
                            iSub = int.Parse(Math.Round(i / rate, 0).ToString());
                            break;
                        }
                    }
                    var dk = Math.Abs(i * spacingMin - iSub * spacingCurrent) <= spacingMin / 2;
                    if (dk) return true;
                }
            }
            return false;
        }

        private static IEnumerable<RebarBeamMainBar> EnumerateMainBars(RebarBeam beam)
        {
            if (beam == null) yield break;
            foreach (var section in new RebarBeamSection[]
                     {
                         beam.RebarBeamSectionStart,
                         beam.RebarBeamSectionMid,
                         beam.RebarBeamSectionEnd
                     })
            {
                if (section?.RebarBeamTop != null)
                {
                    yield return section.RebarBeamTop.RebarBeamTopLevel1;
                    yield return section.RebarBeamTop.RebarBeamTopLevel2;
                    yield return section.RebarBeamTop.RebarBeamTopLevel3;
                }
                if (section?.RebarBeamBot != null)
                {
                    yield return section.RebarBeamBot.RebarBeamBotLevel1;
                    yield return section.RebarBeamBot.RebarBeamBotLevel2;
                    yield return section.RebarBeamBot.RebarBeamBotLevel3;
                }
            }
        }
    }
}


