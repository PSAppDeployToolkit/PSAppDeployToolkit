using iNKORE._Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace iNKORE.UI.WPF.Controls
{
    public class OpacityMaskedImage : Control
    {
        static OpacityMaskedImage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OpacityMaskedImage), new FrameworkPropertyMetadata(typeof(OpacityMaskedImage)));
        }

        public static DependencyProperty SourceProperty = Image.SourceProperty.AddOwner(typeof(OpacityMaskedImage));
        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static DependencyProperty StretchProperty = Image.StretchProperty.AddOwner(typeof(OpacityMaskedImage));
        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        public static DependencyProperty StretchDirectionProperty = Image.StretchDirectionProperty.AddOwner(typeof(OpacityMaskedImage));
        public StretchDirection StretchDirection
        {
            get { return (StretchDirection)GetValue(StretchDirectionProperty); }
            set { SetValue(StretchDirectionProperty, value); }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            //if (!_hasDpiChangedEverFired)
            //{
            //    _hasDpiChangedEverFired = true;
            //    DpiScale dpi = GetDpi();
            //    OnDpiChanged(dpi, dpi);
            //}
            base.MeasureOverride(constraint);
            return MeasureArrangeHelper(constraint);
        }

        Rectangle? PART_Rectangle;

        VisualBrush _maskBrush = new VisualBrush();
        Image _image = new Image();

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            PART_Rectangle = this.Template.FindName(nameof(PART_Rectangle), this) as Rectangle;

            if(PART_Rectangle != null)
            {
                _image = PART_Rectangle.Tag as Image;
                PART_Rectangle.Tag = null;

                _image.SetBinding(WidthProperty, new Binding(nameof(ActualWidth)) { Source = this });
                _image.SetBinding(HeightProperty, new Binding(nameof(ActualHeight)) { Source = this });

                _maskBrush.Stretch = Stretch.None;
                _maskBrush.Visual = _image;

                PART_Rectangle.OpacityMask = _maskBrush;
            }
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            base.ArrangeOverride(arrangeSize);
            return MeasureArrangeHelper(arrangeSize);
        }

        private Size MeasureArrangeHelper(Size inputSize)
        {
            ImageSource source = Source;
            Size size = default(Size);
            if (source == null)
            {
                return size;
            }
            size = new Size(source.Width, source.Height);

            Size size2 = ComputeScaleFactor(inputSize, size, Stretch, StretchDirection);
            return new Size(size.Width * size2.Width, size.Height * size2.Height);
        }

        public static Size ComputeScaleFactor(Size availableSize, Size contentSize, Stretch stretch, StretchDirection stretchDirection)
        {
            double num = 1.0;
            double num2 = 1.0;
            bool flag = !double.IsPositiveInfinity(availableSize.Width);
            bool flag2 = !double.IsPositiveInfinity(availableSize.Height);
            if ((stretch == Stretch.Uniform || stretch == Stretch.UniformToFill || stretch == Stretch.Fill) && (flag || flag2))
            {
                num = (DoubleHelper.IsZero(contentSize.Width) ? 0.0 : (availableSize.Width / contentSize.Width));
                num2 = (DoubleHelper.IsZero(contentSize.Height) ? 0.0 : (availableSize.Height / contentSize.Height));
                if (!flag)
                {
                    num = num2;
                }
                else if (!flag2)
                {
                    num2 = num;
                }
                else
                {
                    switch (stretch)
                    {
                        case Stretch.Uniform:
                            num = (num2 = ((num < num2) ? num : num2));
                            break;
                        case Stretch.UniformToFill:
                            num = (num2 = ((num > num2) ? num : num2));
                            break;
                    }
                }
                switch (stretchDirection)
                {
                    case StretchDirection.UpOnly:
                        if (num < 1.0)
                        {
                            num = 1.0;
                        }
                        if (num2 < 1.0)
                        {
                            num2 = 1.0;
                        }
                        break;
                    case StretchDirection.DownOnly:
                        if (num > 1.0)
                        {
                            num = 1.0;
                        }
                        if (num2 > 1.0)
                        {
                            num2 = 1.0;
                        }
                        break;
                }
            }
            return new Size(num, num2);
        }
    }
}
