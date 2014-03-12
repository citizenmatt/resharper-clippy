using System;
using JetBrains.DataFlow;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
// ReSharper disable once InconsistentNaming
    public static class IUntypedSignalEx
    {
        public static void FlowInto(this IUntypedSignal source, Lifetime lifetime, IUntypedSignal target)
        {
            if (lifetime == null)
                throw new ArgumentNullException("lifetime");
            if (source == null)
                throw new ArgumentNullException("source");
            if (target == null)
                throw new ArgumentNullException("target");
            source.Advise(o => target.Fire(o, null), lifetime);
        }

        // Just to get the args the same way round as ISignal<T>
        public static void Advise(this IUntypedSignal signal, Lifetime lifetime, Action<object> handler)
        {
            signal.Advise(handler, lifetime);
        }
    }
}