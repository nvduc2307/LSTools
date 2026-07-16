using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups
{
    public class MainRebarCanvas
    {
        public int Position { get; set; } // 1: top, 2: bottom
        public int Index { get; set; } // from left to right: 0,1,2,3...
       
        public int Diameter => 14;
        public InstallRebarBeamV2ViewModel InstallRebarBeamV2ViewModel { get; set; }
        public bool IsHook { get; set; }
    }

    public class SecondaryStirrupVerticalCanvas
    {
        public int IndexTop { get; set; }
        public int IndexBottom { get; set; }
    }
    public class MainRebarNotLevel1Canvas
    {
        /// <summary>
        /// 1: top, 2: bottom, 3: side
        /// </summary>
        public int Position { get; set; } // 1: top, 2: bottom, 3: side
        /// <summary>
        /// from out to in: 1,2,3...
        /// </summary>
        public int Index { get; set; } // from out to in: 1,2,3...

        public int Diameter => 14;
        public InstallRebarBeamV2ViewModel InstallRebarBeamV2ViewModel { get; set; }
    }
    public class SecondaryStirrupHorizontalCanvas
    {
        public int Index { get; set; }
        public int Position { get; set; } // 1: top, 2: bottom, 3: side
    }

    public class PositionIndex
    {
        public int Index { get; set; }
        public int IndexStep { get; set; }
        /// <summary>
        /// Số lượng trên ít hơn số lượng thép chủ ở dưới
        /// </summary>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <param name="isTop"></param>
        /// <returns></returns>
        public Tuple<int, int, bool> Valid(int top, int bottom, bool isTop)
        {
            if (top == bottom)
            {
                return new Tuple<int, int, bool>(Index, Index, true);
            }

            if (top - 1 == 0) return new Tuple<int, int, bool>(-1, -1, false);

            var step = (bottom * 1.0 - 1) / (top - 1);
            var listBottom = new List<PositionIndex> { };
            var listTop = new List<PositionIndex> { };
            for (var j = 0; j < bottom; j++)
            {
                listBottom.Add(new PositionIndex() { Index = j, IndexStep = j });
            }

            var i = 0.0;
            var ii = 0;
            while (i < (bottom + 0.01))
            {
                listTop.Add(new PositionIndex() { Index = ii, IndexStep = i % 1 == 0 ? (int)i : -100 });
                i += step;
                ii++;
            }

            if (isTop)
            {
                var top1 = listTop.FirstOrDefault(x => x.Index == Index);
                var bottom1 = listBottom.FirstOrDefault(x =>
                {
                    var mod = x.Index / step;
                    if (mod % 1 != 0)
                    {
                        return false;
                    }

                    return (int)Math.Round(mod) == Index;
                });
                if (top1 == null || bottom1 == null) return new Tuple<int, int, bool>(-1, -1, false);

                return new Tuple<int, int, bool>(top1.Index, bottom1.Index, true);
            }
            else
            {
                var top1 = listTop.FirstOrDefault(x => x.IndexStep == Index);
                var bottom1 = listBottom.FirstOrDefault(x => x.Index == Index);
                if (top1 == null || bottom1 == null) return new Tuple<int, int, bool>(-1, -1, false);

                return new Tuple<int, int, bool>(top1.Index, bottom1.Index, true);
            }
        }

        /// <summary>
        /// Số lượng dưới ít hơn số lượng thép chủ ở trên
        /// </summary>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <param name="isTop"></param>
        /// <returns></returns>
        public Tuple<int, int, bool> Valid2(int top, int bottom, bool isTop)
        {
            if (top == bottom)
            {
                return new Tuple<int, int, bool>(Index, Index, true);
            }

            if (bottom - 1 == 0) return new Tuple<int, int, bool>(-1, -1, false);

            var step = (top * 1.0 - 1) / (bottom - 1);
            var listBottom = new List<PositionIndex> { };
            var listTop = new List<PositionIndex> { };
            for (var j = 0; j < top; j++)
            {
                listTop.Add(new PositionIndex() { Index = j, IndexStep = j });
            }

            var i = 0.0;
            var ii = 0;
            while (i < (top + 0.01))
            {
                listBottom.Add(new PositionIndex() { Index = ii, IndexStep = i % 1 == 0 ? (int)i : -100 });
                i += step;
                ii++;
            }

            if (isTop)
            {
                var top1 = listTop.FirstOrDefault(x => x.Index == Index);
                var bottom1 = listBottom.FirstOrDefault(x => x.IndexStep == Index);
                if (top1 == null || bottom1 == null) return new Tuple<int, int, bool>(-1, -1, false);

                return new Tuple<int, int, bool>(top1.Index, bottom1.Index, true);
            }
            else
            {
                var top1 = listTop.FirstOrDefault(x =>
                {
                    var mod = (x.Index / step);
                    if (mod % 1 != 0) return false;

                    return (int)Math.Round(mod) == Index;
                });
                var bottom1 = listBottom.FirstOrDefault(x => x.Index == Index);
                if (top1 == null || bottom1 == null) return new Tuple<int, int, bool>(-1, -1, false);

                return new Tuple<int, int, bool>(top1.Index, bottom1.Index, true);
            }
            
        }
    }


}


