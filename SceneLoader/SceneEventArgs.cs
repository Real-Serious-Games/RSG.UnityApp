using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RSG
{
    public class SceneEventArgs : EventArgs
    {
        public string[] SceneNames { get; }

        public SceneEventArgs(string sceneName)
        {
            Argument.StringNotNullOrEmpty(() => sceneName);

            this.SceneNames = new [] { sceneName };
        }

        public SceneEventArgs(IEnumerable<string> sceneNames)
        {
            this.SceneNames = sceneNames.ToArray();
        }
    }
}
