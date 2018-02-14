using RSG.Utils;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RSG
{
    /// <summary>
    /// Exceptions to be thrown by SceneLoader
    /// </summary>
    public class SceneLoaderException : Exception
    {
        public SceneLoaderException(string message) : base(message)
        {
        }
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
        
        private class SceneEventHandler: MonoBehaviour
        {
            public event EventHandler<SceneEventArgs> SceneUnloaded;
            public event EventHandler<SceneEventArgs> SceneLoaded;

            private void Awake()
            {
                SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            }

            private void SceneManager_sceneUnloaded(UnityEngine.SceneManagement.Scene scene)
            {
                if (SceneUnloaded == null)
                {
                    return;
                }

                SceneUnloaded(this, new SceneEventArgs(scene.name));
            }

            private void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
            {
                if (SceneLoaded == null)
                {
                    return;
                }

                SceneLoaded(this, new SceneEventArgs(scene.name));
            }

            private void OnDestroy()
            {
                SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
                SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            }
        }

        private SceneEventHandler sceneLoadedEventHandler;
        private SceneEventHandler sceneUnloadedEventHandler;

        /// <summary>
        /// Promise that is resolved when the current scene completes.
        /// </summary>
        private Promise sceneCompletedPromise;

        [Dependency]
        public Utils.ILogger Logger { get; set; }

        /// <summary>
        /// The name of the level currently scene.
        /// </summary>
        public string CurrentSceneName => SceneManager.GetActiveScene().name;

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

            if (IsLoading)
            {
                throw new SceneLoaderException(
                    "Requested load of scene: " + sceneName + ", but already loading: " + LoadingSceneName);
            }

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
            sceneLoadedEventHandler.StartCoroutine(LoadAsyncCoroutine(sceneName, promise));
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

            sceneUnloadedEventHandler.StartCoroutine(UnloadAsyncCoroutine(sceneName, promise));

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
            if (sceneLoadedEventHandler != null)
            {
                return;
            }

            sceneLoadedEventHandler = new GameObject("_SceneLoadedEventHandler").AddComponent<SceneEventHandler>();
            GameObject.DontDestroyOnLoad(sceneLoadedEventHandler.gameObject);

            sceneLoadedEventHandler.SceneLoaded += sceneLoadedEventHandler_SceneLoaded;
        }

        /// <summary>
        /// Create the GameObject/Component that gets the callback when a scene is unloaded.
        /// </summary>
        private void CreateSceneUnloadedEventHandler()
        {
            if (sceneUnloadedEventHandler != null)
            {
                return;
            }

            sceneUnloadedEventHandler = new GameObject("_SceneUnloadedEventHandler").AddComponent<SceneEventHandler>();
            GameObject.DontDestroyOnLoad(sceneUnloadedEventHandler.gameObject);

            sceneUnloadedEventHandler.SceneUnloaded += sceneUnloadedEventHandler_SceneUnloaded;
        }

        /// <summary>
        /// Event raised when a new scene has started async loading.
        /// </summary>
        public event EventHandler<SceneEventArgs> SceneLoading;

        /// <summary>
        /// Event raised when a new scene is loaded.
        /// </summary>
        public event EventHandler<SceneEventArgs> SceneLoaded;

        private void sceneLoadedEventHandler_SceneLoaded(object sender, SceneEventArgs e) => 
            NotifySceneLoaded(e.SceneName);

        /// <summary>
        /// Event raised when a new scene has started async unloaded.
        /// </summary>
        public event EventHandler<SceneEventArgs> SceneUnloading;

        /// <summary>
        /// Event raised when a new scene is unloaded.
        /// </summary>
        public event EventHandler<SceneEventArgs> SceneUnloaded;

        private void sceneUnloadedEventHandler_SceneUnloaded(object sender, SceneEventArgs e) =>
            NotifySceneUnloaded(e.SceneName);

        /// <summary>
        /// Called when a new scene is loaded.
        /// </summary>
        private void NotifySceneLoaded(string sceneName)
        {
            Logger.LogInfo("Scene Loaded: " + sceneName);

            SceneLoaded?.Invoke(this, new SceneEventArgs(sceneName));
        }

        /// <summary>
        /// Called when a new scene is unloaded.
        /// </summary>
        private void NotifySceneUnloaded(string sceneName)
        {
            Logger.LogInfo("Scene Unloaded: " + sceneName);

            SceneUnloaded?.Invoke(this, new SceneEventArgs(sceneName));
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
        private void StartUnloading(string sceneName)
        {
            Logger.LogInfo("Unloading scene (async): " + CurrentSceneName + " -> " + sceneName);

            IsUnloading = true;
            UnloadingSceneName = sceneName;

            RaiseSceneUnloadingEvent(sceneName);
        }

        /// <summary>
        /// Raises the scene loading event.
        /// </summary>
        private void RaiseSceneLoadingEvent(string sceneName) =>
            SceneLoading?.Invoke(this, new SceneEventArgs(sceneName));

        /// <summary>
        /// Raises the scene loading event.
        /// </summary>
        private void RaiseSceneUnloadingEvent(string sceneName) =>
            SceneUnloading?.Invoke(this, new SceneEventArgs(sceneName));

        /// <summary>
        /// Called when async loading has completed.
        /// </summary>
        private void DoneLoading(Action doneCallback)
        {
            IsLoading = false;
            LoadingSceneName = null;

            doneCallback();
        }

        /// <summary>
        /// Called when async unloading has completed.
        /// </summary>
        private void DoneUnloading(Action doneCallback)
        {
            IsUnloading = false;
            UnloadingSceneName = null;

            doneCallback();
        }

        /// <summary>
        /// Coroutine to do an async load.
        /// </summary>
        private IEnumerator LoadAsyncCoroutine(string sceneName, Promise result)
        {
            if (IsLoading)
            {
                DoneLoading(() => result.Reject(new SceneLoaderException(
                    "Requested load of scene: " + sceneName + ", but already loading: " + LoadingSceneName)));
            }

            StartLoading(sceneName, "async");

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            DoneLoading(() => result.Resolve());
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
        private IEnumerator UnloadAsyncCoroutine(string sceneName, Promise result)
        {
            if (!IsSceneLoaded(sceneName))
            {
                DoneUnloading(() => result.Reject(new SceneLoaderException(
                    "Requested unload of scene: " + sceneName + ", but scene isn't loaded")));
            }
            else if (IsUnloading)
            {
                DoneUnloading(() => result.Reject(new SceneLoaderException(
                    "Requested unload of scene: " + sceneName + ", but already unloading: " + LoadingSceneName)));
            }
            else
            {
                StartUnloading(sceneName);

                yield return SceneManager.UnloadSceneAsync(sceneName);

                DoneUnloading(() => result.Resolve());
            }
        }

        /// <summary>
        /// Check if scene is loaded
        /// </summary>
        public bool IsSceneLoaded(string sceneName)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (scene.name == sceneName)
                {
                    return true;
                }
            };

            return false;
        }
    }
}
