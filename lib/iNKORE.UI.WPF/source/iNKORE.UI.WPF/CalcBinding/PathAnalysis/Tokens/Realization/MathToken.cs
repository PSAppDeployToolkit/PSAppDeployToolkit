using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iNKORE.UI.WPF.CalcBinding.PathAnalysis
{
    public class MathToken : PathToken
    {
        public string MathMember { get; private set; }

        private PathTokenId id;
        public override PathTokenId Id { get { return id; } }

        public MathToken(int start, int end, string mathMember)
            : base(start, end)
        {
            MathMember = mathMember;
            id = new PathTokenId(PathTokenType.Math, String.Join(".", "Math", MathMember));
        }
    }
}
