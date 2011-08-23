namespace PackageThis.Core
{
    public class MtpsNode
    {
        public string NavAssetId { get; private set; }
        public string NavLocale { get; private set; }
        public string NavVersion { get; private set; }

        public string TargetAssetId { get; private set; }
        public string TargetContentId { get; private set; }
        public string TargetLocale { get; private set; }
        public string TargetVersion { get; private set; }

        public string Title { get; private set; }

        public bool External { get; private set; }


        public MtpsNode(string navAssetId, string navLocale, string navVersion,string targetContentId, string targetAssetId, string targetLocale, string targetVersion, string title)
        {
            NavAssetId = navAssetId;
            NavLocale = navLocale;
            NavVersion = navVersion;

            TargetContentId = targetContentId;

            TargetAssetId = targetAssetId.ToLower().StartsWith(Constants.ContentIdentifier.ASSETID) ? targetAssetId.Remove(0,8) : targetAssetId;

            TargetLocale = targetLocale;
            TargetVersion = targetVersion;
            

            Title = title;

            External = targetAssetId.ToLower().Contains("http:");
        }

    }
}