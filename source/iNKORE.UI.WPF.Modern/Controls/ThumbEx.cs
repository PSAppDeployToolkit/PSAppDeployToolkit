using iNKORE.UI.WPF.Modern.Common;
using iNKORE.UI.WPF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Modern.Controls
{
    [ContentProperty("Content")]
    public class ThumbEx : Thumb, IAddChild
    {
        static ThumbEx()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ThumbEx), new FrameworkPropertyMetadata(typeof(ThumbEx)));

        }

        #region Content

        public static readonly DependencyProperty ContentProperty = ContentControl.ContentProperty.AddOwner(typeof(ThumbEx), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));
        public object Content
        {
            get { return (object)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }


        protected override IEnumerator LogicalChildren
        {
            get
            {
                if (Content == null)
                {
                    return EmptyEnumerator.Instance;
                }

                return new SingleChildEnumerator(Content);
            }
        }

        void IAddChild.AddChild(object value)
        {
            if (Content != null)
            {
                throw new ArgumentException("CanOnlyHaveOneChild " + GetType().Name, value.GetType().Name);
            }

            Content = value;
        }

        void IAddChild.AddText(string text)
        {
            //XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
            Content = text;
        }

        #endregion
    }
}
