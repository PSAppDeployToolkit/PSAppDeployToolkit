namespace iNKORE.UI.WPF.ColorPicker.Models
{
    public struct ColorState
    {
        double _RGB_R;
        double _RGB_G;
        double _RGB_B;

        double _A;

        double _HSV_H;
        double _HSV_S;
        double _HSV_V;

        double _HSL_H;
        double _HSL_S;
        double _HSL_L;

        public ColorState(double rGB_R, double rGB_G, double rGB_B, double a, double hSV_H, double hSV_S, double hSV_V, double hSL_h, double hSL_s, double hSL_l)
        {
            _RGB_R = rGB_R;
            _RGB_G = rGB_G;
            _RGB_B = rGB_B;
            _A = a;
            _HSV_H = hSV_H;
            _HSV_S = hSV_S;
            _HSV_V = hSV_V;
            _HSL_H = hSL_h;
            _HSL_S = hSL_s;
            _HSL_L = hSL_l;
        }

        public void SetARGB(double a, double r, double g, double b)
        {
            _A = a;
            _RGB_R = r;
            _RGB_G = g;
            _RGB_B = b;
            RecalculateHSVFromRGB();
            RecalculateHSLFromRGB();
        }
        public double A
        {
            get => _A;
            set
            {
                _A = value;
            }
        }

        public double RGB_R
        {
            get => _RGB_R;
            set
            {
                _RGB_R = value;
                RecalculateHSVFromRGB();
                RecalculateHSLFromRGB();
            }
        }

        public double RGB_G
        {
            get => _RGB_G;
            set
            {
                _RGB_G = value;
                RecalculateHSVFromRGB();
                RecalculateHSLFromRGB();
            }
        }

        public double RGB_B
        {
            get => _RGB_B;
            set
            {
                _RGB_B = value;
                RecalculateHSVFromRGB();
                RecalculateHSLFromRGB();
            }
        }

        public double HSV_H
        {
            get => _HSV_H;
            set
            {
                _HSV_H = value;
                RecalculateRGBFromHSV();
                RecalculateHSLFromHSV();
            }
        }

        public double HSV_S
        {
            get => _HSV_S;
            set
            {
                _HSV_S = value;
                RecalculateRGBFromHSV();
                RecalculateHSLFromHSV();
            }
        }

        public double HSV_V
        {
            get => _HSV_V;
            set
            {
                _HSV_V = value;
                RecalculateRGBFromHSV();
                RecalculateHSLFromHSV();
            }
        }
        public double HSL_H
        {
            get => _HSL_H;
            set
            {
                _HSL_H = value;
                RecalculateRGBFromHSL();
                RecalculateHSVFromHSL();
            }
        }

        public double HSL_S
        {
            get => _HSL_S;
            set
            {
                _HSL_S = value;
                RecalculateRGBFromHSL();
                RecalculateHSVFromHSL();
            }
        }

        public double HSL_L
        {
            get => _HSL_L;
            set
            {
                _HSL_L = value;
                RecalculateRGBFromHSL();
                RecalculateHSVFromHSL();
            }
        }

        private void RecalculateHSLFromRGB()
        {
            var hsltuple = ColorSpaceHelper.RgbToHsl(_RGB_R, _RGB_G, _RGB_B);
            double h = hsltuple.Item1, s = hsltuple.Item2, l = hsltuple.Item3;
            if (h != -1)
                _HSL_H = h;
            if (s != -1)
                _HSL_S = s;
            _HSL_L = l;
        }

        private void RecalculateHSLFromHSV()
        {
            var hsltuple = ColorSpaceHelper.HsvToHsl(_HSV_H, _HSV_S, _HSV_V);
            double h = hsltuple.Item1, s = hsltuple.Item2, l = hsltuple.Item3;
            _HSL_H = h;
            if (s != -1)
                _HSL_S = s;
            _HSL_L = l;
        }

        private void RecalculateHSVFromRGB()
        {
            var hsvtuple = ColorSpaceHelper.RgbToHsv(_RGB_R, _RGB_G, _RGB_B);
            double h = hsvtuple.Item1, s = hsvtuple.Item2, v = hsvtuple.Item3;
            if (h != -1)
                _HSV_H = h;
            if (s != -1)
                _HSV_S = s;
            _HSV_V = v;
        }

        private void RecalculateHSVFromHSL()
        {
            var hsvtuple = ColorSpaceHelper.HslToHsv(_HSL_H, _HSL_S, _HSL_L);
            double h = hsvtuple.Item1, s = hsvtuple.Item2, v = hsvtuple.Item3;
            _HSV_H = h;
            if (s != -1)
                _HSV_S = s;
            _HSV_V = v;
        }

        private void RecalculateRGBFromHSL()
        {
            var rgbtuple = ColorSpaceHelper.HslToRgb(_HSL_H, _HSL_S, _HSL_L);
            _RGB_R = rgbtuple.Item1;
            _RGB_G = rgbtuple.Item2;
            _RGB_B = rgbtuple.Item3;
        }

        private void RecalculateRGBFromHSV()
        {
            var rgbtuple = ColorSpaceHelper.HsvToRgb(_HSV_H, _HSV_S, _HSV_V);
            _RGB_R = rgbtuple.Item1;
            _RGB_G = rgbtuple.Item2;
            _RGB_B = rgbtuple.Item3;
        }
    }
}
