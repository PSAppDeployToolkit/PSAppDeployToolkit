using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IconKeyFileMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        OpenFileDialog ofd = new OpenFileDialog()
        {
            Multiselect = false,
        };

        private void Button_LoadFile_Click(object sender, RoutedEventArgs e)
        {
            if(ofd.ShowDialog() == true)
            {
                StringBuilder result = new StringBuilder();

                Dictionary<string, int> keys = new Dictionary<string, int>();
                var extension = new FileInfo(ofd.FileName).Extension.ToLower();

                if (extension == ".json")
                {
                    JObject jsonObject = JObject.Parse(File.ReadAllText(ofd.FileName));

                    foreach (var property in jsonObject.Properties())
                    {
                        var iconNames = property.Name.Replace("ic_fluent_", "").Split("_");
                        int unicodeValue = (int)property.Value;

                        var newName = new StringBuilder();
                        int index = 0;
                        foreach (var name in iconNames)
                        {
                            //if(name.ToLower() == "regular")
                            //{
                            //    continue;
                            //}
                            if (index >= iconNames.Length - 2)
                            {
                                newName.Append("_");
                            }
                            newName.Append(CapitalizeFirstLetter(name));

                            index++;
                        }

                        keys[newName.ToString()] = unicodeValue;
                        //result.AppendLine($"public static readonly string {newName.ToString()} = \"\\u{unicodeValue:X4}\";");
                    }

                }
                else if (extension == ".txt")
                {
                    foreach(var line in File.ReadAllLines(ofd.FileName))
                    {
                        try
                        {
                            var parts = line.Split('\t');
                            if (parts.Length > 2)
                            {
                                keys[parts[0]] = Convert.ToInt32(parts[1], 16);
                            }
                        }
                        catch { }
                    }
                }

                foreach (var key in keys.Keys)
                {
                    //var str = Encoding.Unicode.GetString(BitConverter.GetBytes((int)keys[key])).TrimEnd('\0');
                    //var code = $"\\u{(int)str[0]:X4}";

                    string? variant = null;

                    if (key.EndsWith("_Filled"))
                    {
                        variant = "Filled";
                    }
                    else if (key.EndsWith("_Regular"))
                    {
                        variant = "Regular";
                    }

                    if (!string.IsNullOrEmpty(variant))
                    {
                        variant = $", FluentSystemIconVariants.{variant}";
                    }

                    result.AppendLine($"public static readonly FontIconData {key} = CreateIcon(\"\\u{keys[key]:X}\"{variant});");
                }

                TextBox_Result.Text = result.ToString();
            }
        }

        public static string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return char.ToUpper(input[0]) + input.Substring(1);
        }

    }
}