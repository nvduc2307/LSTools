using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HcBimUtils;
using HcBimUtils.DocumentUtils;
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
            var diameter = SubStirrupDto.RebarBarTypeCustom.RebarBarType.GetRebarDiameter();
            var length = SubStirrupDto.BoxElementPoint.P4.DotProduct(SubStirrupDto.Direction) - SubStirrupDto.BoxElementPoint.P1.DotProduct(SubStirrupDto.Direction);
            var quantity = length / SubStirrupDto.Spacing;
            var phanDu = quantity - (int)quantity;

            
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

                if (i == (int)quantity)
                {
                    last = new Tuple<LineHorizontalDto, int>(lineDto, chanLe);
                }
            }

            if (phanDu > SubStirrupDto.Spacing * 0.5)
            {
                var transform = Transform.CreateTranslation(((int)quantity + 1) * SubStirrupDto.Direction * SubStirrupDto.Spacing);
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
            LineHorizontalDto limitStart = null, limitEnd = null;
            var length = SubStirrupDto.BoxElementPoint.P4.DotProduct(SubStirrupDto.Direction) - SubStirrupDto.BoxElementPoint.P1.DotProduct(SubStirrupDto.Direction);
            var chanLe = 0;

            var center = length / 2;
            var left = center - SubStirrupDto.Spacing;
            var right = center + SubStirrupDto.Spacing;

            var tempPositions2 = new List<LineHorizontalDto>();
            {
                var transform = Transform.CreateTranslation(center * SubStirrupDto.Direction);

                var lineDto = new LineHorizontalDto
                {
                    Left = SubStirrupDto.Left,
                    Right = SubStirrupDto.Right,
                    DirectionToInside = SubStirrupDto.DirectionInside,
                    Transform = transform
                };
                tempPositions2.Add(lineDto);

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

            tempPositions2 = tempPositions2.OrderBy(x => x.Transform.OfPoint(x.Left).DotProduct(SubStirrupDto.Direction)).ToList();
            var dotProductEndOfStartSegment = limitOfStartSegment.Transform.OfPoint(limitOfStartSegment.Left).DotProduct(SubStirrupDto.Direction);
            var dotProductStartOfMidSegment = limitStart.Transform.OfPoint(limitStart.Left).DotProduct(SubStirrupDto.Direction);

            if (Math.Abs(dotProductEndOfStartSegment - dotProductStartOfMidSegment) < SubStirrupDto.Spacing * 0.5)
            {
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

            var limitStartAndEnd = new Tuple<LineHorizontalDto, LineHorizontalDto>(limitStart, limitEnd);
            return limitStartAndEnd;
        }
    }
}


