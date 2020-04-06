using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreativeZone.Utils
{
    static class Extensions
    {
        public static void Deconstruct<K, V>(this KeyValuePair<K, V> pair, out K key, out V value)
        {
            key = pair.Key;
            value = pair.Value;
        }

        public static void SwapWith<T>(ref this T a, ref T b) where T : struct
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        public static bool Set<T>(ref this T target, T value) where T : struct, IEquatable<T>
        {
            if(target.Equals(value))
                return false;

            target = value;
            return true;
        }

        public static bool None<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            return enumerable.Any(predicate) == false;
        }
    }
}
