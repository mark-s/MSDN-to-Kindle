// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.Generic;
using System.Web;
using PackageThis.Core.Constants;

namespace PackageThis.Core
{
    public class Link
    {
        private readonly Content _contentDataSet;
        private readonly Dictionary<string, string> _links;

        public Link(Content contentDataSet, Dictionary<string, string> links)
        {
           _contentDataSet = contentDataSet;
           _links = links;
        }

        // Called by the transform to lookup an href. If it begins with "AssetId:", we lookup  its aKeyword.
        public string Resolve(string href, string version, string locale, bool returnContentId)
        {
            if (!href.ToLower().StartsWith(ContentIdentifier.ASSETID))
            {
                return href;
            }

            var assetId = HttpUtility.UrlDecode(href.Remove(0, ContentIdentifier.ASSETID.Length).ToLower());

            var row = _contentDataSet.Tables[TableNames.ITEM].Rows.Find(assetId);

            if (row == null)
            {
                string target = assetId;

                if (_links.ContainsKey(assetId))
                    target = _links[assetId];

                // Added d=ide for a view that hides the TOC.
                return "http://msdn.microsoft.com/library/" + target + "(" + version + "," + locale + ",d=ide).aspx";
            }

            if (returnContentId)
                return row[ColumnNames.CONTENTID].ToString();
            else
                return assetId;
        } 
    }
}
