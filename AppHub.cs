using RSG.Utils;
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

        [Dependency]
        public IPromiseTimer PromiseTimer { get; set;  }

        [Dependency]
        public ILogger Logger { get; set; }

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

            SerilogHttpSink.SendBatch();

            DispatchQueue.ExecutePending();

            PromiseTimer.Update(Time.deltaTime);

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
            Logger.LogInfo("Application is shutting down...");

            hasShutdown = true;


            if (Shutdown != null)
            {
                Shutdown();
            }

            Logger.LogInfo("Flushing log and completed shutdown.");

            SerilogHttpSink.SendBatch();
        }
    }
}
