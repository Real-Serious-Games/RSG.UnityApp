using RSG.Utils;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        /// Returns true when currently loading a scene.
        /// </summary>
        bool IsUnloading { get; }

        /// <summary>
        /// The name of the scene that is being loaded.
        /// </summary>
        string LoadingSceneName { get; }

        /// <summary>
        /// The name of the scene that is being loaded.
        /// </summary>
        string UnloadingSceneName { get; }

        /// <summary>
        /// Load a scene synchronously.
        /// </summary>
        void Load(string sceneName);

        /// <summary>
        /// Load a scene asynchronously.
        /// </summary>
        IPromise LoadAsync(string sceneName);

        /// <summary>
        /// Load a scene asynchronously.
        /// </summary>
        IPromise UnloadAsync(string sceneName);

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

        /// <summary>
        /// Event raised when a new scene has started async loading.
        /// </summary>
        event EventHandler<SceneUnloadEventArgs> SceneUnloading;

        /// <summary>
        /// Event raised when a new scene is loaded.
        /// </summary>
        event EventHandler<SceneUnloadEventArgs> SceneUnloaded;
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
            private void Awake()
            {
                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            }

            private void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
            {
                if (SceneLoaded == null)
                {
                    return;
                }

                SceneLoaded(this, new SceneLoadEventArgs(scene.name));
            }

            private void OnDestroy()
            {
                SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            }
        }

        public class SceneUnloadedEventHandler : MonoBehaviour
        {
            /// <summary>
            /// Event raised when a new scene is loaded.
            /// </summary>
            public event EventHandler<SceneUnloadEventArgs> SceneUnloaded;

            //
            // Called when new level is loaded.
            // https://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnLevelWasLoaded.html
            //
            private void Awake()
            {
                SceneManager.sceneUnloaded += SceneManager_sceneUnloaded; ;
            }

            private void SceneManager_sceneUnloaded(UnityEngine.SceneManagement.Scene scene)
            {
                if (SceneUnloaded == null)
                {
                    return;
                }

                SceneUnloaded(this, new SceneUnloadEventArgs(scene.name));
            }

            private void OnDestroy()
            {
                SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
            }
        }

        private SceneLoadedEventHandler sceneLoadedEventHandler;
        private SceneUnloadedEventHandler sceneUnloadedEventHandler;

        /// <summary>
        /// Promise that is resolved when the current scene completes.
        /// </summary>
        private Promise sceneCompletedPromise;

        [Dependency]
        public Utils.ILogger Logger { get; set; }

        /// <summary>
        /// The name of the level currently scene.
        /// </summary>
        public string CurrentSceneName
        {
            get
            {
                return SceneManager.GetActiveScene().name;
            }
        }

        /// <summary>
        /// Returns true when currently loading a scene.
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// Returns true when currently unloading a scene.
        /// </summary>
        public bool IsUnloading { get; private set; }

        /// <summary>
        /// The name of the scene that is being loaded.
        /// </summary>
        public string LoadingSceneName { get; private set; }

        /// <summary>
        /// The name of the scene that is being unloaded.
        /// </summary>
        public string UnloadingSceneName { get; private set; }

        /// <summary>
        /// Load a scene synchronously.
        /// </summary>
        public void Load(string sceneName)
        {
            Argument.StringNotNullOrEmpty(() => sceneName);

            CreateSceneLoadedEventHandler();

            ExceptionIfLoading(sceneName);

            RaiseSceneLoadingEvent(sceneName);

            SceneManager.LoadScene(sceneName);
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
        /// Unload a scene asynchronously.
        /// </summary>
        public IPromise UnloadAsync(string sceneName)
        {
            Argument.StringNotNullOrEmpty(() => sceneName);

            CreateSceneUnloadedEventHandler();

            var promise = new Promise();
            sceneUnloadedEventHandler.StartCoroutine(LoadAsyncCoroutine(sceneName, () => promise.Resolve()));
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
        /// Create the GameObject/Component that gets the callback when a scene is loaded.
        /// </summary>
        private void CreateSceneUnloadedEventHandler()
        {
            if (sceneUnloadedEventHandler == null)
            {
                sceneUnloadedEventHandler = new GameObject("_SceneUnoadedEventHandler").AddComponent<SceneUnloadedEventHandler>();
                GameObject.DontDestroyOnLoad(sceneUnloadedEventHandler.gameObject);

                sceneUnloadedEventHandler.SceneUnloaded += sceneUnloadedEventHandler_SceneUnloaded;
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
        /// Event raised when a new scene has started async unloaded.
        /// </summary>
        public event EventHandler<SceneUnloadEventArgs> SceneUnloading;

        /// <summary>
        /// Event raised when a new scene is unloaded.
        /// </summary>
        public event EventHandler<SceneUnloadEventArgs> SceneUnloaded;

        private void sceneUnloadedEventHandler_SceneUnloaded(object sender, SceneUnloadEventArgs e)
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
        /// Called when a new scene is unloaded.
        /// </summary>
        private void NotifySceneUnloaded(string sceneName)
        {
            Logger.LogInfo("Scene Unloaded: " + sceneName);

            if (SceneUnloaded != null)
            {
                SceneUnloaded(this, new SceneUnloadEventArgs(sceneName));
            }
        }

        /// <summary>
        /// Called when async loading has started.
        /// </summary>
        private void StartLoading(string sceneName, string loadType)
        {
            Logger.LogInfo("Loading scene (" + loadType + "): " + CurrentSceneName + " -> " + sceneName);

            IsLoading = true;
            LoadingSceneName = sceneName;

            RaiseSceneLoadingEvent(sceneName);
        }

        /// <summary>
        /// Called when async unloading has started.
        /// </summary>
        private void StartUnloading(string sceneName, string unloadType)
        {
            Logger.LogInfo("Unloading scene (" + unloadType + "): " + CurrentSceneName + " -> " + sceneName);

            IsUnloading = true;
            UnloadingSceneName = sceneName;

            RaiseSceneUnloadingEvent(sceneName);
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
        /// Raises the scene loading event.
        /// </summary>
        private void RaiseSceneUnloadingEvent(string sceneName)
        {
            if (SceneUnloading != null)
            {
                SceneUnloading(this, new SceneUnloadEventArgs(sceneName));
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
        /// Raise an exception if already unloading a scene.
        /// </summary>
        private void ExceptionIfUnloading(string sceneName)
        {
            if (IsUnloading)
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
        /// Called when async loading has completed.
        /// </summary>
        private void DoneUnloading(Action doneCallback)
        {
            IsUnloading = false;
            UnloadingSceneName = null;

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
            ExceptionIfLoading(sceneName);

            StartLoading(sceneName, "async");

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            DoneLoading(doneCallback);
        }

        /// <summary>
        /// Coroutine to do an async additive load.
        /// </summary>
        private IEnumerator LoadAsyncAdditiveCoroutine(string sceneName, Action doneCallback)
        {
            StartLoading(sceneName, "async additive");

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            DoneLoading(doneCallback);
        }

        /// <summary>
        /// Coroutine to do an async unload.
        /// </summary>
        private IEnumerator UnloadAsyncCoroutine(string sceneName, Action doneCallback)
        {
            ExceptionIfUnloading(sceneName);

            StartUnloading(sceneName, "async");

            yield return SceneManager.UnloadSceneAsync(sceneName);

            DoneUnloading(doneCallback);
        }

    }
}
