using System.Collections.Generic;

namespace PackageThis.ContentService
{
    static public class RootContentItem
    {

        static public readonly List<string> Libraries = new List<string> ( new[]{ "MSDN Library", "TechNet Library" });

        static private readonly string[] contentIds = { "ms310241", "Bb126093" };
        static private readonly string[] versions = { "MSDN.10", "TechNet.10" };

        static public int CurrentLibrary = 0;

        static public string ContentId
        {
            get
            {
                return contentIds[CurrentLibrary];
            }
        }

        static public string Version
        {
            get
            {
                return versions[CurrentLibrary];
            }
        }

        static public string Name
        {
            get
            {
                return Libraries[CurrentLibrary];
            }
        }

            
    }
}