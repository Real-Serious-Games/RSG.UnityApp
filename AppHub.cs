using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RSG.Unity
{
    /// <summary>
    /// Hub of the app, notifies important events.
    /// </summary>
    public class AppHub : MonoBehaviour
    {
        /// <summary>
        /// Callback invoked on app shutdown.
        /// </summary>
        public Action Shutdown;

        /// <summary>
        /// Called when the application is quiting.
        /// </summary>
        private void OnApplicationQuit()
        {
            if (Shutdown != null)
            {
                Shutdown();
            }
        }
    }
}
