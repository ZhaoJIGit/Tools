using SuperSocket.ProtoBase;
using System;
using System.Buffers;
using System.Text;


public class UdpPipelineFilter : PipelineFilterBase<TextPackageInfo>
{
    protected override TextPackageInfo DecodePackage(ref ReadOnlySequence<byte> buffer)
    {
        var text = buffer.GetString(Encoding.UTF8);
        buffer = buffer.Slice(buffer.Length); // 消费掉整个 buffer
        return new TextPackageInfo { Text = text };
    }

    public override void Reset()
    {
        // 需要时重置状态
    }

    public override TextPackageInfo Filter(ref SequenceReader<byte> reader)
    {
        //var text = reader.Sequence.GetString(Encoding.UTF8);
        var read = reader.ReadString();
        return new TextPackageInfo { Text = read };
    }
}

