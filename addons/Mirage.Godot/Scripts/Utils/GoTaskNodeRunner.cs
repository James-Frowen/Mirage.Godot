using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Mirage.AsyncTasks
{
    public partial class GoTaskNodeRunner : Node
    {
        public static GoTaskNodeRunner Instance { get; private set; }

        private static readonly UpdateQueue queue = new UpdateQueue();
        public static double Delta { get; private set; }

        public static void CreateInstance(SceneTree sceneTree)
        {
            if (Instance != null)
                return;

            var root = sceneTree.Root;
            var runner = new GoTaskNodeRunner();
            root.AddChild(runner);
        }

        internal static void AddContinuation(Action continuation)
        {
            queue.Enqueue(continuation);
        }

        public override void _Ready()
        {
            GD.Print($"GoTaskNodeRunner._Ready");
            base._Ready();
            if (Instance != null && Instance != this)
                throw new InvalidOperationException("Creating multiple GoTaskNodeRunner");
            Instance = this;
        }

        public override void _Process(double delta)
        {
            //// only log if there are action, to stop spam
            //if (queue.ActionCount > 0)
            //    GD.Print($"TaskRunnerNode Start {delta}");
            Delta = delta;
            queue.Run();
            //if (queue.ActionCount > 0)
            //    GD.Print($"TaskRunnerNode End");
        }
    }

    public struct GoTask
    {
        public static async Task Yield()
        {
            //GD.Print($"GoTask.Yield Start");
            await new ProcessLoopAwaitable();
            //GD.Print($"GoTask.Yield End");
        }
        public static async Task Delay(int milliseconds)
        {
            //GD.Print($"GoTask.Delay Start");

            var seconds = (double)milliseconds / 1000;
            double timer = 0;
            while (timer < seconds)
            {
                await new ProcessLoopAwaitable();
                //GD.Print($"GoTask.Delay after await {GoTaskNodeRunner.Delta}");
                timer += GoTaskNodeRunner.Delta;
            }

            //GD.Print($"GoTask.Delay End");
        }
    }

    public readonly struct ProcessLoopAwaitable
    {
        public Awaiter GetAwaiter()
        {
            return new Awaiter();
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            public bool IsCompleted => false;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                GoTaskNodeRunner.AddContinuation(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                GoTaskNodeRunner.AddContinuation(continuation);
            }
        }
    }

    internal class UpdateQueue
    {
        private const int QUEUE_COUNT = 2;
        // need 2 queues so that item, enqueued while in this fixed update are not invoked till next update (otherwise we might have infinite loop)
        private int _queueIndex;
        private readonly Queue<Action>[] _actionQueue = new Queue<Action>[QUEUE_COUNT] { new Queue<Action>(), new Queue<Action>() };

        public int ActionCount => _actionQueue[_queueIndex].Count;

#if DEBUG
        private readonly Thread _mainThread;
#endif

        public UpdateQueue()
        {
#if DEBUG
            _mainThread = Thread.CurrentThread;
#endif
        }

        public void Enqueue(Action continuation)
        {
#if DEBUG
            if (Thread.CurrentThread != _mainThread)
                GD.PrintErr($"CustomTimingQueue is not thread safe, only call on main thread");
#endif
            _actionQueue[_queueIndex].Enqueue(continuation);
        }

        // delegate entrypoint.
        public void Run()
        {
            // for debugging, create named stacktrace.
#if DEBUG
            _Process();
#else
            RunCore();
#endif
        }


        private void _Process() => RunCore();

        private void RunCore()
        {
#if DEBUG
            if (Thread.CurrentThread != _mainThread)
                GD.PrintErr($"CustomTimingQueue is not thread safe, only call on main thread");
#endif

            // todo can we just use queue count instead of 2 queues?
            var queue = _actionQueue[_queueIndex];
            _queueIndex = (_queueIndex + 1) % QUEUE_COUNT;

            while (queue.Count > 0)
            {
                var action = queue.Dequeue();
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    GD.PrintErr(ex);
                }
            }
        }
    }
}
