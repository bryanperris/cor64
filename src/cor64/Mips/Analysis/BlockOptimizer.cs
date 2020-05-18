using System;
using System.Collections.Generic;
using System.Linq;

namespace cor64.Mips.Analysis
{
    public class BlockOptimizer
    {
        private HashSet<ulong> m_BlocksOptimized = new HashSet<ulong>();

        public BlockOptimizer()
        {
        }

        public void Optimize(InfoBasicBlock block)
        {
            if (m_BlocksOptimized.Contains(block.Address))
            {
                return;
            }

            m_BlocksOptimized.Add(block.Address);

            var newLinks = block.Links.DistinctBy(x => x.TargetAddress).ToList();
            block.UpdateLinks(newLinks);
        }
    }

    internal static class BlockOptimizerHelper
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
