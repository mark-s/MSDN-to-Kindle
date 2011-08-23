using System;

namespace PackageThis.ContentService
{
    public class BadContentIdException : ApplicationException
    {
        public BadContentIdException(string message) : base(message)
        {
        }

    }
}