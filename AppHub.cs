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
    public class AppHub : MonoBehaviour
    {
        [Dependency]
        public ITaskManager TaskManager { get; set; }

        [Dependency]
        public IDispatchQueue DispatchQueue { get; set; }

        /// <summary>
        /// Set to true once shutdown.
        /// </summary>
        private bool hasShutdown = false;

        /// <summary>
        /// Callback invoked on app shutdown.
        /// </summary>
        public Action Shutdown { get; set; }

        private void Start()
        {
            App.Instance.Factory.ResolveDependencies(this);
        }

        protected void Update()
        {
            if (hasShutdown)
            {
                return;
            }

            DispatchQueue.ExecutePending();

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
