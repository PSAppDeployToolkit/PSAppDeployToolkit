using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iNKORE.UI.WPF.CalcBinding.PathAnalysis
{
    public abstract class PathToken
    {
        public int Start { get; private set; }

        public int End { get; private set; }

        public abstract PathTokenId Id { get; }

        protected PathToken(int start, int end)
        {
            Start = start;
            End = end;
        }
    }
}
