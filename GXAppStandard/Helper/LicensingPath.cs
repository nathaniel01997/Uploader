using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXUploader.Helper
{
    public class LicensingPath
    {
        public static string BasePath
        {
            get
            {
                const string path = @"C:\Licensing";
                Directory.CreateDirectory(path);
                return path;
            }
        }
    }
}
