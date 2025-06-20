namespace iNKORE.UI.WPF.ColorPicker.Models
{
    class SecondColorDecorator : IColorStateStorage
    {
        public ColorState ColorState
        {
            get => storage.SecondColorState;
            set => storage.SecondColorState = value;
        }
        private ISecondColorStorage storage;
        public SecondColorDecorator(ISecondColorStorage storage)
        {
            this.storage = storage;
        }
    }
}
