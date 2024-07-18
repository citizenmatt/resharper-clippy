using System;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
// ReSharper disable once InconsistentNaming
    public static class IUntypedSignalEx
    {
        public static void FlowInto(this IUntypedSignal source, Lifetime lifetime, IUntypedSignal target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            source.Advise(lifetime, o => target.Fire(o, null));
        }
    }
}