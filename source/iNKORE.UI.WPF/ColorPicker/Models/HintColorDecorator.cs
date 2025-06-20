namespace iNKORE.UI.WPF.ColorPicker.Models
{
    class HintColorDecorator : IColorStateStorage
    {
        public ColorState ColorState
        {
            get => storage.HintColorState;
            set => storage.HintColorState = value;
        }
        private IHintColorStateStorage storage;
        public HintColorDecorator(IHintColorStateStorage storage)
        {
            this.storage = storage;
        }
    }
}
