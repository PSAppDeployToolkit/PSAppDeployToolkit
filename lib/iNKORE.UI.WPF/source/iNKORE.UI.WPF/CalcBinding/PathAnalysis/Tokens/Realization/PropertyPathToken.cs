using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iNKORE.UI.WPF.CalcBinding.PathAnalysis
{
    public class PropertyPathToken : PathToken
    {
        public IEnumerable<string> Properties { get; private set; }

        private PathTokenId id;
        public override PathTokenId Id { get { return id; } }

        public PropertyPathToken(int start, int end, IEnumerable<string> properties)
            : base(start, end)
        {
            Properties = properties.ToList();
            id = new PathTokenId(PathTokenType.Property, String.Join(".", Properties));
        }
    }
}
