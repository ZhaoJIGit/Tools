using Common.Models;
using SuperSocket.ProtoBase;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Filters
{
    public class SimplePipelineFilter : TerminatorPipelineFilter<Common.Models.TextPackageInfo>
    {
        public SimplePipelineFilter() : base(Encoding.UTF8.GetBytes("\r\n"))
        {
        }

        protected override Common.Models.TextPackageInfo DecodePackage(ref ReadOnlySequence<byte> buffer)
        {
            var text = buffer.GetString(Encoding.UTF8);
            return new Common.Models.TextPackageInfo { Text = text };
        }
    }
   
}
