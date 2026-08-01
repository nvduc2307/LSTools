using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.MainStirrups;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups;
using RIMT.Utils.Revit;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup
{
    public abstract class InstallSubStirrupRebarBeam(SubHorizontalStirrupCollectionDto subStirrupDto)
    {
        public List<Rebar> Rebars { get; protected set; } = [];
        protected readonly SubHorizontalStirrupCollectionDto SubStirrupDto = subStirrupDto;

        //public void Run(bool hasRemainder)
        //{
        //    var diameter = SubStirrupDto.RebarBarTypeCustom.RebarBarType.GetRebarDiameter();
        //    var length = SubStirrupDto.BoxElementPoint.P4.DotProduct(SubStirrupDto.Direction) - SubStirrupDto.BoxElementPoint.P1.DotProduct(SubStirrupDto.Direction);
        //    var quantity = length / SubStirrupDto.Spacing;
        //    var phanDu = quantity - (int)quantity;

        //    var chanLe = 0;
        //    for (var i = 0; i < quantity; i++)
        //    {
        //        chanLe = i;
        //        var transform = Transform.CreateTranslation(i * SubStirrupDto.Direction * SubStirrupDto.Spacing);
        //        PlaceRebar(new LineHorizontalDto
        //        { Left = SubStirrupDto.Left, Right = SubStirrupDto.Right, Transform = transform, DirectionToInside = SubStirrupDto.DirectionInside}, chanLe);
        //    }

        //    // Thêm một cây nữa
        //    if (hasRemainder && phanDu > diameter * 2)
        //    {
        //        var transform = Transform.CreateTranslation(length * SubStirrupDto.Direction);
        //        PlaceRebar(new LineHorizontalDto
        //        { Left = SubStirrupDto.Left, Right = SubStirrupDto.Right, Transform = transform, DirectionToInside = SubStirrupDto.DirectionInside }, ++chanLe);
        //    }
        //}
        protected abstract void PlaceRebar(LineHorizontalDto lineDto, int chanLe);

        /// <summary>
        /// Đặt thép ở vị trí start segment và end segment của 1 dầm
        /// </summary>
        /// <returns>
        /// Kết quả là 1 tuple chứa vị trí cuối cùng trong segment
        /// item1: vị trí
        /// item2: thể hiện chẵn hay lẻ
        /// </returns>
        public Tuple<LineHorizontalDto, int> RunForEndAndStartSegment()
        {
            Tuple<LineHorizontalDto, int> last = null;
            var length = SubStirrupDto.BoxElementPoint.P4.DotProduct(SubStirrupDto.Direction) - SubStirrupDto.BoxElementPoint.P1.DotProduct(SubStirrupDto.Direction);
            if (length <= 0.0 || SubStirrupDto.Spacing <= 0.0)
            {
                throw new InvalidOperationException(
                    "Horizontal secondary stirrup segment length and spacing "
                    + "must be positive.");
            }
            var quantity = length / SubStirrupDto.Spacing;
            var phanDu = quantity - (int)quantity;
            var remainderLength = phanDu * SubStirrupDto.Spacing;

            
            var chanLe = 0;
            for (var i = 0; i < quantity; i++)
            {
                chanLe = i;
                var transform = Transform.CreateTranslation(i * SubStirrupDto.Direction * SubStirrupDto.Spacing);
                var lineDto = new LineHorizontalDto()
                {
                    Left = SubStirrupDto.Left,
                    Right = SubStirrupDto.Right,
                    Transform = transform,
                    DirectionToInside = SubStirrupDto.DirectionInside,
                };
                PlaceRebar(lineDto, chanLe);
                last = new Tuple<LineHorizontalDto, int>(lineDto, chanLe);
            }

            if (remainderLength > SubStirrupDto.Spacing * 0.5)
            {
                var transform = Transform.CreateTranslation(
                    length * SubStirrupDto.Direction);
                var lineDto = new LineHorizontalDto()
                {
                    Left = SubStirrupDto.Left,
                    Right = SubStirrupDto.Right,
                    Transform = transform,
                    DirectionToInside = SubStirrupDto.DirectionInside,
                };
                PlaceRebar(lineDto, ++chanLe);
                last = new Tuple<LineHorizontalDto, int>(lineDto, chanLe);
            }

            return last;
        }

        public Tuple<LineHorizontalDto, LineHorizontalDto> RunAtMidSegment(LineHorizontalDto limitOfStartSegment, LineHorizontalDto limitOfEndSegment)
        {
            if (limitOfStartSegment == null || limitOfEndSegment == null)
            {
                throw new InvalidOperationException(
                    "Horizontal secondary stirrup middle zone requires both "
                    + "start and end boundary references.");
            }

            LineHorizontalDto limitStart = null, limitEnd = null;
            var length = SubStirrupDto.BoxElementPoint.P4.DotProduct(SubStirrupDto.Direction) - SubStirrupDto.BoxElementPoint.P1.DotProduct(SubStirrupDto.Direction);
            if (length <= 0.0 || SubStirrupDto.Spacing <= 0.0)
            {
                throw new InvalidOperationException(
                    "Horizontal secondary stirrup middle-zone length and "
                    + "spacing must be positive.");
            }

            var chanLe = 0;

            var center = length / 2;
            var left = center - SubStirrupDto.Spacing;
            var right = center + SubStirrupDto.Spacing;

            var tempPositions2 = new List<LineHorizontalDto>();
            LineHorizontalDto centerLine;
            {
                var transform = Transform.CreateTranslation(center * SubStirrupDto.Direction);

                centerLine = new LineHorizontalDto
                {
                    Left = SubStirrupDto.Left,
                    Right = SubStirrupDto.Right,
                    DirectionToInside = SubStirrupDto.DirectionInside,
                    Transform = transform
                };
                tempPositions2.Add(centerLine);

                //PlaceRebar(lineDto, chanLe++);
            }

            {
                var max = 0;
                while (left > 0)
                {
                    var transform = Transform.CreateTranslation(left * SubStirrupDto.Direction);
                    var lineDto = new LineHorizontalDto()
                    {
                        Left = SubStirrupDto.Left,
                        Right = SubStirrupDto.Right,
                        DirectionToInside = SubStirrupDto.DirectionInside,
                        Transform = transform
                    };

                    tempPositions2.Add(lineDto);
                    limitStart = lineDto;
                    left -= SubStirrupDto.Spacing;
                    max++;
                    if (max > 1000) break;

                    
                }
            }

            {
                var max = 0;
                while (right < length)
                {
                    var transform = Transform.CreateTranslation(right * SubStirrupDto.Direction);
                    var lineDto = new LineHorizontalDto
                    {
                        Left = SubStirrupDto.Left,
                        Right = SubStirrupDto.Right,
                        DirectionToInside = SubStirrupDto.DirectionInside,
                        Transform = transform
                    };
                    tempPositions2.Add(lineDto);
                    limitEnd = lineDto;
                    right += SubStirrupDto.Spacing;
                    max++;
                    if (max > 1000) break;
                }
            }

            limitStart ??= centerLine;
            limitEnd ??= centerLine;
            tempPositions2 = tempPositions2.OrderBy(x => x.Transform.OfPoint(x.Left).DotProduct(SubStirrupDto.Direction)).ToList();
            var dotProductEndOfStartSegment = limitOfStartSegment.Transform.OfPoint(limitOfStartSegment.Left).DotProduct(SubStirrupDto.Direction);
            var dotProductStartOfMidSegment = limitStart.Transform.OfPoint(limitStart.Left).DotProduct(SubStirrupDto.Direction);

            if (Math.Abs(dotProductEndOfStartSegment - dotProductStartOfMidSegment) < SubStirrupDto.Spacing * 0.5)
            {
                if (tempPositions2.Count <= 1)
                {
                    throw new InvalidOperationException(
                        "Horizontal secondary stirrup middle zone is too "
                        + "short to place a bar without violating the "
                        + "minimum boundary spacing.");
                }

                var moveTransform =
                    Transform.CreateTranslation(-SubStirrupDto.Direction * SubStirrupDto.Spacing * 0.5);
                tempPositions2 = tempPositions2.Skip(1).Select(x =>
                {
                    x.Transform = x.Transform.Multiply(moveTransform);
                    return x;
                }).ToList();
            }

            chanLe = 0;
            foreach (var lineDto in tempPositions2)
            {
                PlaceRebar(lineDto, chanLe++);
            }

            limitStart = tempPositions2.First();
            limitEnd = tempPositions2.Last();
            var limitStartAndEnd = new Tuple<LineHorizontalDto, LineHorizontalDto>(limitStart, limitEnd);
            return limitStartAndEnd;
        }
    }
}


