using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RSG
{
    /// <summary>
    /// Interface for procedural creation and management of game objects.
    /// All procedural game objects are automatically cleaned up on shutdown.
    /// </summary>
    public interface IGameObjectCreator
    {
        /// <summary>
        /// Instantiate one game object from another.
        /// Game object is parented under the 'procedural game object parent'.
        /// </summary>
        GameObject Instantiate(string name, GameObject source);

        /// <summary>
        /// Instantiate one game object from another under a particular parent.
        /// </summary>
        GameObject Instantiate(string name, GameObject source, GameObject parent);

        /// <summary>
        /// Create a named object under a particular parent
        /// </summary>
        GameObject Create(string name, GameObject parent);

        /// <summary>
        /// Create a named object.
        /// Game object is parented under the 'procedural game object parent'.
        /// </summary>
        GameObject Create(string name);

        /// <summary>
        /// Create a permanent game object that can survive 'scene reload'.
        /// Game object is parented under the 'procedural game object parent'.
        /// </summary>
        GameObject CreatePermanent(string name);

        /// <summary>
        /// Load a game object using Unity's resource API
        /// </summary>
        GameObject Load(string newGameObjectName, string resourcePath);
    }

    /// <summary>
    /// Interface for procedural creation and management of game objects.
    /// All procedural game objects are automatically cleaned up on shutdown.
    /// </summary>
    [Singleton(typeof(IGameObjectCreator))]
    public class GameObjectCreator : IGameObjectCreator, IStartable
    {
        private static readonly string ProceduralGameObjectsRootName = "_Procedural";

        private static readonly string PermanentProceduralGameObjectsRootName = "_PermanentProcedural";

        /// <summary>
        /// Instantiate one game object from another.
        /// </summary>
        public GameObject Instantiate(string name, GameObject source)
        {
            Argument.StringNotNullOrEmpty(() => name);
            Argument.NotNull(() => source);

            return Instantiate(name, source, GetProceduralRootObject());
        }

        /// <summary>
        /// Instantiate one game object from another under a particular parent.
        /// </summary>
        public GameObject Instantiate(string name, GameObject source, GameObject parent)
        {
            Argument.StringNotNullOrEmpty(() => name);
            Argument.NotNull(() => source);
            Argument.NotNull(() => parent);

            var gameObject = (GameObject)GameObject.Instantiate(source);
            gameObject.transform.parent = parent.transform;
            return gameObject;
        }

        /// <summary>
        /// Create a named object under a particular parent
        /// </summary>
        public GameObject Create(string name, GameObject parent)
        {
            Argument.StringNotNullOrEmpty(() => name);
            Argument.NotNull(() => parent);

            var gameObject = new GameObject(name);
            gameObject.transform.parent = parent.transform;
            return gameObject;
        }

        /// <summary>
        /// Create a named object.
        /// </summary>
        public GameObject Create(string name)
        {
            Argument.StringNotNullOrEmpty(() => name);

            return Create(name, GetProceduralRootObject());
        }

        /// <summary>
        /// Load a game object using Unity's resource API
        /// </summary>
        public GameObject Load(string newGameObjectName, string resourcePath)
        {
            Argument.StringNotNullOrEmpty(() => newGameObjectName);
            Argument.StringNotNullOrEmpty(() => resourcePath);

            var loadedObj = Resources.Load(resourcePath);

            if (loadedObj == null)
            {
                throw new ApplicationException("Failed to load assets from path: " + resourcePath);
            }

            return Instantiate(newGameObjectName, (GameObject)loadedObj);
        }

        /// <summary>
        /// Helper function to find/create a procedural root object.
        /// </summary>
        private GameObject GetProceduralRootObject()
        {
            var parent = GameObject.Find(ProceduralGameObjectsRootName);
            if (parent == null)
            {
                parent = new GameObject(ProceduralGameObjectsRootName);
            }
            return parent;
        }

        /// <summary>
        /// Create a permanent game object that can survive 'scene reload'.
        /// Game object is parented under the 'procedural game object parent'.
        /// </summary>
        public GameObject CreatePermanent(string name)
        {
            Argument.StringNotNullOrEmpty(() => name);

            return CreatePermanentGameObject(name);
        }

        /// <summary>
        /// Global function for creating a permanent game object, used to create Unity singletons.
        /// </summary>
        public static GameObject CreatePermanentGameObject(string name)
        {
            var parent = GetPermanentProceduralRootObject();
            var gameObject = new GameObject(name);
            gameObject.transform.parent = parent.transform;
            gameObject.hideFlags = HideFlags.DontSave;
            GameObject.DontDestroyOnLoad(gameObject);
            return gameObject;
        }

        /// <summary>
        /// Helper function to find/create a procedural root object.
        /// </summary>
        public static GameObject GetPermanentProceduralRootObject()
        {
            var parent = GameObject.Find(PermanentProceduralGameObjectsRootName);
            if (parent == null)
            {
                parent = new GameObject(PermanentProceduralGameObjectsRootName);
                parent.hideFlags = HideFlags.DontSave;
                GameObject.DontDestroyOnLoad(parent);
            }
            return parent;
        }

        public void Startup()
        {
        }

        /// <summary>
        /// Shutdown and destroy all permanent procedural game objects.
        /// </summary>
        public void Shutdown()
        {
            var parent = GameObject.Find(PermanentProceduralGameObjectsRootName);
            if (parent != null)
            {
                GameObject.DestroyImmediate(parent);
            }
        }
    }
}
