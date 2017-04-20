using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
{
    /// <summary>
    /// Interface to the dispatcher.
    /// Executes tasks asynchronously on the main thread.
    /// </summary>
    public interface IDispatcher
    {
        void InvokeAsync(Action action);
    }

    /// <summary>
    /// Dispatches tasks immediately, for use in tests and the command line test bed.
    /// </summary>
    public class ImmediateDispatcher : IDispatcher
    {
        public void InvokeAsync(Action action)
        {
            action.Invoke();
        }
    }

    /// <summary>
    /// Represents the queue of dispatched actions.
    /// </summary>
    public interface IDispatchQueue
    {
        
        /// <summary>
        /// Execute all pending actions.
        /// </summary>
        void ExecutePending();
    }

    /// <summary>
    /// Executes tasks asynchronously on the main thread.
    /// Required by unity so that the WebUI can trigger actions on the main thread.
    /// </summary>
    public class Dispatcher : IDispatcher, IDispatchQueue
    {
        /// <summary>
        /// List of pending actions.
        /// </summary>
        private List<Action> pending = new List<Action>(); //todo: this could be a lock free queue.

        private ILogger logger;

        public Dispatcher(ILogger logger)
        {
            Argument.NotNull(() => logger);

            this.logger = logger;
        }

        public void InvokeAsync(Action action)
        {
            Argument.NotNull(() => action);

            lock (pending)
            {
                pending.Add(action);
            }
        }

        /// <summary>
        /// Execute all pending actions.
        /// </summary>
        public void ExecutePending()
        {
            while (pending.Count > 0)
            {
                Action[] working = null;


                lock (pending)
                {
                    working = pending.ToArray();
                    pending.Clear();
                }

                foreach (var action in working)
                {
                    InvokeAsyncAction(action);
                }
            }
        }

        /// <summary>
        /// Invoke a single action action.
        /// </summary>
        private void InvokeAsyncAction(Action action)
        {
            Argument.NotNull(() => action);

            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                logger.LogError("Error During Dispatcher Invoke! " + e);
            }
        }
    }
}
