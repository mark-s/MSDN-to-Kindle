// Copyright (c) Microsoft Corporation.  All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using PackageThis.Core.Constants;
using PackageThis.MSDNContentService;

namespace PackageThis.ContentService
{
    public class ContentItem
    {

        //Echo these back out for convenience
        public string Locale { get { return _locale; } }
        public string Product { get { return _collection + "." + _version; } }

        public string Xml{ get; private set; }
        public string Toc { get; private set; }
        public int NumImages { get; private set; }
        public string Links { get; private set; }
        public string Application { get; private set; }
        public string ContentIdentifier { get; private set; }

        public string Metadata { get;  set; }
        public string Annotations { get;  set; }
        public string ContentId { get;  set; }
        
        private readonly string _collection;
        private readonly string _locale;
        private readonly string _version;

        public List<Image> Images = new List<Image>();

        // Because strings returned from the server are used as filenames for pictures and html files, 
        // validate against very restricted set of characters.
        // \w is equivalent to [a-zA-Z0-9_].
        static readonly Regex validateAsFilename = new Regex(@"^[-\w.]+$");


        public ContentItem(string contentIdentifier, string locale, string version, string collection, string application)
        {
            ContentIdentifier = contentIdentifier;
            _locale = locale;
            _version = version;
            _collection = collection;
            Application = application;
        }

        public ContentItem(string contentIdentifier)
        {
            ContentIdentifier = contentIdentifier;
        }

        // Iterator to return filenames in the format required by the xhtml.
        public IEnumerable ImageFilenames
        {
            get
            {
                for (int i = 0; i < Images.Count; i++)
                {
                    if (Images[i].Data == null)
                        continue;

                    yield return GetImageFilename(Images[i]);
                }
            }
        }

        

        // Added the loadFailSafe optimization
        public void Load(bool loadImages, bool loadFailSafe = true)
        {

            var request = new GetContentRequest1(new appId(), new getContentRequest())
            {
                getContentRequest =
                {
                    contentIdentifier = ContentIdentifier,
                    locale =_locale,
                    version =_collection + "." + _version
                }
            };


            var documents = new List<requestedDocument>
                                {
                                    new requestedDocument {type = documentTypes.common, selector = Selectors.Mtps.LINKS},
                                    new requestedDocument {type = documentTypes.primary,  selector = Selectors.Mtps.TOC},
                                    new requestedDocument {type = documentTypes.common, selector = Selectors.Mtps.SEARCH},
                                    new requestedDocument {type = documentTypes.feature,  selector = Selectors.Mtps.ANNOTATIONS}
                                };


            if (loadFailSafe)
                documents.Add(new requestedDocument { type = documentTypes.primary, selector = Selectors.Mtps.FAILSAFE });

            request.getContentRequest.requestedDocuments = documents.ToArray();

            var client = new ContentServicePortTypeClient(WcfService.SERVICE_NAME);
            //client.GetContent()  appIdValue = new appId { value = Application };

            GetContentResponse1 response;
            try
            {
                response = client.GetContent(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debugger.Break();
                return;
            }

            if (validateAsFilename.Match(response.getContentResponse.contentId).Success)
            {
                ContentId = response.getContentResponse.contentId;
            }
            else
            {
                throw (new BadContentIdException("ContentId contains illegal characters: [" + ContentId + "]"));
            }

            NumImages = response.getContentResponse.imageDocuments.Length;


            foreach (var commonDoc in response.getContentResponse.commonDocuments)
            {
                if (commonDoc.Any == null) continue;

                if (commonDoc.commonFormat.ToLowerInvariant() == Selectors.Mtps.SEARCH.ToLowerInvariant())
                {
                    Metadata = commonDoc.Any[0].OuterXml;
                }
                else if (commonDoc.commonFormat.ToLowerInvariant() == Selectors.Mtps.LINKS.ToLowerInvariant())
                {
                    Links = commonDoc.Any[0].OuterXml;
                }
            }

            foreach (primary primaryDoc in response.getContentResponse.primaryDocuments)
            {
                if (primaryDoc.Any == null) continue;

                if (primaryDoc.primaryFormat.ToLowerInvariant() == Selectors.Mtps.FAILSAFE.ToLowerInvariant())
                {
                    Xml = primaryDoc.Any.OuterXml;
                }
                else if (primaryDoc.primaryFormat.ToLowerInvariant() == Selectors.Mtps.TOC.ToLowerInvariant())
                {
                    Toc = primaryDoc.Any.OuterXml;
                }
            }


            foreach (feature featureDoc in response.getContentResponse.featureDocuments)
            {
                if (featureDoc.Any == null) continue;

                if (featureDoc.featureFormat.ToLowerInvariant() == Selectors.Mtps.ANNOTATIONS.ToLowerInvariant())
                {
                    Annotations = featureDoc.Any[0].OuterXml;
                }

            }

            // If we get no meta/search or wiki data, plug in NOP data because
            // we can't LoadXml an empty string nor pass null navigators to
            // the transform.
            if (string.IsNullOrEmpty(Metadata))
                Metadata = "<se:search xmlns:se=\"urn:mtpg-com:mtps/2004/1/search\" />";

            if (string.IsNullOrEmpty(Annotations))
                Annotations = "<an:annotations xmlns:an=\"urn:mtpg-com:mtps/2007/1/annotations\" />";


            if (loadImages)
            {
                var imageDocs = new requestedDocument[response.getContentResponse.imageDocuments.Length];

                // Now that we know their names, we run a request with each image.
                for (int i = 0; i < response.getContentResponse.imageDocuments.Length; i++)
                {
                    imageDocs[i] = new requestedDocument
                                       {
                                           type = documentTypes.image,
                                           selector = response.getContentResponse.imageDocuments[i].name + "." + response.getContentResponse.imageDocuments[i].imageFormat
                                       };
                }

                request.getContentRequest.requestedDocuments = imageDocs;


                response = client.GetContent(request);

                foreach (image imageDoc in response.getContentResponse.imageDocuments)
                {
                    var imageFilename = imageDoc.name + "." + imageDoc.imageFormat;

                    if (validateAsFilename.Match(imageFilename).Success)
                    {
                        Images.Add(new Image(imageDoc.name, imageDoc.imageFormat, imageDoc.Value));
                    }
                    else
                    {
                        throw (new BadImageNameExeception("Image filename contains illegal characters: [" + imageFilename + "]"));
                    }
                }
            }
        }


        // Returns the navigation node that corresponds to this content. If
        // we give it a navigation node already, it'll return that node, so
        // no harm done.
        public string GetNavigationNode()
        {
            // Load the contentItem. If we get a Toc entry, then we know it is
            // a navigation node rather than a content node. The reason is that
            // getNavigationPaths only returns the root node if the target node is
            // a navigation node already. We could check to see if we get one path
            // consisting of one node, but the user could give a target node that is
            // the same as the root node. Perf isn't an issue because this should
            // only be called once with the rootNode.

            Load(false); // Don't load images in case we are a content node.

            if (Toc != null)
                return ContentId;

            var root = new navigationKey { contentId = RootContentItem.ContentId, locale = _locale, version = RootContentItem.Version };

            var target = new navigationKey { contentId = ContentId, locale = _locale, version = _collection + "." + _version };
            //            target.contentId = "AssetId:" + assetId;

            var client = new ContentServicePortTypeClient("ContentService");
            var request = new GetNavigationPathsRequest1
                              {
                                  getNavigationPathsRequest = {target = target, root = root}
                              };

            var response = client.GetNavigationPaths(request);

            // We need to deal with the case where the content appears in many
            // places in the TOC. For now, just use the first path.
            if (response.getNavigationPathsResponse.navigationPaths.Length == 0)
                return null;

            // This is the last node in the first path.
            return response.getNavigationPathsResponse.navigationPaths[0].navigationPathNodes[response.getNavigationPathsResponse.navigationPaths[0].navigationPathNodes.Length - 1].navigationNodeKey.contentId;
        }

        public void Write(string directory)
        {
            Write(directory, ContentId + ApplicationStrings.FILE_EXTENSION_HTM);
        }

        public void Write(string directory, string filename)
        {
            if (Xml == null)
                return;

            //Save image files

            int i = -1;
            String[] imageFiles = new String[Images.Count];

            foreach (Image image in Images)
            {
                i++;
                if (image.Data == null)
                    continue;

                imageFiles[i] = GetImageFilename(image);
                using (var bw = new BinaryWriter(File.Open(Path.Combine(directory, imageFiles[i]), FileMode.Create)))
                {
                    bw.Write(image.Data, 0, image.Data.Length);
                }

            }

            //Adjust Image Links in topic xml
            if (Images.Count != 0)
                Xml = FixImageLinks.XmlFixLinks(Xml, imageFiles);

            //Save HTML file

            using (var sw = new StreamWriter(Path.Combine(directory, filename)))
            {
                sw.Write(Xml);
            }
        }

        private string GetImageFilename(Image image)
        {
            return ContentId + "." + image.Name + "(" + _locale + "," + _collection + "." + _version + ")." + image.ImageFormat;
        }


    }
}
