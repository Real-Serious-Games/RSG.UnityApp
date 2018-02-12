using RSG.Utils;
using System;

namespace RSG
{
    public class SceneEventArgs : EventArgs
    {
        public string SceneName { get; private set; }

        public SceneEventArgs(string sceneName)
        {
            Argument.StringNotNullOrEmpty(() => sceneName);

            this.SceneName = sceneName;
        }
    }
}
