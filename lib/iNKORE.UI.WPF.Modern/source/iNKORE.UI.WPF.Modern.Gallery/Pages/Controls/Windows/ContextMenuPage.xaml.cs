namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class ContextMenuPage
    {
        public ContextMenuPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
            Example4.Xaml = Example4Xaml;
            Example5.Xaml = Example5Xaml;
            Example6.Xaml = Example6Xaml;
        }

        public string Example1Xaml => $@"
<Button Content=""Options"">
    <Button.ContextMenu>
        <ContextMenu>
            <MenuItem Header=""Reset"" />
            <Separator />
            <MenuItem
                Header=""Repeat""
                IsCheckable=""True""
                IsChecked=""True"" />
            <MenuItem
                Header=""Shuffle""
                IsCheckable=""True""
                IsChecked=""True"" />
        </ContextMenu>
    </Button.ContextMenu>
</Button>
";

        public string Example2Xaml => $@"
<Button Content=""File Options"">
    <Button.ContextMenu>
        <ContextMenu>
            <MenuItem Header=""Open"" />
            <MenuItem Header=""Send to"">
                <MenuItem Header=""Bluetooth"" />
                <MenuItem Header=""Desktop (shortcut)"" />
                <MenuItem Header=""Compressed file"">
                    <MenuItem Header=""Compress and email"" />
                    <MenuItem Header=""Compress to .7z"" />
                    <MenuItem Header=""Compress to .zip"" />
                </MenuItem>
            </MenuItem>
        </ContextMenu>
    </Button.ContextMenu>
</Button>
";

        public string Example3Xaml => $@"
<Button Content=""Edit Options"">
    <Button.ContextMenu>
        <ContextMenu>
            <MenuItem Header=""Share"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE72D;"" />
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header=""Copy"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE16F;"" />
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header=""Delete"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE107;"" />
                </MenuItem.Icon>
            </MenuItem>
            <Separator />
            <MenuItem Header=""Rename"" />
            <MenuItem Header=""Select"" />
        </ContextMenu>
    </Button.ContextMenu>
</Button>
";

        public string Example4Xaml => $@"
<Button Content=""Edit Options"">
    <Button.ContextMenu>
        <ContextMenu>
            <MenuItem Header=""Share"" InputGestureText=""Ctrl+S"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE72D;"" />
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header=""Copy"" InputGestureText=""Ctrl+C"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE16F;"" />
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header=""Delete"" InputGestureText=""Delete"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE107;"" />
                </MenuItem.Icon>
            </MenuItem>
            <Separator />
            <MenuItem Header=""Rename"" />
            <MenuItem Header=""Select"" />
        </ContextMenu>
    </Button.ContextMenu>
</Button>
";

        public string Example5Xaml => $@"
<Button Content=""Edit Options"">
    <ui:ContextFlyoutService.ContextFlyout>
        <ui:MenuFlyout>
            <MenuItem Header=""Share"" InputGestureText=""Ctrl+S"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE72D;"" />
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header=""Copy"" InputGestureText=""Ctrl+C"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE16F;"" />
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header=""Delete"" InputGestureText=""Delete"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE107;"" />
                </MenuItem.Icon>
            </MenuItem>
            <Separator />
            <MenuItem Header=""Rename"" />
            <MenuItem Header=""Select"" />
        </ui:MenuFlyout>
    </ui:ContextFlyoutService.ContextFlyout>
</Button>
";

        public string Example6Xaml => $@"
<TextBox MinWidth=""150"" Text=""Some text"" />
";

        #endregion

    }
}
