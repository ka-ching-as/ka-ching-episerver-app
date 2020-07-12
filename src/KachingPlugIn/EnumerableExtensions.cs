using System.Collections.Generic;

namespace KachingPlugIn
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(
            this IEnumerable<T> collection,
            int batchSize)
        {
            List<T> nextBatch = new List<T>(batchSize);
            foreach (T item in collection)
            {
                nextBatch.Add(item);

                if (nextBatch.Count < batchSize)
                {
                    continue;
                }

                yield return nextBatch;

                nextBatch = new List<T>(batchSize);
            }

            if (nextBatch.Count > 0)
            {
                yield return nextBatch;
            }
        }

        public static IEnumerable<IDictionary<TKey, TValue>> Batch<TKey, TValue>(
            this IDictionary<TKey, TValue> collection,
            int batchSize)
        {
            IDictionary<TKey, TValue> nextBatch = new Dictionary<TKey, TValue>(batchSize);

            foreach (KeyValuePair<TKey, TValue> item in collection)
            {
                nextBatch.Add(item);

                if (nextBatch.Count < batchSize)
                {
                    continue;
                }

                yield return nextBatch;

                nextBatch = new Dictionary<TKey, TValue>(batchSize);
            }

            if (nextBatch.Count > 0)
            {
                yield return nextBatch;
            }
        }
    }
}
