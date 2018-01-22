using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Thrinax.Utility
{
    public class KeyCounter<T>
    {
        private Dictionary<T, int> dict = new Dictionary<T, int>();

        public Dictionary<T, int> Dict { get { return dict; } }

        public int this[T key]
        {
            get
            {
                if (key == null) return -1;
                if (dict.ContainsKey(key)) return dict[key];
                return 0;
            }
            set
            {
                if (key == null) return;
                if (!dict.ContainsKey(key)) dict.Add(key, value);
                else dict[key] = value;
            }
        }

        public ICollection<T> Keys
        {
            get { return dict.Keys; }
        }

        public T ArgMax
        {
            get
            {
                if (dict.Count == 0) return default(T);
                return HtmlUtility.ArgMax(dict, p => p.Value).Key;
            }
        }

        public IEnumerable<T> ArgMaxAll
        {
            get
            {
                return HtmlUtility.ArgMaxAll(dict, p => p.Value).Select(p => p.Key);
            }
        }
    }
}
