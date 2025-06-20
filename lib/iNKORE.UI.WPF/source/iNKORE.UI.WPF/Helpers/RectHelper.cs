using iNKORE._Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace iNKORE.UI.WPF.Helpers
{
    public static class RectHelper
    {
        public static Rect Round(this Rect rect, int digits = 0)
        {
            return new Rect(rect.X.Round(digits), rect.Y.Round(digits), rect.Width.Round(digits), rect.Height.Round(digits));
        }

        public static Rect Max(this Rect rect, Size minSize)
        {
            bool needsToScale = rect.Width < minSize.Width || rect.Height < minSize.Height;

            if (needsToScale)
            {
                var r = rect;
                var ratio = rect.Width / rect.Height;

                r = rect;
                r.Width = minSize.Width;
                r.Height = r.Width / ratio;

                if (r.Height > minSize.Height)
                {
                    return r;
                }
                else
                {
                    r = rect;
                    r.Height = minSize.Height;
                    r.Width = r.Height * ratio;

                    return r;
                }
            }
            else
            {
                return rect;
            }
        }


        public static double GetCenterX(this Rect rect)
        {
            return rect.Left + rect.Width / 2d;
        }

        public static double GetCenterY(this Rect rect)
        {
            return rect.Top + rect.Height / 2d;
        }

        public static bool AreClose(this Rect rect1, Rect rect2)
        {
            if (rect1.IsEmpty)
            {
                return rect2.IsEmpty;
            }

            return DoubleHelper.AreClose(rect1.X, rect2.X) && DoubleHelper.AreClose(rect1.Y, rect2.Y)
                && DoubleHelper.AreClose(rect1.Width, rect2.Width) && DoubleHelper.AreClose(rect1.Height, rect2.Height);
        }

        public static bool AreClose(this Rect rect1, Rect rect2, double maxDifference)
        {
            return Math.Abs(rect1.X - rect2.X).LessThan(maxDifference) && Math.Abs(rect1.Y - rect2.Y).LessThan(maxDifference)
                && Math.Abs(rect1.Width - rect2.Width).LessThan(maxDifference) && Math.Abs(rect1.Height - rect2.Height).LessThan(maxDifference);
        }


        public static bool HasNaN(this Rect r)
        {
            if (DoubleHelper.IsNaN(r.X) || DoubleHelper.IsNaN(r.Y) || DoubleHelper.IsNaN(r.Height) || DoubleHelper.IsNaN(r.Width))
            {
                return true;
            }
            return false;
        }

        public static Rect ApplyMargins(Rect rect, Thickness margins)
        {
            var r = rect;
            r.X = rect.X + margins.Left;
            r.Y = rect.Y + margins.Top;
            r.Width = Math.Max(rect.Width - margins.Right - margins.Left, 0);
            r.Height = Math.Max(rect.Height - margins.Bottom - margins.Top, 0);

            return r;
        }

        public static Rect ApplyMargins(Rect rect, double marginX, double marginY)
        {
            return ApplyMargins(rect, new Thickness(marginX, marginY, marginX, marginY));
        }


        public static Rect GetBounds(this IEnumerable<Point> points)
        {
            Rect? rect = null;
            foreach(var pt in points)
            {
                if(rect == null)
                {
                    rect = new Rect(pt.X, pt.Y, 0, 0);
                }
                else
                {
                    var r = rect.Value;
                    r.Union(pt);

                    rect = r;
                }
            }

            return rect ?? Rect.Empty;
        }

        public static Rect GetBounds(this IEnumerable<StylusPoint> stylusPoints)
        {
            return GetBounds(ToPoints(stylusPoints));
        }


        public static Point[] ToPoints(this IEnumerable<StylusPoint> stylusPoints)
        {
            List<Point> points = new List<Point>();

            foreach(var spt in stylusPoints)
            {
                points.Add(spt.ToPoint());
            }

            return points.ToArray();
        }

        public static Thickness GetMargins(Rect outer, Rect inner)
        {
            return new Thickness(inner.X - outer.X, inner.Y - outer.Y, outer.Right - inner.Right, outer.Bottom - inner.Bottom);
        }

        public static Thickness Abs(Thickness thickness)
        {
            return new Thickness(Math.Abs(thickness.Left), Math.Abs(thickness.Top), Math.Abs(thickness.Right), Math.Abs(thickness.Bottom));
        }
    }
}
