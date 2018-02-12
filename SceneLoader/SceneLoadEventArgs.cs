using RSG.Utils;
using System;

namespace RSG
{
    /// <summary>
    /// Args for the SceneLoaded event.
    /// </summary>
    public class SceneLoadEventArgs : EventArgs
    {
        /// <summary>
        /// The name of the scene that was loaded.
        /// </summary>
        public string SceneName { get; private set; }

        public SceneLoadEventArgs(string sceneName)
        {
            Argument.StringNotNullOrEmpty(() => sceneName);

            this.SceneName = sceneName;
        }
    }
}
