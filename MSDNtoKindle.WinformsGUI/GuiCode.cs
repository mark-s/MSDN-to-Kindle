using Microsoft.Win32;

namespace PackageThis.GUI
{
    static public class Gui
    {
        static readonly string key = @"HKEY_CURRENT_USER\Software\CodePlex\PackageThis";
        public static readonly string VID_MshcFile = "MshcFilename";

        static public string GetString(string valueName, string defaultValue)
        {
            return (string)Registry.GetValue(key, valueName, defaultValue);
        }

        static public void SetString(string valueName, string value)
        {
            Registry.SetValue(key, valueName, value, RegistryValueKind.String);
        }


    }
}


