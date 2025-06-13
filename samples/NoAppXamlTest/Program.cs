using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoAppXamlTest
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            new MainWindow().ShowDialog();
        }
    }
}
