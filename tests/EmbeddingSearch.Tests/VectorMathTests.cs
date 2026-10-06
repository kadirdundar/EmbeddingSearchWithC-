using EmbeddingSearch.Core.Math;

namespace EmbeddingSearch.Tests;

public class VectorMathTests
{
    [Fact]
    public void Cosine_of_identical_vectors_is_one()
    {
        float[] v = { 1, 2, 3 };
        Assert.Equal(1f, VectorMath.CosineSimilarity(v, v), precision: 5);
    }

    [Fact]
    public void Cosine_of_orthogonal_vectors_is_zero()
    {
        Assert.Equal(0f, VectorMath.CosineSimilarity(new float[] { 1, 0 }, new float[] { 0, 1 }), precision: 5);
    }

    [Fact]
    public void Cosine_of_opposite_vectors_is_minus_one()
    {
        Assert.Equal(-1f, VectorMath.CosineSimilarity(new float[] { 1, 2 }, new float[] { -1, -2 }), precision: 5);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(17)]
    [InlineData(384)]
    [InlineData(1537)]
    public void Simd_dot_matches_scalar_dot_for_any_length(int length)
    {
        // Uzunluk SIMD genişliğinin katı olmadığında "kalan elemanlar" döngüsü de test edilir.
        var random = new Random(length);
        var a = Enumerable.Range(0, length).Select(_ => (float)random.NextDouble() - 0.5f).ToArray();
        var b = Enumerable.Range(0, length).Select(_ => (float)random.NextDouble() - 0.5f).ToArray();

        Assert.Equal(VectorMath.DotScalar(a, b), VectorMath.Dot(a, b), precision: 3);
    }

    [Fact]
    public void Dot_rejects_mismatched_lengths()
    {
        Assert.Throws<ArgumentException>(() => VectorMath.Dot(new float[3], new float[4]));
    }

    [Fact]
    public void Normalized_vector_has_unit_length()
    {
        var v = VectorMath.Normalized(new float[] { 3, 4 });
        Assert.Equal(1f, VectorMath.Norm(v), precision: 5);
        Assert.Equal(new[] { 0.6f, 0.8f }, v);
    }

    [Fact]
    public void Normalizing_zero_vector_leaves_it_unchanged()
    {
        var v = new float[3];
        VectorMath.NormalizeInPlace(v);
        Assert.All(v, x => Assert.Equal(0f, x));
    }

    [Fact]
    public void Euclidean_distance_matches_pythagoras()
    {
        Assert.Equal(5f, VectorMath.EuclideanDistance(new float[] { 0, 0 }, new float[] { 3, 4 }), precision: 5);
    }
}
