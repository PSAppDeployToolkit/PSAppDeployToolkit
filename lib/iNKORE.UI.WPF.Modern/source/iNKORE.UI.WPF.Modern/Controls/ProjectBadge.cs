using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public class ProjectBadge : Button
    {
        static ProjectBadge()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ProjectBadge), new FrameworkPropertyMetadata(typeof(ProjectBadge)));
        }

        protected override void OnClick()
        {
            base.OnClick();
            Process.Start(new ProcessStartInfo(ThemeManager.Link_GithubRepo) { UseShellExecute = true });
        }
    }
}
