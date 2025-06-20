using System.Collections.Generic;

namespace iNKORE.UI.WPF.CalcBinding.Inversion
{
    /// <summary>
    /// Two keys dictionary
    /// </summary>
    /// <typeparam name="TKey1"></typeparam>
    /// <typeparam name="TKey2"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    internal class Dictionary<TKey1, TKey2, TValue> : Dictionary<TKey1, Dictionary<TKey2, TValue>>
    {
        public void Add(TKey1 key1, TKey2 key2, TValue value)
        {
            if (!ContainsKey(key1))
                Add(key1, new Dictionary<TKey2, TValue>());

            this[key1].Add(key2, value);
        }

        public virtual TValue this[TKey1 key1, TKey2 key2]
        {
            get
            {
                return this[key1][key2];
            }
        }
    }
}
