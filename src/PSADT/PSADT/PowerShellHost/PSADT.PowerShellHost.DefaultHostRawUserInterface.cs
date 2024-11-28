using System;
using System.Management.Automation.Host;

namespace PSADT.PowerShellHost
{
    /// <summary>
    /// Provides a default implementation of PSHostRawUserInterface.
    /// </summary>
    internal class DefaultHostRawUserInterface : PSHostRawUserInterface
    {
        public override ConsoleColor BackgroundColor { get; set; }
        public override Size BufferSize { get; set; }
        public override Coordinates CursorPosition { get; set; }
        public override int CursorSize { get; set; }
        public override ConsoleColor ForegroundColor { get; set; }
        public override bool KeyAvailable => false;
        public override Size MaxPhysicalWindowSize => new Size(int.MaxValue, int.MaxValue);
        public override Size MaxWindowSize => new Size(int.MaxValue, int.MaxValue);
        public override Coordinates WindowPosition { get; set; }
        public override Size WindowSize { get; set; }
        public override string WindowTitle { get; set; } = string.Empty;
        public override void FlushInputBuffer() { }
        public override BufferCell[,] GetBufferContents(Rectangle rectangle) => new BufferCell[0, 0];
        public override KeyInfo ReadKey(ReadKeyOptions options) => new KeyInfo();
        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill) { }
        public override void SetBufferContents(Rectangle rectangle, BufferCell fill) { }
        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents) { }
    }
}
