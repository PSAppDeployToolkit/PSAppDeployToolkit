using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSADT.UserInterface.Dialogs.Classic
{
    public partial class ClassicDialog : Form
    {
        static ClassicDialog()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        }

        public ClassicDialog()
        {
            InitializeComponent();
        }
    }
}
