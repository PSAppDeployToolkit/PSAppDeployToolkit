using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iNKORE.UI.WPF.Modern.Gallery.Helpers
{
    public static class StringHelper
    {
        public static string fIndent(this string text, double indent, bool doFirstLine = false)
        {
            string[] lines = RemoveLeadingAndTrailingEmptyLines(text).Split('\n');
            string indentStr = new string(' ', (int)(indent * 4));
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0 && !doFirstLine) continue;
                lines[i] = indentStr + lines[i];
            }
            return string.Join('\n', lines);
        }

        public static string RemoveLeadingAndTrailingEmptyLines(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int start = 0;
            while (start < lines.Length && string.IsNullOrWhiteSpace(lines[start]))
                start++;

            int end = lines.Length - 1;
            while (end >= start && string.IsNullOrWhiteSpace(lines[end]))
                end--;

            var trimmedLines = lines.Skip(start).Take(end - start + 1);
            return string.Join(Environment.NewLine, trimmedLines);
        }

    }
}
