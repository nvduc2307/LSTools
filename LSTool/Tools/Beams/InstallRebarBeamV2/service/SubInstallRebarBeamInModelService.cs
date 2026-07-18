using Autodesk.Revit.DB;
using HcBimUtils;
using HcBimUtils.DocumentUtils;
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
    public class SubInstallRebarBeamInModelService : ISubInstallRebarBeamInModelService
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

        public List<MainBarBeamReal> GetMainBarBeamReals(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType,
            double extentCoverSide)
        {
            try
            {
                var rebarBeams = installRebarBeamV2ViewModel.ElementInstances.RebarBeams;
                var subBeams = installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs;
                var qRebarBeams = rebarBeams.Count;
                var rebarStirrupInfos = GetStirrupGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart)
                    .LastOrDefault();
                var diameterStirrup = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .FirstOrDefault(x => x.NameStyle == rebarStirrupInfos.Diameter);
                var rebarGroupInfos = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType);
                var rebarGroupInfosStart = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType);
                var rebarGroupInfosMid = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionMid,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType);
                var rebarGroupInfosEnd = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionEnd,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType);
                // all rebarInfo of Beams
                var rebarBeamMains = GetRebarBeamAllSection(installRebarBeamV2ViewModel);
                var rqMax = rebarBeamMains.Max(x => x.Quantity);
                var rqMin = rebarBeamMains.Min(x => x.Quantity);
                // all RebarInfo on Beam, include Start, Mid, End
                var rebarInfos = new List<RebarBeamMainBar>();
                for (int i = 0; i < qRebarBeams; i++)
                {
                    rebarInfos.Add(rebarGroupInfosStart[i]);
                    rebarInfos.Add(rebarGroupInfosMid[i]);
                    rebarInfos.Add(rebarGroupInfosEnd[i]);
                }
                // refresh rebarInfo.QtyInstall
                foreach (var item in rebarInfos)
                {
                    item.QtyInstall = item.Quantity;
                }
                var qRebarsMax = rebarInfos.Max(x => x.Quantity) > rqMax ? rebarInfos.Max(x => x.Quantity) : rqMax;
                var rebarGroupInfo = rebarGroupInfos.FirstOrDefault();
                var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .FirstOrDefault(x => x.NameStyle == rebarGroupInfo.Diameter);
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                var coverDiameter = diameterStirrup.ModelBarDiameter + diameter.ModelBarDiameter / 2;
                //mainBox
                var boxElementPoint = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.BoxElementPoint;
                var ps = GetPointControls(
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType,
                    coverDiameter,
                    extentCoverSide);
                var cursMain = ps.PointsToCurves(true);
                var lengArr = cursMain
                    .GroupBy(x => x.Length)
                    .OrderBy(x => x.FirstOrDefault().Length)
                    .FirstOrDefault()
                    .FirstOrDefault();
                var spacing = lengArr.Length / (qRebarsMax - 1);
                var rebars = new List<MainBarBeamReal>();
                var cRebarInstall = 0;
                for (int i = 0; i < qRebarsMax; i++)
                {
                    var curve = new MainBarBeamReal();
                    curve.MainPoints = new List<XYZ>();
                    var qRebarInfos = rebarInfos.Count;
                    var c = 0;
                    foreach (var rebarInfo in rebarInfos)
                    {
                        try
                        {

                            var qty = rebarInfo.Quantity;
                            var rebarBeam = installRebarBeamV2ViewModel.ElementInstances.RebarBeams.FirstOrDefault(x => x.BeamId == rebarInfo.HostId);
                            var boxSubBeam = subBeams.FirstOrDefault(x => x.Id == rebarInfo.HostId);
                            var boxSubBeamPoint = boxSubBeam.BoxElementPoint;
                            var beamRule = rebarBeam.BeamStressRule;
                            var beamIndex = subBeams.IndexOf(boxSubBeam) - 1;
                            var sectionIndex = (RebarBeamSectionType)(c - (beamIndex + 1) * 3);
                            var sectionIndexPrev = (RebarBeamSectionType)(c - 1 - beamIndex * 3);
                            //check start hook
                            if (sectionIndex == RebarBeamSectionType.SectionStart && qty > 0) curve.StartHook = true;
                            var psSub = GetPointControls(
                                installRebarBeamV2ViewModel,
                                boxSubBeam,
                                rebarBeamMainBarLevelType,
                                rebarBeamMainBarGroupType,
                                coverDiameter,
                                extentCoverSide);
                            var cursSub = psSub.PointsToCurves(true);
                            var curveSub = cursSub
                                .GroupBy(x => Math.Round(x.Length.FootToMm(), 0))
                                .OrderBy(x => x.FirstOrDefault().Length)
                                .LastOrDefault()
                                .OrderBy(x => x.Midpoint().DotProduct(vty))
                                .FirstOrDefault();

                            var curveSubEnd = cursSub
                                .GroupBy(x => Math.Round(x.Length.FootToMm(), 0))
                                .OrderBy(x => x.FirstOrDefault().Length)
                                .LastOrDefault()
                                .OrderBy(x => x.Midpoint().DotProduct(vty));
                            var lSub = curveSub.Direction().IsSameDirection(vtx)
                                ? curveSub
                                : curveSub.CreateReversed();
                            var psTarget = lSub.GetEndPoint(0) + vty * i * spacing;
                            var peTarget = lSub.GetEndPoint(1) + vty * i * spacing;
                            var lTarget = Line.CreateBound(psTarget, peTarget);
                            var pBase = lTarget.Direction().IsSameDirection(vtx)
                                ? lTarget.GetEndPoint(0)
                                : lTarget.GetEndPoint(1);
                            var spacingSub = lengArr.Length / (qty - 1);
                            Line l = null;
                            switch (sectionIndex)
                            {
                                case RebarBeamSectionType.SectionStart:
                                    var distance = lTarget.Length * beamRule.Stress[0];
                                    var p1 = pBase;
                                    var p2 = p1
                                        + vtx * (distance + installRebarBeamV2ViewModel.ElementInstances.RebarExtend.RebarTopExtend * diameter.ModelBarDiameter);
                                    l = Line.CreateBound(p1, p2);
                                    break;
                                case RebarBeamSectionType.SectionMid:
                                    distance = lTarget.Length * beamRule.Stress[1];
                                    p1 = pBase
                                        + vtx * (lTarget.Length * beamRule.Stress[0] - installRebarBeamV2ViewModel.ElementInstances.RebarExtend.RebarTopExtend * diameter.ModelBarDiameter);
                                    p2 = pBase
                                        + vtx * (lTarget.Length * beamRule.Stress[0] + distance + installRebarBeamV2ViewModel.ElementInstances.RebarExtend.RebarTopExtend * diameter.ModelBarDiameter);
                                    l = Line.CreateBound(p1, p2);
                                    break;
                                case RebarBeamSectionType.SectionEnd:
                                    distance = lTarget.Length * beamRule.Stress[2];
                                    p1 = pBase
                                        + vtx * (lTarget.Length * beamRule.Stress[0] + lTarget.Length * beamRule.Stress[1] - installRebarBeamV2ViewModel.ElementInstances.RebarExtend.RebarTopExtend * diameter.ModelBarDiameter);
                                    p2 = pBase
                                        + vtx * (lTarget.Length * beamRule.Stress[0] + lTarget.Length * beamRule.Stress[1] + distance);
                                    l = Line.CreateBound(p1, p2);
                                    break;
                            }
                            if (qty != 0)
                            {
                                var isIndexRebarMain = CheckIndexRebarMain(i, qty, qRebarsMax, spacingSub, spacing);
                                if (isIndexRebarMain)
                                {
                                    if (i == 0 || i == qRebarsMax - 1)
                                    {
                                        curve.MainPoints.AddRange(new List<XYZ>() { l.GetEndPoint(0), l.GetEndPoint(1) });
                                        rebarInfo.QtyInstall--;
                                    }
                                    else
                                    {
                                        if (rebarInfo.QtyInstall > 1)
                                        {
                                            curve.MainPoints.AddRange(new List<XYZ>() { l.GetEndPoint(0), l.GetEndPoint(1) });
                                            rebarInfo.QtyInstall--;
                                        }
                                    }
                                }

                                else
                                {

                                    //check end hook
                                    if (sectionIndexPrev == RebarBeamSectionType.SectionEnd)
                                        curve.EndHook = true;
                                    if (curve.MainPoints.Count > 0)
                                    {
                                        cRebarInstall++;
                                        rebars.Add(curve);
                                    }
                                    curve = new MainBarBeamReal();
                                    curve.MainPoints = new List<XYZ>();
                                }
                            }
                            else
                            {
                                if (curve.MainPoints.Any())
                                {
                                    //check end hook
                                    if (sectionIndexPrev == RebarBeamSectionType.SectionEnd)
                                        curve.EndHook = true;
                                    if (curve.MainPoints.Count > 0)
                                    {
                                        cRebarInstall++;
                                        rebars.Add(curve);
                                    }
                                    curve = new MainBarBeamReal();
                                    curve.MainPoints = new List<XYZ>();
                                }
                            }
                            if (c == qRebarInfos - 1)
                            {
                                if (curve.MainPoints.Any())
                                {
                                    //check end hook
                                    if (sectionIndex == RebarBeamSectionType.SectionEnd)
                                        curve.EndHook = true;
                                    if (curve.MainPoints.Count > 0)
                                    {
                                        cRebarInstall++;
                                        rebars.Add(curve);
                                    }
                                    curve = new MainBarBeamReal();
                                    curve.MainPoints = new List<XYZ>();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                $"Failed to calculate main-bar geometry at section index {c}.", ex);
                        }
                        c++;
                    }
                }
                rebars = rebars.Where(x => x.MainPoints.Any()).ToList();
                foreach (var rebar in rebars)
                {
                    var mainPs = rebar.MainPoints.OrderBy(x => x.DotProduct(vtx));
                    rebar.StartPoint = mainPs.FirstOrDefault();
                    rebar.EndPoint = mainPs.LastOrDefault();
                    rebar.Level = (int)rebarBeamMainBarLevelType;
                    rebar.Group = (int)rebarBeamMainBarGroupType;
                    GenerateRebarDeverlop(installRebarBeamV2ViewModel, rebar);
                }
                return rebars;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to calculate main-bar geometry.", ex);
            }
        }

        public List<XYZ> GetPointControls(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            BoxElement boxElement,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType,
            double extentStirrupFt,
            double extentCoverSideFt = 0)
        {
            try
            {
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                var coverBeam = installRebarBeamV2ViewModel.ElementInstances.CoverBeam;
                var ps = new List<XYZ>();
                var extentZ = 0.0;
                switch (rebarBeamMainBarGroupType)
                {
                    case RebarBeamMainBarGroupType.GroupLevel1:
                        extentZ = 0.0;
                        break;
                    case RebarBeamMainBarGroupType.GroupLevel2:
                        extentZ = installRebarBeamV2ViewModel.ElementInstances.DistanceRebarToRebarMm.MmToFoot();
                        break;
                    case RebarBeamMainBarGroupType.GroupLevel3:
                        extentZ = 2 * installRebarBeamV2ViewModel.ElementInstances.DistanceRebarToRebarMm.MmToFoot();
                        break;
                }
                switch (rebarBeamMainBarLevelType)
                {
                    case RebarBeamMainBarLevelType.RebarTop:
                        ps = new List<XYZ>()
                        {
                            boxElement.BoxElementPoint.P5.EditZ(boxElement.BoxElementPoint.P5.Z - coverBeam.TopCover.MmToFoot() - extentZ - extentStirrupFt)
                            + vty * (coverBeam.RightCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                            boxElement.BoxElementPoint.P6.EditZ(boxElement.BoxElementPoint.P5.Z - coverBeam.TopCover.MmToFoot() - extentZ- extentStirrupFt)
                            - vty * (coverBeam.LeftCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                            boxElement.BoxElementPoint.P7.EditZ(boxElement.BoxElementPoint.P5.Z - coverBeam.TopCover.MmToFoot() - extentZ- extentStirrupFt)
                            - vty * (coverBeam.LeftCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                            boxElement.BoxElementPoint.P8.EditZ(boxElement.BoxElementPoint.P5.Z - coverBeam.TopCover.MmToFoot() - extentZ- extentStirrupFt)
                            + vty * (coverBeam.RightCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                        };
                        break;
                    case RebarBeamMainBarLevelType.RebarBot:
                        ps = new List<XYZ>()
                        {
                            boxElement.BoxElementPoint.P1.EditZ(boxElement.BoxElementPoint.P1.Z + coverBeam.BottomCover.MmToFoot() + extentZ + extentStirrupFt)
                            + vty * (coverBeam.RightCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                            boxElement.BoxElementPoint.P2.EditZ(boxElement.BoxElementPoint.P1.Z + coverBeam.BottomCover.MmToFoot() + extentZ + extentStirrupFt)
                            - vty * (coverBeam.LeftCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                            boxElement.BoxElementPoint.P3.EditZ(boxElement.BoxElementPoint.P1.Z + coverBeam.BottomCover.MmToFoot() + extentZ + extentStirrupFt)
                            - vty * (coverBeam.LeftCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                            boxElement.BoxElementPoint.P4.EditZ(boxElement.BoxElementPoint.P1.Z + coverBeam.BottomCover.MmToFoot() + extentZ + extentStirrupFt)
                            + vty * (coverBeam.RightCover.MmToFoot() + extentStirrupFt + extentCoverSideFt)
                        };
                        break;
                }
                return ps;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to calculate control points for beam {boxElement?.Id}.", ex);
            }
        }

        public void GenerateRebarDeverlop(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            MainBarBeamReal mainBarBeamReal)
        {
            var wSolid = 50;
            var rebarDevelopType = (RebarBeamAnchorType)installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchorType.Type;
            var levelType = (RebarBeamMainBarLevelType)mainBarBeamReal.Level;
            var groupType = (RebarBeamMainBarGroupType)mainBarBeamReal.Group;
            //var vt = (mainBarBeamReal.EndPoint - mainBarBeamReal.StartPoint).Normalize();
            var vt = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
            var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
            var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
            var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
            var fEvelator = new FaceCustom(vty, mainBarBeamReal.StartPoint);
            var fPlan = new FaceCustom(vtz, mainBarBeamReal.StartPoint);
            var rebarInfo = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    levelType,
                    groupType)
                    .FirstOrDefault();
            var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);
            var radiusMm = diameter.ModelBarDiameter.FootToMm();
            var bendrRadiusMm = diameter.StandardBendDiameter.FootToMm();

            //Y
            var type1_L1_Y_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_Y_Start - radiusMm / 2
                : 0;
            var type1_L3_Y_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_Y_Start - radiusMm / 2
                : 0;
            var type1_L1_Y_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_Y_End - radiusMm / 2
                : 0;
            var type1_L3_Y_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_Y_End - radiusMm / 2
                : 0;
            var type2_L1_Y_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_Y_Start - radiusMm / 2
                : 0;
            var type2_L3_Y_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_Y_Start - radiusMm / 2
                : 0;
            var type2_L1_Y_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_Y_End - radiusMm / 2
                : 0;
            var type2_L3_Y_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_Y_End - radiusMm / 2
                : 0;

            //X
            var type1_L1_X_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_X_Start + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_X_Start;
            var type1_L3_X_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_X_Start + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_X_Start;
            var type1_L1_X_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_X_End + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_X_End;
            var type1_L3_X_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_X_End + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_X_End;
            var type2_L1_X_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_X_Start + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_X_Start;
            var type2_L3_X_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_X_Start + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_X_Start;
            var type2_L1_X_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_X_End + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_X_End;
            var type2_L3_X_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_X_End + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_X_End;

            var type1_top_X_start = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type1_L1_X_Start
                : type1_L3_X_Start;

            var type1_top_Y_start = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type1_L1_Y_Start
                : type1_L3_Y_Start;

            var type1_top_X_end = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type1_L1_X_End
                : type1_L3_X_End;

            var type1_top_Y_end = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type1_L1_Y_End
                : type1_L3_Y_End;

            var type2_top_X_start = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type2_L1_X_Start
                : type2_L3_X_Start;

            var type2_top_Y_start = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type2_L1_Y_Start
                : type2_L3_Y_Start;

            var type2_top_X_end = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type2_L1_X_End
                : type2_L3_X_End;

            var type2_top_Y_end = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type2_L1_Y_End
                : type2_L3_Y_End;

            mainBarBeamReal.MainPoints = new List<XYZ>() { mainBarBeamReal.StartPoint, mainBarBeamReal.EndPoint };
            var extent = 0.0;
            switch (groupType)
            {
                case RebarBeamMainBarGroupType.GroupLevel1:
                    extent = 0;
                    break;
                case RebarBeamMainBarGroupType.GroupLevel2:
                    extent = installRebarBeamV2ViewModel.ElementInstances.DistanceRebarToRebarMm.MmToFoot();
                    break;
                case RebarBeamMainBarGroupType.GroupLevel3:
                    extent = 2 * installRebarBeamV2ViewModel.ElementInstances.DistanceRebarToRebarMm.MmToFoot();
                    break;
            }

            var dirHook = levelType == RebarBeamMainBarLevelType.RebarTop
                ? -vtz
                : vtz;
            XYZ sh0 = null;
            XYZ sh1 = null;
            XYZ eh0 = null;
            XYZ eh1 = null;
            switch (rebarDevelopType)
            {
                case RebarBeamAnchorType.Type1:
                    var lCheckS = mainBarBeamReal.StartPoint.CreateLine(mainBarBeamReal.StartPoint - vt * wSolid.MmToFoot());
                    var lCheckE = mainBarBeamReal.EndPoint.CreateLine(mainBarBeamReal.EndPoint + vt * wSolid.MmToFoot());
                    var solidS = lCheckS.CreateSolid(wSolid, wSolid);
                    var solidE = lCheckE.CreateSolid(wSolid, wSolid);
                    //solidE.CreateDirectShape(AC.Document);
                    var solidFilterS = new ElementIntersectsSolidFilter(solidS);
                    var solidFilterE = new ElementIntersectsSolidFilter(solidE);
                    var eleIdS = new FilteredElementCollector(AC.Document)
                        .WherePasses(solidFilterS)
                        .ToElementIds()
                        .Where(x => !installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs.Any(y => y.Id == x.Value))
                        .FirstOrDefault();
                    var eleIdE = new FilteredElementCollector(AC.Document)
                        .WherePasses(solidFilterE)
                        .ToElementIds()
                        .Where(x => !installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs.Any(y => y.Id == x.Value))
                        .FirstOrDefault();
                    if (eleIdS != null)
                    {
                        try
                        {
                            var ps = AC.Document.GetElement(eleIdS)
                                .GetSolidsExtensions()
                                .Select(x => x.GetPoints())
                                .Aggregate((a, b) => a.Concat(b).ToList())
                                .Select(x => x.RayPointToFace(vty, fEvelator))
                                .Select(x => x.RayPointToFace(vtz, fPlan))
                                .OrderBy(x => x.DotProduct(vt));
                            var min = ps.FirstOrDefault();
                            var max = ps.LastOrDefault();

                            sh1 = min + vt * (type1_top_X_start.MmToFoot() + extent);
                            sh0 = sh1 + dirHook * type1_top_Y_start.MmToFoot();
                        }
                        catch (Exception)
                        {
                            sh0 = mainBarBeamReal.StartPoint;
                            sh1 = mainBarBeamReal.StartPoint;
                        }
                    }
                    else
                    {
                        sh0 = mainBarBeamReal.StartPoint;
                        sh1 = mainBarBeamReal.StartPoint;
                    }
                    if (eleIdE != null)
                    {
                        try
                        {
                            var ps = AC.Document.GetElement(eleIdE)
                                .GetSolidsExtensions()
                                .Select(x => x.GetPoints())
                                .Aggregate((a, b) => a.Concat(b).ToList())
                                .Select(x => x.RayPointToFace(vty, fEvelator))
                                .Select(x => x.RayPointToFace(vtz, fPlan))
                                .OrderBy(x => x.DotProduct(vt));
                            var min = ps.FirstOrDefault();
                            var max = ps.LastOrDefault();

                            eh1 = max - vt * (type1_top_X_end.MmToFoot() + extent);
                            eh0 = eh1 + dirHook * type1_top_Y_end.MmToFoot();
                        }
                        catch (Exception)
                        {
                            eh0 = mainBarBeamReal.EndPoint;
                            eh1 = mainBarBeamReal.EndPoint;
                        }
                    }
                    else
                    {
                        eh0 = mainBarBeamReal.EndPoint;
                        eh1 = mainBarBeamReal.EndPoint;
                    }
                    break;
                case RebarBeamAnchorType.Type2:
                    sh1 = mainBarBeamReal.StartPoint
                        - vt * (type2_top_X_start.MmToFoot() - extent);
                    sh0 = sh1 + dirHook * type2_top_Y_start.MmToFoot();
                    eh1 = mainBarBeamReal.EndPoint
                        + vt * (type2_top_X_end.MmToFoot() - extent);
                    eh0 = eh1 + dirHook * type2_top_Y_end.MmToFoot();
                    break;
            }
            var case1 = new List<XYZ>() { sh0, sh1, eh1, eh0 };
            var case2 = new List<XYZ>() { sh0, sh1, mainBarBeamReal.EndPoint };
            var case3 = new List<XYZ>() { mainBarBeamReal.StartPoint, eh1, eh0 };
            var case4 = new List<XYZ>() { mainBarBeamReal.StartPoint, mainBarBeamReal.EndPoint };
            mainBarBeamReal.MainPoints = mainBarBeamReal.StartHook && mainBarBeamReal.EndHook
                ? case1
                : mainBarBeamReal.StartHook && !mainBarBeamReal.EndHook
                ? case2
                : !mainBarBeamReal.StartHook && mainBarBeamReal.EndHook
                ? case3
                : case4;
            mainBarBeamReal.MainPoints = mainBarBeamReal.MainPoints.Distinct(new ComparePoint()).ToList();
        }

        public List<MainBarBeamReal> GetSideBarBeamReals(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            double extentStirrupFt)
        {
            try
            {
                var results = new List<MainBarBeamReal>();
                var extend = 100.MmToFoot();
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                var rebarBeams = installRebarBeamV2ViewModel.ElementInstances.RebarBeams;
                var subBeams = installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs;
                var qRebarBeams = rebarBeams.Count;
                foreach (var subBeam in subBeams)
                {
                    try
                    {
                        var rebarBeam = rebarBeams.FirstOrDefault(x => x.BeamId == subBeam.Id);
                        var rebarStirrupInfo = rebarBeam.RebarBeamSectionStart.RebarBeamStirrup;
                        var diameterStirrup = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == rebarStirrupInfo.Diameter);
                        var rebarSideInfo = rebarBeam.RebarBeamSectionStart.RebarBeamSideBar;
                        var diameterSide = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == rebarSideInfo.Diameter);
                        var extentCoverSide = diameterStirrup.ModelBarDiameter + diameterSide.ModelBarDiameter / 2;
                        var qtySide = rebarSideInfo.QuantitySide;
                        var csBot = GetPointControls(
                            installRebarBeamV2ViewModel,
                            subBeam,
                            RebarBeamMainBarLevelType.RebarBot,
                            RebarBeamMainBarGroupType.GroupLevel3,
                            extentStirrupFt,
                            extentCoverSide)
                            .PointsToCurves(true)
                            .GroupBy(x => Math.Round(x.Length.FootToMm(), 0))
                            .OrderBy(x => x.FirstOrDefault().Length)
                            .LastOrDefault()
                            .OrderBy(x => x.Midpoint().DotProduct(vty));
                        var csTop = GetPointControls(
                            installRebarBeamV2ViewModel,
                            subBeam,
                            RebarBeamMainBarLevelType.RebarTop,
                            RebarBeamMainBarGroupType.GroupLevel3,
                            extentStirrupFt,
                            extentCoverSide)
                            .PointsToCurves(true)
                            .GroupBy(x => Math.Round(x.Length.FootToMm(), 0))
                            .OrderBy(x => x.FirstOrDefault().Length)
                            .LastOrDefault()
                            .OrderBy(x => x.Midpoint().DotProduct(vty));
                        var pMid = csBot.FirstOrDefault().GetEndPoint(0).Midpoint(csTop.FirstOrDefault().GetEndPoint(0));
                        var heightSpace = csBot.FirstOrDefault().GetEndPoint(0).Distance(csTop.FirstOrDefault().GetEndPoint(0));
                        var spacingSide = heightSpace / (qtySide + 1);

                        var installSpace = (qtySide - 1) * spacingSide;
                        var pLast = pMid - vtz * installSpace / 2;

                        for (int i = 0; i < qtySide; i++)
                        {
                            try
                            {
                                var pTarget = qtySide < 2
                                    ? pMid
                                    : pLast + vtz * i * spacingSide;
                                var lRight = Line.CreateBound(
                                    csBot.FirstOrDefault().GetEndPoint(1).EditZ(pTarget.Z) - vtx * extend,
                                    csBot.FirstOrDefault().GetEndPoint(0).EditZ(pTarget.Z) + vtx * extend);
                                var lLeft = Line.CreateBound(
                                    csBot.LastOrDefault().GetEndPoint(0).EditZ(pTarget.Z) - vtx * extend,
                                    csBot.LastOrDefault().GetEndPoint(1).EditZ(pTarget.Z) + vtx * extend);

                                var rRight = new MainBarBeamReal
                                {
                                    StartPoint = lRight.GetEndPoint(0),
                                    EndPoint = lRight.GetEndPoint(1),
                                    Diameter = rebarSideInfo.Diameter
                                };
                                var rLeft = new MainBarBeamReal
                                {
                                    StartPoint = lLeft.GetEndPoint(0),
                                    EndPoint = lLeft.GetEndPoint(1),
                                    Diameter = rebarSideInfo.Diameter
                                };
                                results.Add(rRight);
                                results.Add(rLeft);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException(
                                    $"Failed to calculate side-bar pair {i} for beam {subBeam.Id}.", ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to calculate side-bar geometry for beam {subBeam.Id}.", ex);
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to calculate side-bar geometry.", ex);
            }
        }

        public List<MainBarBeamReal> GetDantoryBarBeamReals(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            double extentCover)
        {
            try
            {
                var results = new List<MainBarBeamReal>();
                var extend = 100.MmToFoot();
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                var rebarBeams = installRebarBeamV2ViewModel.ElementInstances.RebarBeams;
                var subBeams = installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs;
                var qRebarBeams = rebarBeams.Count;
                foreach (var subBeam in subBeams)
                {
                    try
                    {
                        var rebarBeam = rebarBeams.FirstOrDefault(x => x.BeamId == subBeam.Id);
                        var rebarStirrupInfo = rebarBeam.RebarBeamSectionStart.RebarBeamStirrup;
                        var diameterStirrup = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == rebarStirrupInfo.Diameter);
                        var diameterDantory = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == installRebarBeamV2ViewModel.ElementInstances.RebarBeamDantory.Diameter);
                        var extentCoverSide = diameterStirrup.StandardBendDiameter / 2;
                        var qtyDantory = installRebarBeamV2ViewModel.ElementInstances.RebarBeamDantory.Quantity;
                        var psBot = GetPointControls(
                            installRebarBeamV2ViewModel,
                            subBeam,
                            RebarBeamMainBarLevelType.RebarBot,
                            RebarBeamMainBarGroupType.GroupLevel1,
                            extentCover,
                            extentCoverSide)
                            .Select(x => new XYZ(x.X, x.Y, x.Z - diameterDantory.ModelBarDiameter / 2))
                            .ToList()
                            .PointsToCurves(true)
                            .ToList();
                        var csMaxLength = psBot
                            .GroupBy(x => Math.Round(x.Length.FootToMm(), 0))
                            .OrderBy(x => x.FirstOrDefault().Length)
                            .LastOrDefault()
                            .OrderBy(x => x.Midpoint().DotProduct(vty))
                            .ToList();
                        var csMinLength = psBot
                            .GroupBy(x => Math.Round(x.Length.FootToMm(), 0))
                            .OrderBy(x => x.FirstOrDefault().Length)
                            .FirstOrDefault()
                            .OrderBy(x => x.Midpoint().DotProduct(vtx))
                            .ToList();
                        if (qtyDantory == 1)
                        {
                            var dtr = new MainBarBeamReal()
                            {
                                StartPoint = 0.5 * (csMaxLength.FirstOrDefault().GetEndPoint(0) + csMaxLength.LastOrDefault().GetEndPoint(0)),
                                EndPoint = 0.5 * (csMaxLength.FirstOrDefault().GetEndPoint(1) + csMaxLength.LastOrDefault().GetEndPoint(1)),
                                Diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBeamDantory.Diameter
                            };
                            results.Add(dtr);
                            continue;
                        }
                        var spacing = csMinLength.FirstOrDefault().Length / (qtyDantory - 1);
                        for (int i = 0; i < qtyDantory; i++)
                        {
                            var dtr = new MainBarBeamReal()
                            {
                                StartPoint = csMaxLength.FirstOrDefault().GetEndPoint(0) + i * spacing * vty,
                                EndPoint = csMaxLength.FirstOrDefault().GetEndPoint(1) + i * spacing * vty,
                                Diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBeamDantory.Diameter
                            };
                            results.Add(dtr);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to calculate dantory geometry for beam {subBeam.Id}.", ex);
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to calculate dantory geometry.", ex);
            }
        }

        public static bool CheckIndexRebarMain(int i, int qty, int maxQty, double spacingCurrent, double spacingMin)
        {
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
    }
}


