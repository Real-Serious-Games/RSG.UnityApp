using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RSG
{
    /// <summary>
    /// Attribute the defines a 'Unity Singleton', a MonoBehavior that is procedurally added to the scene and setup for dependency injection.
    /// </summary>
    public class UnitySingletonAttribute : SingletonAttribute
    {
        /// <summary>
        /// Name of the single permanent object in the scene that contains all Unity singletons.
        /// </summary>
        public static readonly string UnitySingletonsGameObjectName = "_UnitySingletons";

        public UnitySingletonAttribute()
        {
        }

        public UnitySingletonAttribute(Type interfaceType) :
            base(interfaceType)
        {
        }

        /// <summary>
        /// Set up a UnitySingleton for a specified type and platform.
        /// </summary>
        public UnitySingletonAttribute(Type interfaceType, params RuntimePlatform[] supportedPlatforms) :
            base(interfaceType, () => supportedPlatforms.Contains(Application.platform))
        {
        }

        public override object CreateInstance(IFactory factory, Type type)
        {
            Argument.NotNull(() => factory);
            Argument.NotNull(() => type);

            var unitySingletonHolder = GameObject.Find(UnitySingletonsGameObjectName);
            if (unitySingletonHolder == null)
            {
                unitySingletonHolder = GameObjectCreator.CreatePermanentGameObject(UnitySingletonsGameObjectName);
            }

            // Instantiate the component.
            var singletonComponent = (MonoBehaviour)unitySingletonHolder.AddComponent(type);
            if (singletonComponent == null)
            {
                throw new ApplicationException(string.Format("Failed to add Unity singleton component {0} to GameObject {1}", type.Name, unitySingletonHolder.name));
                //todo: throw new FormattedException("Failed to add Unity singleton component {SingletonTypeName} to GameObject {GameObjectName}", type.Name, unitySingletonHolder.name);
            }

            factory.ResolveDependencies(singletonComponent);

            return singletonComponent;
        }
    }
}
