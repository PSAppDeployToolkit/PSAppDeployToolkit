using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iNKORE.UI.WPF.CalcBinding.PathAnalysis
{
    public class PathTokenId
    {
        public PathTokenType PathType { get; private set; }
        public string Value { get; private set; }

        public PathTokenId(PathTokenType pathType, string value)
        {
            PathType = pathType;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var o = obj as PathTokenId;

            if (o == null)
                return false;

            return (o.PathType == PathType && o.Value == Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ PathType.GetHashCode();
        }
    }
}
