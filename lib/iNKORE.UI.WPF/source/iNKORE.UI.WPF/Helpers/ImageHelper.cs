using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace iNKORE.UI.WPF.Helpers
{
    public static class ImageHelper
    {
        public static BitmapImage? LoadImage(string? uri, int decodeWidth = 512)
        {
            if (uri == null)
                return null;

            BitmapImage bmp = new BitmapImage();
            bmp.DecodePixelHeight = 250; // 确定解码高度，宽度不同时设置
            bmp.BeginInit();
            // 延迟，必要时创建
            bmp.CreateOptions = BitmapCreateOptions.DelayCreation;
            bmp.DecodePixelWidth = decodeWidth;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(uri);

            //var fs = new FileStream(uri, FileMode.Open, FileAccess.Read);

            //bmp.StreamSource = fs;
            bmp.EndInit(); //结束初始化

            if (bmp.CanFreeze)
                bmp.Freeze();

            //fs.Close();
            //fs.Dispose();

            return bmp;
        }
    }
}
