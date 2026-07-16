using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.MainStirrups;
using RIMT.Utils.Revit;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service.MainStirrup
{
    public abstract class InstallMainStirrupRebarBeam(MainStirrupCollectionDto mainStirrupDto)
    {
        public List<Rebar> Rebars { get; protected set; } = [];
        protected readonly MainStirrupCollectionDto MainStirrupDto = mainStirrupDto;

        /// <summary>
        /// Đặt thép ở vị trí start segment và end segment của 1 dầm
        /// </summary>
        /// <returns>
        /// Kết quả là 1 tuple chứa vị trí cuối cùng trong segment
        /// item1: vị trí
        /// item2: thể hiện chẵn hay lẻ
        /// </returns>
        public Tuple<RectangleDto, int> RunForEndAndStartSegment()
        {
            Tuple<RectangleDto, int> last = null;
            var diameter = MainStirrupDto.RebarBarTypeCustom.RebarBarType.GetRebarDiameter();
            var length = MainStirrupDto.BoxElementPoint.P4.DotProduct(MainStirrupDto.Direction) - MainStirrupDto.BoxElementPoint.P1.DotProduct(MainStirrupDto.Direction);
            var quantity = length / MainStirrupDto.Spacing;
            var phanDu = quantity - (int)quantity;

            var topLeft = MainStirrupDto.BoxElementPoint.P6;
            var topRight = MainStirrupDto.BoxElementPoint.P5;
            var bottomLeft = MainStirrupDto.BoxElementPoint.P2;
            var bottomRight = MainStirrupDto.BoxElementPoint.P1;

            var basicX = (bottomLeft - bottomRight).Normalize();
            var basicY = (topLeft - bottomLeft).Normalize();

            topLeft = topLeft - basicX * (MainStirrupDto.CoverFootBeam.LeftCover) - basicY *
                (MainStirrupDto.CoverFootBeam.TopCover);
            topRight = topRight + basicX * (MainStirrupDto.CoverFootBeam.RightCover) - basicY * (MainStirrupDto.CoverFootBeam.TopCover);
            bottomLeft = bottomLeft - basicX * (MainStirrupDto.CoverFootBeam.LeftCover) + basicY * (MainStirrupDto.CoverFootBeam.BottomCover);
            bottomRight = bottomRight + basicX * (MainStirrupDto.CoverFootBeam.RightCover) + basicY * (MainStirrupDto.CoverFootBeam.BottomCover);
            var chanLe = 0;
            for (var i = 0; i < quantity; i++)
            {
                try
                {
                    chanLe = i;
                    var transform = Transform.CreateTranslation(i * MainStirrupDto.Direction * MainStirrupDto.Spacing);
                    var rectangleDto = new RectangleDto
                    {
                        BottomLeft = bottomLeft,
                        BottomRight = bottomRight,
                        TopLeft = topLeft,
                        TopRight = topRight,
                        Transform = transform
                    };
                    PlaceRebar(rectangleDto, chanLe);

                    if (i == (int)quantity)
                        last = new Tuple<RectangleDto, int>(rectangleDto, chanLe);
                }
                catch (Exception)
                {
                }
            }

            if (phanDu > MainStirrupDto.Spacing * 0.5)
            {
                var transform = Transform.CreateTranslation(((int)quantity + 1) * MainStirrupDto.Direction * MainStirrupDto.Spacing);
                var rectangleDto = new RectangleDto
                {
                    BottomLeft = bottomLeft,
                    BottomRight = bottomRight,
                    TopLeft = topLeft,
                    TopRight = topRight,
                    Transform = transform
                };
                PlaceRebar(rectangleDto, ++chanLe);
                last = new Tuple<RectangleDto, int>(rectangleDto, chanLe);
            }
            return last;
        }

        public Tuple<RectangleDto, RectangleDto> RunAtMidSegment(RectangleDto limitOfStartSegment, RectangleDto limitOfEndSegment)
        {
            RectangleDto limitStart = null, limitEnd = null;
            var length = MainStirrupDto.BoxElementPoint.P4.DotProduct(MainStirrupDto.Direction) - MainStirrupDto.BoxElementPoint.P1.DotProduct(MainStirrupDto.Direction);
            var topLeft = MainStirrupDto.BoxElementPoint.P6;
            var topRight = MainStirrupDto.BoxElementPoint.P5;
            var bottomLeft = MainStirrupDto.BoxElementPoint.P2;
            var bottomRight = MainStirrupDto.BoxElementPoint.P1;

            var basicX = (bottomLeft - bottomRight).Normalize();
            var basicY = (topLeft - bottomLeft).Normalize();

            topLeft = topLeft - basicX * (MainStirrupDto.CoverFootBeam.LeftCover) - basicY *
                (MainStirrupDto.CoverFootBeam.TopCover);
            topRight = topRight + basicX * (MainStirrupDto.CoverFootBeam.RightCover) - basicY * (MainStirrupDto.CoverFootBeam.TopCover);
            bottomLeft = bottomLeft - basicX * (MainStirrupDto.CoverFootBeam.LeftCover) + basicY * (MainStirrupDto.CoverFootBeam.BottomCover);
            bottomRight = bottomRight + basicX * (MainStirrupDto.CoverFootBeam.RightCover) + basicY * (MainStirrupDto.CoverFootBeam.BottomCover);
            var chanLe = 0;

            var center = length / 2;
            var left = center - MainStirrupDto.Spacing;
            var right = center + MainStirrupDto.Spacing;

            var tempPositions2 = new List<RectangleDto>();
            {
                var transform = Transform.CreateTranslation(center * MainStirrupDto.Direction);

                var rectangleDto = new RectangleDto
                {
                    BottomLeft = bottomLeft,
                    BottomRight = bottomRight,
                    TopLeft = topLeft,
                    TopRight = topRight,
                    Transform = transform
                };
                tempPositions2.Add(rectangleDto);
                //PlaceRebar(rectangleDto, chanLe++);
            }

            {
                var max = 0;
                while (left > 0)
                {
                    var transform = Transform.CreateTranslation(left * MainStirrupDto.Direction);
                    var rectangleDto = new RectangleDto
                    {
                        BottomLeft = bottomLeft,
                        BottomRight = bottomRight,
                        TopLeft = topLeft,
                        TopRight = topRight,
                        Transform = transform
                    };
                    //PlaceRebar(rectangleDto, chanLe++);
                    tempPositions2.Add(rectangleDto);
                    limitStart = rectangleDto;
                    left -= MainStirrupDto.Spacing;
                    max++;
                    if (max > 1000) break;
                }
            }

            {
                var max = 0;
                while (right < length)
                {
                    var transform = Transform.CreateTranslation(right * MainStirrupDto.Direction);
                    var rectangleDto = new RectangleDto
                    {
                        BottomLeft = bottomLeft,
                        BottomRight = bottomRight,
                        TopLeft = topLeft,
                        TopRight = topRight,
                        Transform = transform
                    };
                    //PlaceRebar(rectangleDto, chanLe++);
                    tempPositions2.Add(rectangleDto);
                    limitEnd = rectangleDto;
                    right += MainStirrupDto.Spacing;
                    max++;
                    if (max > 1000) break;
                }
            }

            tempPositions2 = tempPositions2.OrderBy(x => x.Transform.OfPoint(x.BottomLeft).DotProduct(MainStirrupDto.Direction)).ToList();
            var dotProductEndOfStartSegment = limitOfStartSegment.Transform.OfPoint(limitOfStartSegment.BottomLeft).DotProduct(MainStirrupDto.Direction);
            var dotProductStartOfMidSegment = limitStart.Transform.OfPoint(limitStart.BottomLeft).DotProduct(MainStirrupDto.Direction);

            if (Math.Abs(dotProductEndOfStartSegment - dotProductStartOfMidSegment) < MainStirrupDto.Spacing * 0.5)
            {
                var moveTransform =
                    Transform.CreateTranslation(-MainStirrupDto.Direction * MainStirrupDto.Spacing * 0.5);
                tempPositions2 = tempPositions2.Skip(1).Select(x =>
                {
                    x.Transform = x.Transform.Multiply(moveTransform);
                    return x;
                }).ToList();
            }

            chanLe = 0;
            foreach (var rectangleDto in tempPositions2)
            {
                PlaceRebar(rectangleDto, chanLe++);
            }

            var limitStartAndEnd = new Tuple<RectangleDto, RectangleDto>(limitStart, limitEnd);
            return limitStartAndEnd;
        }
        protected abstract void PlaceRebar(RectangleDto rectangleDto, int chanLe);
    }
}


