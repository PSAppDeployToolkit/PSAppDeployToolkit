using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Helpers
{
    public static class MatrixHelper
    {
        public static Matrix GetTransform(Rect from, Rect to)
        {
            var matrix = new Matrix();

            var scaleX = to.Width / from.Width;
            var scaleY = to.Height / from.Height;

            matrix.ScaleAt(scaleX, scaleY, to.GetCenterX(), to.GetCenterY());
            matrix.Translate(to.GetCenterX() - from.GetCenterX(), to.GetCenterY() - from.GetCenterY());
         
            return matrix;
        }


        #region MatrixHelper
        // PresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
        // System.Windows.Ink.MatrixHelper

        public static bool ContainsNaN(this Matrix matrix)
        {
            if (double.IsNaN(matrix.M11) || double.IsNaN(matrix.M12) || double.IsNaN(matrix.M21) || double.IsNaN(matrix.M22) || double.IsNaN(matrix.OffsetX) || double.IsNaN(matrix.OffsetY))
            {
                return true;
            }
            return false;
        }

        public static bool ContainsInfinity(this Matrix matrix)
        {
            if (double.IsInfinity(matrix.M11) || double.IsInfinity(matrix.M12) || double.IsInfinity(matrix.M21) || double.IsInfinity(matrix.M22) || double.IsInfinity(matrix.OffsetX) || double.IsInfinity(matrix.OffsetY))
            {
                return true;
            }
            return false;
        }

        #endregion
    }
}
