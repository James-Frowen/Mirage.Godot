namespace Mirage.Events
{
    public abstract class AddLateEventBase
    {
        protected bool HasInvoked { get; private set; }

        protected void MarkInvoked()
        {
            HasInvoked = true;
        }

        /// <summary>
        /// Resets invoked flag, meaning new handles wont be invoked untill invoke is called again
        /// <para>Reset does not remove listeners</para>
        /// </summary>
        public void Reset()
        {
            HasInvoked = false;
        }
    }
}
