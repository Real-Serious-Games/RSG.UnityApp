using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RSG
{
    /// <summary>
    /// Hub of the app, notifies important events.
    /// </summary>
    public interface IAppHub
    {
        /// <summary>
        /// Callback invoked on app shutdown.
        /// </summary>
        Action Shutdown { get; set; }
    }

    /// <summary>
    /// Hub of the app, notifies important events.
    /// </summary>
    [UnitySingleton(typeof(IAppHub))]
    public class AppHub : MonoBehaviour, IAppHub
    {
        [Dependency]
        public ITaskManager TaskManager { get; set; }

        /// <summary>
        /// Set to true once shutdown.
        /// </summary>
        private bool hasShutdown = false;

        /// <summary>
        /// Callback invoked on app shutdown.
        /// </summary>
        public Action Shutdown { get; set; }

        protected void Update()
        {
            if (hasShutdown)
            {
                return;
            }

            TaskManager.Update(Time.deltaTime);
        }

        protected void LateUpdate()
        {
            if (hasShutdown)
            {
                return;
            }

            TaskManager.LateUpdate(Time.deltaTime);
        }

        public void OnRenderObject()
        {
            if (hasShutdown)
            {
                return;
            }

            TaskManager.Render();
        }

        private IEnumerator EndOfFrame()
        {
            while (true)
            {
                if (hasShutdown)
                {
                    yield return null;
                }

                yield return new WaitForEndOfFrame();
                TaskManager.EndOfFrame();
            }
        }

        /// <summary>
        /// Called when the application is quiting.
        /// </summary>
        private void OnApplicationQuit()
        {
            hasShutdown = true;

            if (Shutdown != null)
            {
                Shutdown();
            }
        }
    }
}
