// Copyright (c) Microsoft Corporation.  All rights reserved.
//

namespace PackageThis.ContentService
{
    public class Image
    {
        public string Name { get; private set; }
        public string ImageFormat { get; private set; }
        public byte[] Data { get; private set; }
        public string Filename
        {
            get
            {
                return Name + "." + ImageFormat;
            }
        }

        public Image(string name, string imageFormat, byte[] data)
        {
           Name = name;
           ImageFormat = imageFormat;
           Data = data;
        }


    }
}
