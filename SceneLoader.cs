using RSG;
using RSG.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RSG.Unity
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

    /// <summary>
    /// Interface Unity scene loading.
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>
        /// The name of the level currently scene.
        /// </summary>
        string CurrentSceneName { get; }

        /// <summary>
        /// Returns true when currently loading a scene.
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// The name of the scene that is being loaded.
        /// </summary>
        string LoadingSceneName { get; }

        /// <summary>
        /// Load a scene synchronously.
        /// </summary>
        void Load(string sceneName);

        /// <summary>
        /// Load a scene asynchronously.
        /// </summary>
        IPromise LoadAsync(string sceneName);

        /// <summary>
        /// Load a scene asynchronously and merge it to the current scene.
        /// </summary>
        IPromise LoadAsyncAdditive(string sceneName);

        /// <summary>
        /// Resolves the promise when the current scene has completed.
        /// </summary>
        IPromise WaitUntilCurrentSceneCompleted();

        /// <summary>
        /// Notify that the current scene has complete. 
        /// </summary>
        void NotifyCurrentSceneCompleted();

        /// <summary>
        /// Event raised when a new scene has started async loading.
        /// </summary>
        event EventHandler<SceneLoadEventArgs> SceneLoading;

        /// <summary>
        /// Event raised when a new scene is loaded.
        /// </summary>
        event EventHandler<SceneLoadEventArgs> SceneLoaded;
    }

    /// <summary>
    /// Wrapper for Unity scene loading.
    /// </summary>
    [LazySingleton(typeof(ISceneLoader))]
    public class SceneLoader : ISceneLoader
    {
        //
        // When other singletons were changed to depend on scene config which depends on the scene loader it
        // borked the order of creation of singletons.
        // Had to change this singleton to be a regular non-Unity singleton to break the circular dependency.
        // This won't be an issue if all singletons (both Unity and non-Unity) are created as one sorted list.
        //
        public class SceneLoadedEventHandler : MonoBehaviour
        {
            /// <summary>
            /// Event raised when a new scene is loaded.
            /// </summary>
            public event EventHandler<SceneLoadEventArgs> SceneLoaded;

            //
            // Called when new level is loaded.
            // https://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnLevelWasLoaded.html
            //
            protected void OnLevelWasLoaded(int level)
            {
                if (SceneLoaded != null)
                {
                    SceneLoaded(this, new SceneLoadEventArgs(Application.loadedLevelName));
                }
            }
        }

        private SceneLoadedEventHandler sceneLoadedEventHandler;

        /// <summary>
        /// Promise that is resolved when the current scene completes.
        /// </summary>
        private Promise sceneCompletedPromise;

        [Dependency]
        public ILogger Logger { get; set; }

        /// <summary>
        /// The name of the level currently scene.
        /// </summary>
        public string CurrentSceneName
        {
            get
            {
                return Application.loadedLevelName;
            }
        }

        /// <summary>
        /// Returns true when currently loading a scene.
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// The name of the scene that is being loaded.
        /// </summary>
        public string LoadingSceneName { get; private set; }

        /// <summary>
        /// Load a scene synchronously.
        /// </summary>
        public void Load(string sceneName)
        {
            Argument.StringNotNullOrEmpty(() => sceneName);

            CreateSceneLoadedEventHandler();

            ExceptionIfLoading(sceneName);

            RaiseSceneLoadingEvent(sceneName);

            Application.LoadLevel(sceneName);
        }

        /// <summary>
        /// Load a scene asynchronously.
        /// </summary>
        public IPromise LoadAsync(string sceneName)
        {
            Argument.StringNotNullOrEmpty(() => sceneName);

            CreateSceneLoadedEventHandler();

            var promise = new Promise();
            sceneLoadedEventHandler.StartCoroutine(LoadAsyncCoroutine(sceneName, () => promise.Resolve()));
            return promise;
        }

        /// <summary>
        /// Load a scene asynchronously and merge it to the current scene.
        /// </summary>
        public IPromise LoadAsyncAdditive(string sceneName)
        {
            Argument.StringNotNullOrEmpty(() => sceneName);

            CreateSceneLoadedEventHandler();

            var promise = new Promise();
            sceneLoadedEventHandler.StartCoroutine(LoadAsyncAdditiveCoroutine(sceneName, () => promise.Resolve()));
            return promise;
        }

        /// <summary>
        /// Resolves the promise when the current scene has completed.
        /// </summary>
        public IPromise WaitUntilCurrentSceneCompleted()
        {
            if (sceneCompletedPromise == null)
            {
                sceneCompletedPromise = new Promise();
            }

            return sceneCompletedPromise;
        }

        /// <summary>
        /// Notify that the current scene has complete. 
        /// </summary>
        public void NotifyCurrentSceneCompleted()
        {
            if (sceneCompletedPromise != null)
            {
                sceneCompletedPromise.Resolve();
                sceneCompletedPromise = null;
            }
        }

        /// <summary>
        /// Create the GameObject/Component that gets the callback when a scene is loaded.
        /// </summary>
        private void CreateSceneLoadedEventHandler()
        {
            if (sceneLoadedEventHandler == null)
            {
                sceneLoadedEventHandler = new GameObject("_SceneLoadedEventHandler").AddComponent<SceneLoadedEventHandler>();
                GameObject.DontDestroyOnLoad(sceneLoadedEventHandler.gameObject);

                sceneLoadedEventHandler.SceneLoaded += sceneLoadedEventHandler_SceneLoaded;
            }
        }

        /// <summary>
        /// Event raised when a new scene has started async loading.
        /// </summary>
        public event EventHandler<SceneLoadEventArgs> SceneLoading;

        /// <summary>
        /// Event raised when a new scene is loaded.
        /// </summary>
        public event EventHandler<SceneLoadEventArgs> SceneLoaded;

        private void sceneLoadedEventHandler_SceneLoaded(object sender, SceneLoadEventArgs e)
        {
            NotifySceneLoaded(e.SceneName);
        }

        /// <summary>
        /// Called when a new scene is loaded.
        /// </summary>
        private void NotifySceneLoaded(string sceneName)
        {
            Logger.LogInfo("Scene Loaded: " + sceneName);

            if (SceneLoaded != null)
            {
                SceneLoaded(this, new SceneLoadEventArgs(sceneName));
            }
        }

        /// <summary>
        /// Called when async loading has started.
        /// </summary>
        private void StartLoading(string sceneName, string loadType)
        {
            ExceptionIfLoading(sceneName);

            Logger.LogInfo("Loading scene (" + loadType + "): " + CurrentSceneName + " -> " + sceneName);

            IsLoading = true;
            LoadingSceneName = sceneName;

            RaiseSceneLoadingEvent(sceneName);
        }

        /// <summary>
        /// Raises the scene loading event.
        /// </summary>
        private void RaiseSceneLoadingEvent(string sceneName)
        {
            if (SceneLoading != null)
            {
                SceneLoading(this, new SceneLoadEventArgs(sceneName));
            }
        }

        /// <summary>
        /// Raise an exception if already loading a scene.
        /// </summary>
        private void ExceptionIfLoading(string sceneName)
        {
            if (IsLoading)
            {
                throw new ApplicationException("Requested load of scene: " + sceneName + ", but already loading: " + LoadingSceneName);
            }
        }

        /// <summary>
        /// Called when async loading has completed.
        /// </summary>
        private void DoneLoading(Action doneCallback)
        {
            IsLoading = false;
            LoadingSceneName = null;

            //
            // Invoke callback.
            //
            doneCallback();
        }

        /// <summary>
        /// Coroutine to do an async load.
        /// </summary>
        private IEnumerator LoadAsyncCoroutine(string sceneName, Action doneCallback)
        {
            StartLoading(sceneName, "async");

            yield return Application.LoadLevelAsync(sceneName);

            DoneLoading(doneCallback);
        }

        /// <summary>
        /// Coroutine to do an async additive load.
        /// </summary>
        private IEnumerator LoadAsyncAdditiveCoroutine(string sceneName, Action doneCallback)
        {
            StartLoading(sceneName, "async additive");

            yield return Application.LoadLevelAdditiveAsync(sceneName);

            DoneLoading(doneCallback);
        }

    }
}
