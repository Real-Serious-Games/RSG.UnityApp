using System;

namespace RSG
{
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
}
