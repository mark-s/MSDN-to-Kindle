using System;

namespace PackageThis.ContentService
{
    public class BadImageNameExeception : ApplicationException
    {
        public BadImageNameExeception(string message) : base(message)
        {
        }
    }
}