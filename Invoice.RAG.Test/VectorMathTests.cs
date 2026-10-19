using Invoice.RAG.Retrieval;
using Xunit;

namespace Invoice.RAG.Test;

public class VectorMathTests
{
    [Fact]
    public void NormalizeAndDotAndByteRoundtrip()
    {
        var v = new float[] { 3f, 4f };
        var n = VectorMath.Normalize(v);
        Assert.Equal(2, n.Length);
        Assert.Equal(3f / 5f, n[0], 5);
        Assert.Equal(4f / 5f, n[1], 5);

        var dot = VectorMath.Dot(n, n);
        Assert.InRange(dot, 0.9999, 1.0001);

        var bytes = VectorMath.ToBytes(v);
        var round = VectorMath.FromBytes(bytes);
        Assert.Equal(v.Length, round.Length);
        Assert.Equal(v[0], round[0]);
        Assert.Equal(v[1], round[1]);
    }
}
