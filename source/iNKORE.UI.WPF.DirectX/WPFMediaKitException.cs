using System;
using System.Runtime.Serialization;

namespace iNKORE.UI.WPF.DirectX
{
    public class DirectXException : Exception
    {
        public DirectXException()
            : base()
        {
        }

        public DirectXException(string message)
            : base(message)
        {
        }

        public DirectXException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DirectXException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}