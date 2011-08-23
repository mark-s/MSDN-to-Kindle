// Copyright (c) Microsoft Corporation.  All rights reserved.
//

using System.Collections.Generic;
using System.Globalization;
using PackageThis.Core;
using PackageThis.Core.Constants;
using PackageThis.MSDNContentService;

namespace PackageThis.ContentService
{
    public static class Utility
    {
        // Use the standard root node to get a list of locales available.
        public static SortedDictionary<string, string> GetLocales()
        {
            var locales = new SortedDictionary<string, string>();

            var client = new ContentServicePortTypeClient(WcfService.SERVICE_NAME);


            GetContentResponse1 response;

            var request = new GetContentRequest1(new appId(), new getContentRequest())
            {
                getContentRequest =
                {
                    contentIdentifier = RootContentItem.ContentId,
                    locale = Defaults.LANGUAGE
                }
            };


            try
            {
                response = client.GetContent(request);
            }
            catch
            {   
                locales.Add(Defaults.LOCALE, Defaults.LANGUAGE);
                return locales;
            }

            foreach (var versionAndLocale in response.getContentResponse.availableVersionsAndLocales)
            {
                // Use the DisplayName as the key instead of the locale because
                // that's how we want the collection sorted.
                string displayName = new CultureInfo(versionAndLocale.locale).DisplayName;

                if (locales.ContainsKey(displayName) == false)
                    locales.Add(displayName, versionAndLocale.locale.ToLower());
            }

            return locales;
        }
    }
}