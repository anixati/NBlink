using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Ko.NBlink
{
    public interface IBlinkCmd
    {
    }

    public interface IBlinkCmdHandler<T> where T : class, IBlinkCmd
    {
        Task Execute(T message, CancellationToken token);
    }

    public interface IBlinkDispatcher
    {
        void Publish<T>(T message) where T : class, IBlinkCmd;

        Task Publish<T>(T message, CancellationToken token) where T : class, IBlinkCmd;

        void Register<T>(Func<IBlinkCmdHandler<T>> factory) where T : class, IBlinkCmd;

        void Register<T>(Action<T> handler) where T : class, IBlinkCmd;
    }

    public sealed class BlinkDispatcher : IBlinkDispatcher
    {
        private readonly ActionBlock<HandlerRequest> _actionQueue;
        private readonly ConcurrentQueue<Func<object, CancellationToken, Task>> _cmdHandlers;

        public BlinkDispatcher()
        {
            _cmdHandlers = new ConcurrentQueue<Func<object, CancellationToken, Task>>();
            var handlers = new List<Func<object, CancellationToken, Task>>();
            _actionQueue = new ActionBlock<HandlerRequest>(async rq =>
            {
                while (_cmdHandlers.TryDequeue(out Func<object, CancellationToken, Task> newHandler))
                {
                    handlers.Add(newHandler);
                }

                ExecutionState result = new ExecutionState();
                foreach (Func<object, CancellationToken, Task> handler in handlers)
                {
                    if (rq.CancelToken.IsCancellationRequested)
                    {
                        break;
                    }
                    try
                    {
                        await handler.Invoke(rq.Message, rq.CancelToken);
                    }
                    catch (Exception ex)
                    {
                        result.AddException(ex);
                        continue;
                    }
                }
                rq.OnComplete(result);
            }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1000, MaxDegreeOfParallelism = Environment.ProcessorCount });
        }

        public void Register<T>(Func<IBlinkCmdHandler<T>> factory) where T : class, IBlinkCmd
        {
            Register<T>((m, ct) => factory.Invoke().Execute(m, ct));
        }

        public void Register<T>(Action<T> handler) where T : class, IBlinkCmd
        {
            Register<T>((m, ct) =>
            {
                handler.Invoke(m);
                return Task.FromResult(0);
            });
        }

        public void Register<T>(Func<T, CancellationToken, Task> handler) where T : class, IBlinkCmd
        {
            Func<object, CancellationToken, Task> hc = async (m, ct) =>
            {
                if (m.GetType().IsAssignableFrom(typeof(T)))
                {
                    await handler.Invoke((T)m, ct);
                }
            };
            _cmdHandlers.Enqueue(hc);
        }

        public void Publish<T>(T message) where T : class, IBlinkCmd
        {
            var task = Publish(message, CancellationToken.None);
        }

        public Task Publish<T>(T message, CancellationToken token) where T : class, IBlinkCmd
        {
            var rval = new TaskCompletionSource<ExecutionState>();
            _actionQueue.Post(new HandlerRequest(message, token, result => rval.SetResult(result)));
            return rval.Task;
        }

        private class HandlerRequest
        {
            public object Message { get; private set; }
            public CancellationToken CancelToken { get; private set; }
            public Action<ExecutionState> OnComplete { get; private set; }

            public HandlerRequest(object message, CancellationToken token, Action<ExecutionState> onComplete)
            {
                Message = message;
                CancelToken = token;
                OnComplete = onComplete;
            }
        }
    }

    public class ExecutionState
    {
        public bool Completed => ErrorMsgs.Count > 0;
        public List<string> ErrorMsgs { get; set; } = new List<string>();

        public void AddException(Exception ex)
        {
            ErrorMsgs.Add(ex.Message);
        }
    }
}