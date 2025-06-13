namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class ToolTipPage
    {
        public ToolTipPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsInitialized) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
        }

        public string Example1Xaml => $@"
<Button Content=""Button with a simple ToolTip."" ToolTip=""Simple ToolTip"" />
";

        public string Example2Xaml => $@"
<TextBlock Text=""TextBlock with an offset ToolTip."">
    <ToolTipService.ToolTip>
        <ToolTip Content=""Offset ToolTip."" VerticalOffset=""-80"" />
    </ToolTipService.ToolTip>
</TextBlock>
";

        public string Example3Xaml => $@"
<Image x:Name=""textBoxToPlace""
    Source=""/Assets/SampleMedia/cliff.jpg"">
    <ToolTipService.ToolTip>
        <ToolTip Content=""Non-occluding ToolTip.""
            Placement=""Right""
            PlacementRectangle=""0,0,400,266"" />
    </ToolTipService.ToolTip>
</Image>
";

        #endregion

    }
}
