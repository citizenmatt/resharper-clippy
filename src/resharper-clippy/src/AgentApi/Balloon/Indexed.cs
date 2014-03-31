namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon
{
    public class Indexed<T>
    {
        public int Index { get; private set; }
        public T Value { get; private set; }

        public Indexed(int index, T value)
        {
            Index = index;
            Value = value;
        }
    }
}