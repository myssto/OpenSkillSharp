namespace OpenSkillSharp.Util;

public static class EnumerableExtensions
{
    public static bool IsEqualLengthTo<T, K>(this IEnumerable<T> source, IEnumerable<K> target) =>
        source.Count() == target.Count();

    public static IList<double> Normalize(this IList<double> source, double min, double max)
    {
        if (source.Count == 1)
        {
            return new List<double> { max };
        }
        
        var srcMin = source.Min();
        var srcRange = source.Max() - srcMin;
        
        if (srcRange == 0)
        {
            srcRange = 0.0001;
        }
        
        return source.Select(v => (v - srcMin) / srcRange * (max - min) + min).ToList();
    }

    /// <summary>
    /// Retain the stochastic tenet of a sort to revert the original sort order.
    /// </summary>
    /// <param name="tenet">A list of tenets for each object in the target list.</param>
    /// <param name="target">A list of objects to sort.</param>
    /// <typeparam name="T">Type of object to sort.</typeparam>
    /// <returns>Ordered objects and their tenets.</returns>
    public static (IList<T> target, IList<double> tenet) Unwind<T>(this IList<double> tenet, IList<T> target)
    {
        if (tenet.Count == 0 || target.Count == 0 || tenet.Count != target.Count)
        {
            return (new List<T>(), new List<double>());
        }
        
        var matrix = target
            .Select((t, i) => (Tenet: tenet[i], Index: i, Object: t))
            .OrderBy(x => x.Tenet)
            .ToList();
        
        var sortedTarget = matrix.Select(x => x.Object).ToList();
        var sortedTenet = matrix.Select(x => (double)x.Index).ToList();
        
        return (sortedTarget, sortedTenet);
    }
}