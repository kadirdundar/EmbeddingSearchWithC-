using System.Numerics;
using System.Runtime.InteropServices;

namespace EmbeddingSearch.Core.Math;

/// <summary>
/// Vektörler üzerinde temel işlemler. Arama motorunun "sıcak yolu" (hot path) burasıdır:
/// her sorguda binlerce kez <see cref="Dot"/> çağrılır, bu yüzden SIMD ile hızlandırılmıştır.
/// </summary>
public static class VectorMath
{
    /// <summary>
    /// İç çarpım (dot product): Σ a[i] * b[i].
    /// <see cref="Vector{T}"/> sayesinde işlemci tek komutta 4, 8 veya 16 float'ı birlikte işler (SIMD).
    /// </summary>
    public static float Dot(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException($"Boyutlar farklı: {a.Length} != {b.Length}");

        var sum = Vector<float>.Zero;
        int width = Vector<float>.Count;
        int i = 0;

        if (Vector.IsHardwareAccelerated && a.Length >= width)
        {
            var va = MemoryMarshal.Cast<float, Vector<float>>(a);
            var vb = MemoryMarshal.Cast<float, Vector<float>>(b);
            for (int j = 0; j < va.Length; j++)
                sum += va[j] * vb[j];
            i = va.Length * width;
        }

        float result = Vector.Dot(sum, Vector<float>.One);

        // SIMD genişliğine sığmayan kalan elemanlar.
        for (; i < a.Length; i++)
            result += a[i] * b[i];

        return result;
    }

    /// <summary>SIMD kullanmayan düz döngü. Doğruluk testleri ve benchmark karşılaştırması için.</summary>
    public static float DotScalar(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException($"Boyutlar farklı: {a.Length} != {b.Length}");

        float result = 0f;
        for (int i = 0; i < a.Length; i++)
            result += a[i] * b[i];
        return result;
    }

    /// <summary>Öklid (L2) normu: √(Σ v[i]²).</summary>
    public static float Norm(ReadOnlySpan<float> v) => MathF.Sqrt(Dot(v, v));

    /// <summary>
    /// Kosinüs benzerliği: iki vektör arasındaki açının kosinüsü, [-1, 1] aralığında.
    /// Vektörlerin uzunluğundan bağımsızdır, yalnızca yönlerine bakar.
    /// </summary>
    public static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        float denom = Norm(a) * Norm(b);
        return denom == 0f ? 0f : Dot(a, b) / denom;
    }

    /// <summary>Öklid uzaklığı: √(Σ (a[i] - b[i])²).</summary>
    public static float EuclideanDistance(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException($"Boyutlar farklı: {a.Length} != {b.Length}");

        float sum = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            float d = a[i] - b[i];
            sum += d * d;
        }
        return MathF.Sqrt(sum);
    }

    /// <summary>
    /// Vektörü yerinde birim uzunluğa (norm = 1) getirir.
    /// Normalize edilmiş vektörlerde kosinüs benzerliği = iç çarpım olur; bu da aramayı hızlandırır.
    /// </summary>
    public static void NormalizeInPlace(Span<float> v)
    {
        float norm = Norm(v);
        if (norm == 0f) return;
        for (int i = 0; i < v.Length; i++)
            v[i] /= norm;
    }

    /// <summary>Normalize edilmiş bir kopya döndürür; girdiyi değiştirmez.</summary>
    public static float[] Normalized(ReadOnlySpan<float> v)
    {
        var copy = v.ToArray();
        NormalizeInPlace(copy);
        return copy;
    }
}
