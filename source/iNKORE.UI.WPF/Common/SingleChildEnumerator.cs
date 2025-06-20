using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iNKORE.UI.WPF.Common
{
    public class SingleChildEnumerator : IEnumerator
    {
        private int _index = -1;

        private int _count;

        private object _child;

        object IEnumerator.Current
        {
            get
            {
                if (_index != 0)
                {
                    return null;
                }

                return _child;
            }
        }

        public SingleChildEnumerator(object Child)
        {
            _child = Child;
            _count = ((Child != null) ? 1 : 0);
        }

        bool IEnumerator.MoveNext()
        {
            _index++;
            return _index < _count;
        }

        void IEnumerator.Reset()
        {
            _index = -1;
        }
    }
}
