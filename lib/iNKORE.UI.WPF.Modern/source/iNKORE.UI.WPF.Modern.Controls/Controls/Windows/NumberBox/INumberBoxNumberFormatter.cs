namespace iNKORE.UI.WPF.Modern.Controls
{
    public interface INumberBoxNumberFormatter
    {
        string FormatDouble(double value);
        double? ParseDouble(string text);
    }
}
