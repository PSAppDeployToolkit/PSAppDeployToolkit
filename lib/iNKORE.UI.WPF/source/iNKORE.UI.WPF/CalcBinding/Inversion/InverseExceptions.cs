using System;
using System.Runtime.Serialization;

namespace iNKORE.UI.WPF.CalcBinding.Inversion
{
    [Serializable]
    public class InverseException: Exception
    {
        public InverseException() { }
        public InverseException(string message) : base(message) { }
        public InverseException(string message, Exception inner) : base(message, inner) { }
        protected InverseException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }
    }
}
