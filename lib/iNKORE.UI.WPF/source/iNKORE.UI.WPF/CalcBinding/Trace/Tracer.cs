using System.Diagnostics;

namespace iNKORE.UI.WPF.CalcBinding.Trace
{
    public sealed class Tracer
    {
        static Tracer()
        {
            _sourceSwitch = new SourceSwitch("CalcBindingTraceLevel", $"{SourceLevels.Off}");
            _traceSource = new TraceSource("CalcBindingTracer")
            {
                Switch = _sourceSwitch
            };
        }

        public Tracer(TraceComponent component)
        {
            _componentName = component.ToString();
        }

        [Conditional("DEBUG")]
        public void TraceDebug(string str)
        {
            Trace(TraceEventType.Verbose, str);
        }

        public void TraceInformation(string str)
        {
            Trace(TraceEventType.Information, str);
        }

        public void TraceError(string str)
        {
            Trace(TraceEventType.Error, str);
        }

        public static TraceListenerCollection Listeners => _traceSource.Listeners;

        private void Trace(TraceEventType level, string str)
        {
            if (_sourceSwitch.ShouldTrace(level))
            {
                _traceSource.TraceData(level, 0, $"{_componentName}: {str}");
            }
        }

        private readonly string _componentName;
        private static readonly SourceSwitch _sourceSwitch;
        private static readonly TraceSource _traceSource;
    }
}
