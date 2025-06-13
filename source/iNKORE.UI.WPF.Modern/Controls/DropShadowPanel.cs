using iNKORE.UI.WPF.Converters;
using iNKORE.UI.WPF.Modern.Common.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public enum ShadowMode
    {
        Content = 0,
        Inner,
        Outer,
    }

    public class DropShadowPanel : Decorator
    {


        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }
        // Using a DependencyProperty as the backing store for Background.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register("Background", typeof(Brush), typeof(DropShadowPanel), new PropertyMetadata(null));



        public double BlurRadius
        {
            get { return (double)GetValue(BlurRadiusProperty); }
            set { SetValue(BlurRadiusProperty, value); }
        }
        // Using a DependencyProperty as the backing store for BlurRadius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BlurRadiusProperty =
            DependencyProperty.Register("BlurRadius", typeof(double), typeof(DropShadowPanel), new PropertyMetadata(20.0));


        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }
        // Using a DependencyProperty as the backing store for Color.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(DropShadowPanel), new PropertyMetadata(Colors.Black));


        public double Direction
        {
            get { return (double)GetValue(DirectionProperty); }
            set { SetValue(DirectionProperty, value); }
        }
        // Using a DependencyProperty as the backing store for Direction.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register("Direction", typeof(double), typeof(DropShadowPanel), new PropertyMetadata(315.0));

        public double ShadowOpacity
        {
            get { return (double)GetValue(ShadowOpacityProperty); }
            set { SetValue(ShadowOpacityProperty, value); }
        }
        // Using a DependencyProperty as the backing store for ShadowOpacity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShadowOpacityProperty =
            DependencyProperty.Register("ShadowOpacity", typeof(double), typeof(DropShadowPanel), new PropertyMetadata(0.8));


        public RenderingBias RenderingBias
        {
            get { return (RenderingBias)GetValue(RenderingBiasProperty); }
            set { SetValue(RenderingBiasProperty, value); }
        }
        // Using a DependencyProperty as the backing store for RenderingBias.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RenderingBiasProperty =
            DependencyProperty.Register("RenderingBias", typeof(RenderingBias), typeof(DropShadowPanel), new PropertyMetadata(RenderingBias.Performance));


        public double ShadowDepth
        {
            get { return (double)GetValue(ShadowDepthProperty); }
            set { SetValue(ShadowDepthProperty, value); }
        }
        // Using a DependencyProperty as the backing store for ShadowDepth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShadowDepthProperty =
            DependencyProperty.Register("ShadowDepth", typeof(double), typeof(DropShadowPanel), new PropertyMetadata(0.0));

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }
        // Using a DependencyProperty as the backing store for CornerRadius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(DropShadowPanel), new PropertyMetadata(new CornerRadius(0)));




        public ShadowMode ShadowMode
        {
            get { return (ShadowMode)GetValue(ShadowModeProperty); }
            set { SetValue(ShadowModeProperty, value); }
        }
        // Using a DependencyProperty as the backing store for ShadowMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShadowModeProperty =
            DependencyProperty.Register("ShadowMode", typeof(ShadowMode), typeof(DropShadowPanel), new PropertyMetadata(ShadowMode.Content));



        private ContainerVisual _internalVisual;

        public DropShadowPanel()
        {

        }

        private ContainerVisual InternalVisual
        {
            get
            {
                if (_internalVisual == null)
                {
                    _internalVisual = new ContainerVisual();
                    AddVisualChild(_internalVisual);
                }
                return _internalVisual;
            }
        }

        private UIElement InternalChild
        {
            get
            {
                var children = InternalVisual.Children;
                if (children.Count != 0) return children[0] as UIElement;
                else return null;
            }
            set
            {
                var children = InternalVisual.Children;
                if (children.Count != 0) children.Clear();
                children.Add(value);
            }
        }

        public override UIElement Child
        {
            get { return InternalChild; }
            set
            {
                var old = InternalChild;

                if (old != value)
                {
                    // 古い要素をLogicalTreeから取り除く
                    RemoveLogicalChild(old);

                    var ic = CreateInternalVisual(value);

                    if (ic != null)
                    {
                        AddLogicalChild(ic);
                    }

                    InternalChild = ic;

                    InvalidateMeasure();
                }
            }
        }

        protected internal UIElement CreateInternalVisual(UIElement value)
        {
            var effect = new DropShadowEffect();
            BindingOperations.SetBinding(effect, DropShadowEffect.BlurRadiusProperty, new Binding("BlurRadius") { Source = this });
            BindingOperations.SetBinding(effect, DropShadowEffect.ColorProperty, new Binding("Color") { Source = this });
            BindingOperations.SetBinding(effect, DropShadowEffect.DirectionProperty, new Binding("Direction") { Source = this });
            BindingOperations.SetBinding(effect, DropShadowEffect.OpacityProperty, new Binding("ShadowOpacity") { Source = this });
            BindingOperations.SetBinding(effect, DropShadowEffect.RenderingBiasProperty, new Binding("RenderingBias") { Source = this });
            BindingOperations.SetBinding(effect, DropShadowEffect.ShadowDepthProperty, new Binding("ShadowDepth") { Source = this });

            // DropShadowのモード切替用にトリガーを設定
            var st = new Style();

            // ShadowMode.Contentの場合は、ContentをVisualBrushとして扱ってContentの形状に応じた影を作る
            var brush = new VisualBrush(value)
            {
                TileMode = TileMode.None,
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                ViewboxUnits = BrushMappingMode.Absolute
            };
            var contentTrigger = new DataTrigger()
            {
                Binding = new Binding("ShadowMode") { Source = this },
                Value = ShadowMode.Content,
            };
            contentTrigger.Setters.Add(new Setter()
            {
                Property = Border.BorderBrushProperty,
                Value = brush
            });
            st.Triggers.Add(contentTrigger);

            // ShadowMode.Outerの場合は、影はコントロールの外側にだけ表示
            var outerTrigger = new DataTrigger()
            {
                Binding = new Binding("ShadowMode") { Source = this },
                Value = ShadowMode.Outer,
            };
            outerTrigger.Setters.Add(new Setter()
            {
                Property = ClipProperty,
                Value = new MultiBinding()
                {
                    Bindings =
                    {
                        new Binding("ActualWidth") { Source = this },
                        new Binding("ActualHeight") { Source = this },
                        new Binding("BlurRadius") { Source = this },
                        new Binding("CornerRadius") { Source = this }
                    },
                    Converter = new ClipInnerRectConverter()
                }
            });
            outerTrigger.Setters.Add(new Setter()
            {
                Property = Border.BackgroundProperty,
                Value = Brushes.White
            });
            st.Triggers.Add(outerTrigger);

            // ShadowMode.Innerの場合は、影はコントロールの内側にだけ表示
            var innerTrigger = new DataTrigger()
            {
                Binding = new Binding("ShadowMode") { Source = this },
                Value = ShadowMode.Inner,
            };
            innerTrigger.Setters.Add(new Setter()
            {
                Property = Border.BorderBrushProperty,
                Value = Brushes.White
            });
            innerTrigger.Setters.Add(new Setter()
            {
                Property = Border.BorderThicknessProperty,
                Value = new Binding("BlurRadius") { Source = this, Converter = new NegativeMarginConverter() { Multiple = 1 } },
            });
            innerTrigger.Setters.Add(new Setter()
            {
                Property = MarginProperty,
                Value = new Binding("BlurRadius") { Source = this, Converter = new NegativeMarginConverter() }
            });
            innerTrigger.Setters.Add(new Setter()
            {
                Property = ClipProperty,
                Value = new MultiBinding()
                {
                    Bindings =
                    {
                        new Binding("ActualWidth") { Source = this },
                        new Binding("ActualHeight") { Source = this },
                        new Binding("BlurRadius") { Source = this },
                        new Binding("CornerRadius") { Source = this }
                    },
                    Converter = new ClipOuterRectConverter()
                }
            });
            st.Triggers.Add(innerTrigger);

            var border = new Border()
            {
                Effect = effect,
                Style = st,
            };

            border.SetBinding(Border.CornerRadiusProperty, new Binding("CornerRadius") { Source= this });

            var grid = new Grid();
            BindingOperations.SetBinding(grid, Panel.BackgroundProperty, new Binding("Background") { Source = this });
            grid.Children.Add(border);
            if (value != null)
            {
                grid.Children.Add(value);
            }

            return grid;
        }

        // 常に1を返しておく
        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            return InternalVisual;
        }

        protected override IEnumerator LogicalChildren
        {
            get
            {
                if (InternalChild == null)
                {
                    return null;
                }

                var list = new List<UIElement>();
                list.Add(InternalChild);
                return list.GetEnumerator();
            }
        }
    }
}
