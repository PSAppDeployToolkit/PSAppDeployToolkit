using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iNKORE.UI.WPF.Common
{
    public class EmptyEnumerator : IEnumerator
    {
        private static IEnumerator _instance;

        public static IEnumerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EmptyEnumerator();
                }

                return _instance;
            }
        }

        public object Current
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        public EmptyEnumerator()
        {
        }

        public void Reset()
        {
        }

        public bool MoveNext()
        {
            return false;
        }
    }
}