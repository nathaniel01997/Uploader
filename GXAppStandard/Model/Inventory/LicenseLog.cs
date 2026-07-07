using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXUploader.Model.Inventory
{
    public class LicenseLog
    {
        public string App { get; set; }
        public DateTime Issued { get; set; }
        public DateTime Expiry { get; set; }
        public int Days { get; set; }
        public string Signature { get; set; }
        public string LicenseKey { get; set; }
    }
}
