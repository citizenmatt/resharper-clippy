using System;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
    public class BalloonOption
    {
        public static readonly object Null = new object();

        public string Text { get; private set; }
        public object Tag { get; private set; }
        public bool RequiresSeparator { get; private set; }

        public BalloonOption(string text)
            : this(text, false, Null)
        {
        }

        public BalloonOption(string text, object tag)
            : this(text, false, tag)
        {
        }

        public BalloonOption(string text, bool requiresSeparator, object tag)
        {
            if (tag == null)
                throw new ArgumentNullException("tag");

            Text = text;
            RequiresSeparator = requiresSeparator;
            Tag = tag;
        }
    }
}