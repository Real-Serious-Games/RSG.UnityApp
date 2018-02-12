using RSG.Utils;
using System;

namespace RSG
{
    /// <summary>
    /// Args for the SceneUnloaded event.
    /// </summary>
    public class SceneUnloadEventArgs : EventArgs
    {
        /// <summary>
        /// The name of the scene that was loaded.
        /// </summary>
        public string SceneName { get; private set; }

        public SceneUnloadEventArgs(string sceneName)
        {
            Argument.StringNotNullOrEmpty(() => sceneName);

            this.SceneName = sceneName;
        }
    }
}
