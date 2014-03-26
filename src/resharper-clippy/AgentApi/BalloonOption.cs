﻿using System;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
    public class BalloonOption
    {
        public static readonly object Null = new object();

        public string Text { get; private set; }
        public bool Enabled { get; private set; }
        public object Tag { get; private set; }
        public bool RequiresSeparator { get; private set; }

        public BalloonOption(string text)
            : this(text, false, true, Null)
        {
        }

        public BalloonOption(string text, object tag)
            : this(text, false, true, tag)
        {
        }

        public BalloonOption(string text, bool requiresSeparator, bool enabled, object tag)
        {
            if (tag == null)
                throw new ArgumentNullException("tag");

            Text = text;
            RequiresSeparator = requiresSeparator;
            Enabled = enabled;
            Tag = tag;
        }
    }
}