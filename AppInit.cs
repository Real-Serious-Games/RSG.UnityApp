using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RSG.Unity
{
    /// <summary>
    /// This MonoBehavior must be manually added to the GameObject in the Unity scene to bootstrap the RSG application.
    /// </summary>
    public class AppInit : MonoBehaviour
    {
        void Awake()
        {
            App.Init();
        }
    }
}
