using iNKORE.UI.WPF.Modern.Gallery.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace iNKORE.UI.WPF.Modern.Gallery.Common
{
    public class ImageLoader
    {
        public static ControlInfoDataItem GetSource(DependencyObject obj)
        {
            return (ControlInfoDataItem)obj.GetValue(SourceProperty);
        }

        public static void SetSource(DependencyObject obj, ControlInfoDataItem value)
        {
            obj.SetValue(SourceProperty, value);
        }

        // Using a DependencyProperty as the backing store for Path.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached("Source", typeof(ControlInfoDataItem), typeof(ImageLoader), new PropertyMetadata(null, OnPropertyChanged));

        private async static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Image image)
            {
                var item = e.NewValue as ControlInfoDataItem;
                if (item?.ImagePath != null)
                {
                    Uri imageUri = new Uri(item.ImagePath, UriKind.Relative);
                    BitmapImage imageBitmap = new BitmapImage(imageUri);
                    image.Source = imageBitmap;
                }
            }
        }
    }
}
