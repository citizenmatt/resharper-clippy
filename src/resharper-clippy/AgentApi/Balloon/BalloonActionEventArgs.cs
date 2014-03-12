using System;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon
{
    public class BalloonActionEventArgs<T> : EventArgs
    {
        public int Index;
        public T Tag;

        public BalloonActionEventArgs(int index, T tag)
        {
            Index = index;
            Tag = tag;
        }
    }
}