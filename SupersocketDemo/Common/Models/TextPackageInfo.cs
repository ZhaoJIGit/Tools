using SuperSocket.ProtoBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class TextPackageInfo : IKeyedPackageInfo<string>
    {
        public string Key => Text;
        public string Text { get; set; }
    }
}
