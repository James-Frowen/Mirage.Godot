using System;
using System.Collections.Generic;

namespace Mirage.Events
{
    /// <summary>
    /// An event that will invoke handlers immediately if they are added after <see cref="Invoke"/> has been called
    /// </summary>
    /// <remarks>
    /// <para>
    /// AddLateEvent should be used for time sensitive events where Invoke might be called before the user has chance to add a handler. 
    /// For example Server Started event.
    /// </para>
    /// <para>
    /// Events that are invoked multiple times, like AuthorityChanged, will have the most recent <see cref="Invoke"/> argument sent to new handler. 
    /// </para>
    /// </remarks>
    /// <example>
    /// This Example shows uses of Event
    /// <code>
    /// 
    /// public class Server : MonoBehaviour
    /// {
    ///     // shows in inspector
    ///     [SerializeField]
    ///     private AddLateEvent _started;
    ///
    ///     // expose interface so others can add handlers, but does not let them invoke
    ///     public IAddLateEvent Started => customEvent;
    ///
    ///     public void StartServer()
    ///     {
    ///         // ...
    ///
    ///         // invoke using field
    ///         _started.Invoke();
    ///     }
    ///
    ///     public void StopServer()
    ///     {
    ///         // ...
    ///
    ///         // reset event, resets the hasInvoked flag
    ///         _started.Reset();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <example>
    /// This is an example to show how to create events with arguments:
    /// <code>
    /// // Serializable so that it can be used in inspector
    /// [Serializable]
    /// public class IntUnityEvent : UnityEvent&lt;int&gt; { }
    /// [Serializable]
    /// public class IntAddLateEvent : AddLateEvent&lt;int, IntUnityEvent&gt; { }
    /// 
    /// public class MyClass : MonoBehaviour
    /// {
    ///     [SerializeField]
    ///     private IntAddLateEvent customEvent;
    /// 
    ///     public IAddLateEvent&lt;int&gt; CustomEvent => customEvent;
    /// }
    /// </code>
    /// </example>
    public sealed class AddLateEvent : AddLateEventBase, IAddLateEvent
    {
        private List<Action> _tmp = new List<Action>();
        public List<Action> _event = new List<Action>();

        public void AddListener(Action handler)
        {
            // invoke handler if event has been invoked atleast once
            if (HasInvoked)
            {
                handler.Invoke();
            }

            // add handler to inner event so that it can be invoked again
            _event.Add(handler);
        }

        public void RemoveListener(Action handler)
        {
            _event.Remove(handler);
        }

        public void Invoke()
        {
            MarkInvoked();

            // tmp incase RemoveListener is called inside loop
            _tmp.AddRange(_event);
            foreach (var handler in _tmp)
                handler.Invoke();
            _tmp.Clear();
        }
    }

    /// <summary>
    /// Version of <see cref="AddLateEvent"/> with 1 argument
    /// </summary>
    /// <typeparam name="T0">argument 0</typeparam>
    /// <typeparam name="TEvent">UnityEvent</typeparam>
    public class AddLateEvent<T0> : AddLateEventBase, IAddLateEvent<T0>
    {
        private List<Action<T0>> _tmp = new List<Action<T0>>();
        public List<Action<T0>> _event = new List<Action<T0>>();

        private T0 _arg0;

        public void AddListener(Action<T0> handler)
        {
            // invoke handler if event has been invoked atleast once
            if (HasInvoked)
            {
                handler.Invoke(_arg0);
            }

            // add handler to inner event so that it can be invoked again
            _event.Add(handler);
        }

        public void RemoveListener(Action<T0> handler)
        {
            _event.Remove(handler);
        }

        public void Invoke(T0 arg0)
        {
            MarkInvoked();

            _arg0 = arg0;
            // tmp incase RemoveListener is called inside loop
            _tmp.AddRange(_event);
            foreach (var handler in _tmp)
                handler.Invoke(arg0);
            _tmp.Clear();
        }
    }

    /// <summary>
    /// Version of <see cref="AddLateEvent"/> with 2 arguments
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <typeparam name="T1"></typeparam>
    public class AddLateEvent<T0, T1> : AddLateEventBase, IAddLateEvent<T0, T1>
    {
        private List<Action<T0, T1>> _tmp = new List<Action<T0, T1>>();
        public List<Action<T0, T1>> _event = new List<Action<T0, T1>>();

        private T0 _arg0;
        private T1 _arg1;

        public void AddListener(Action<T0, T1> handler)
        {
            // invoke handler if event has been invoked atleast once
            if (HasInvoked)
            {
                handler.Invoke(_arg0, _arg1);
            }

            // add handler to inner event so that it can be invoked again
            _event.Add(handler);
        }

        public void RemoveListener(Action<T0, T1> handler)
        {
            _event.Remove(handler);
        }

        public void Invoke(T0 arg0, T1 arg1)
        {
            MarkInvoked();

            _arg0 = arg0;
            _arg1 = arg1;

            // tmp incase RemoveListener is called inside loop
            _tmp.AddRange(_event);
            foreach (var handler in _tmp)
                handler.Invoke(arg0, arg1);
            _tmp.Clear();
        }
    }
}
