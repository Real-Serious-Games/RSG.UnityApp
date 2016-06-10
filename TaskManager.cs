using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RSG.Utils;


namespace RSG
{
    public interface ITaskManager
    {
        /// <summary>
        /// Register an updatable object.
        /// </summary>
        void RegisterUpdatable(IUpdatable updatable);

        /// <summary>
        /// Unregister an updatable object.
        /// </summary>
        void UnregisterUpdatable(IUpdatable updatable);

        /// <summary>
        /// Register a late updatable object.
        /// </summary>
        void RegisterLateUpdatable(ILateUpdatable updatable);

        /// <summary>
        /// Unregister a late updatable object.
        /// </summary>
        void UnregisterLateUpdatable(ILateUpdatable updatable);

        /// <summary>
        /// Register an end of frame updatable object.
        /// </summary>
        void RegisterEndOfFrameUpdatable(IEndOfFrameUpdatable updatable);

        /// <summary>
        /// Unregister an end of frame updatable object.
        /// </summary>
        void UnregisterEndOfFrameUpdatable(IEndOfFrameUpdatable updatable);

        /// <summary>
        /// Register a renderable object.
        /// </summary>
        void RegisterRenderable(IRenderable renderable);

        /// <summary>
        /// Unegister a renderable object.
        /// </summary>
        void UnregisterRenderable(IRenderable renderable);

        /// <summary>
        /// Call Update on all registered objects
        /// </summary>        
        void Update(float deltaTime);

        /// <summary>
        /// Call LateUpdate on all registered objects
        /// </summary>        
        void LateUpdate(float deltaTime);

        /// <summary>
        /// Call EndOfFrame update on all registered objects
        /// </summary>        
        void EndOfFrame();

        /// <summary>
        /// Call all renderables to render.
        /// </summary>
        void Render();

        /// <summary>
        /// Return false to cancel application shutdown.
        /// </summary>
        bool QueryShutdown();

        /// <summary>
        /// Calls the shutdown event on the task manager.
        /// </summary>
        void NotifyShutdown();

        /// <summary>
        /// Event raised when the system is shutting down.
        /// </summary>
        event EventHandler<EventArgs> Shutdown;
    }

    [LazySingleton(typeof(ITaskManager))]
    public class TaskManager : ITaskManager
    {
        /// <summary>
        /// Used for logging errors and info.
        /// </summary>
        [Dependency]
        public ILogger Logger { get; set; }

        /// <summary>
        /// Objects that need to be updated.
        /// </summary>
        private List<IUpdatable> updatables = new List<IUpdatable>();
        private List<ILateUpdatable> lateUpdatables = new List<ILateUpdatable>();
        private List<IEndOfFrameUpdatable> endOfFrameUpdatables = new List<IEndOfFrameUpdatable>();
        private List<IRenderable> renderables = new List<IRenderable>();

        /// <summary>
        /// Register an updatable object.
        /// </summary>
        public void RegisterUpdatable(IUpdatable updatable)
        {
            Argument.NotNull(() => updatable);

            if (updatables.Contains(updatable))
            {
                throw new FormattedException("Updatable {TypeName} is already registered.", updatable.GetType().Name);
            }

            this.updatables.Add(updatable);
        }

        /// <summary>
        /// Unregister an updatable object.
        /// </summary>
        public void UnregisterUpdatable(IUpdatable updatable)
        {
            Argument.NotNull(() => updatable);

            this.updatables.Remove(updatable);
        }

        /// <summary>
        /// Register a late updatable object.
        /// </summary>
        public void RegisterLateUpdatable(ILateUpdatable updatable)
        {
            Argument.NotNull(() => updatable);

            if (lateUpdatables.Contains(updatable))
            {
                throw new FormattedException("Late updatable {TypeName} is already registered.", updatable.GetType().Name);
            }

            this.lateUpdatables.Add(updatable);
        }

        /// <summary>
        /// Unregister a late updatable object.
        /// </summary>
        public void UnregisterLateUpdatable(ILateUpdatable updatable)
        {
            Argument.NotNull(() => updatable);

            this.lateUpdatables.Remove(updatable);
        }

        /// <summary>
        /// Register an end of frame updatable object.
        /// </summary>
        public void RegisterEndOfFrameUpdatable(IEndOfFrameUpdatable updatable)
        {
            Argument.NotNull(() => updatable);

            if (endOfFrameUpdatables.Contains(updatable))
            {
                throw new FormattedException("End-of-frame updatable {TypeName} is already registered.", updatable.GetType().Name);
            }

            this.endOfFrameUpdatables.Add(updatable);
        }

        /// <summary>
        /// Unregister an end of frame updatable object.
        /// </summary>
        public void UnregisterEndOfFrameUpdatable(IEndOfFrameUpdatable updatable)
        {
            Argument.NotNull(() => updatable);

            this.endOfFrameUpdatables.Remove(updatable);
        }

        /// <summary>
        /// Register a renderable object.
        /// </summary>
        public void RegisterRenderable(IRenderable renderable)
        {
            Argument.NotNull(() => renderable);

            if (renderables.Contains(renderable))
            {
                throw new FormattedException("Renderable {TypeName} is already registered.", renderable.GetType().Name);
            }

            this.renderables.Add(renderable);
        }

        /// <summary>
        /// Unregister a renderable object.
        /// </summary>
        public void UnregisterRenderable(IRenderable renderable)
        {
            Argument.NotNull(() => renderable);

            this.renderables.Remove(renderable);
        }

        /// <summary>
        /// Call Update on all registered objects
        /// </summary>        
        public void Update(float deltaTime)
        {
            foreach (var updateable in this.updatables.ToArray())
            {
                updateable.Update(deltaTime);
            }
        }

        /// <summary>
        /// Call LateUpdate on all registered objects
        /// </summary>        
        public void LateUpdate(float deltaTime)
        {
            foreach (var updateable in this.lateUpdatables.ToArray())
            {
                updateable.LateUpdate(deltaTime);
            }
        }

        /// <summary>
        /// Call EndOfFrame update on all registered objects
        /// </summary>        
        public void EndOfFrame()
        {
            foreach (var updateable in this.endOfFrameUpdatables.ToArray())
            {
                updateable.EndOfFrame();
            }
        }

        /// <summary>
        /// Call all renderables to render.
        /// </summary>
        public void Render()
        {
            renderables.Each(renderable => renderable.Render());
        }

        /// <summary>
        /// Return false to cancel application shutdown.
        /// </summary>
        public bool QueryShutdown()
        {
            //todo: raise an event to query observers to see if we can shutdown.
            return true;
        }

        /// <summary>
        /// Calls the shutdown event on the task manager.
        /// </summary>
        public void NotifyShutdown()
        {
            if (Shutdown != null)
            {
                Shutdown(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Event raised when the system is shutting down.
        /// </summary>
        public event EventHandler<EventArgs> Shutdown;
    }
}
