using UnityEngine;
using System;
using System.Collections.Generic;

namespace OmiyaGames
{
    ///-----------------------------------------------------------------------
    /// <copyright file="Singleton.cs" company="Omiya Games">
    /// The MIT License (MIT)
    /// 
    /// Copyright (c) 2014-2015 Omiya Games
    /// 
    /// Permission is hereby granted, free of charge, to any person obtaining a copy
    /// of this software and associated documentation files (the "Software"), to deal
    /// in the Software without restriction, including without limitation the rights
    /// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    /// copies of the Software, and to permit persons to whom the Software is
    /// furnished to do so, subject to the following conditions:
    /// 
    /// The above copyright notice and this permission notice shall be included in
    /// all copies or substantial portions of the Software.
    /// 
    /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    /// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    /// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    /// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    /// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    /// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    /// THE SOFTWARE.
    /// </copyright>
    /// <author>Taro Omiya</author>
    /// <date>5/18/2015</date>
    ///-----------------------------------------------------------------------
    /// <summary>
    /// Any GameObject with this script will not be destroyed when switching between
    /// scenes. However, only one instance of this script may exist in a scene.
    /// Allows retrieving any components in itself or its children.
    /// </summary>
    /// <seealso cref="ISingletonScript"/>
    public class Singleton : MonoBehaviour
    {
        private static Singleton msInstance = null;
        private readonly Dictionary<Type, Component> mCacheRetrievedComponent = new Dictionary<Type, Component>();

        public event Action<float> OnUpdate;
        public event Action<float> OnRealTimeUpdate;

        public static Singleton Instance
        {
            get
            {
                return msInstance;
            }
        }

        public static COMPONENT Get<COMPONENT>() where COMPONENT : Component
        {
            COMPONENT returnObject = null;
            Type retrieveType = typeof(COMPONENT);
            if (msInstance != null)
            {
                if (msInstance.mCacheRetrievedComponent.ContainsKey(retrieveType) == true)
                {
                    returnObject = msInstance.mCacheRetrievedComponent[retrieveType] as COMPONENT;
                }
                else
                {
                    returnObject = msInstance.GetComponentInChildren<COMPONENT>();
                    msInstance.mCacheRetrievedComponent.Add(retrieveType, returnObject);
                }
            }
            return returnObject;
        }

        // Use this for initialization
        void Awake()
        {
            int index = 0;
            ISingletonScript[] allSingletonScripts = null;
            if (msInstance == null)
            {
                // Set the instance variable
                msInstance = this;

                // Prevent this object from destroying itself
                DontDestroyOnLoad(gameObject);

                // Go through every ISingletonScript, and run singleton awake
                allSingletonScripts = GetComponentsInChildren<ISingletonScript>();
                for (index = 0; index < allSingletonScripts.Length; ++index)
                {
                    // Run singleton awake
                    allSingletonScripts[index].SingletonAwake(msInstance);
                }
            }
            else
            {
                // Destroy this gameobject
                Destroy(gameObject);

                // Retrieve the singleton script from the instance
                allSingletonScripts = msInstance.GetComponentsInChildren<ISingletonScript>();
            }

            // Go through every ISingletonScript, and run scene awake
            for (index = 0; index < allSingletonScripts.Length; ++index)
            {
                allSingletonScripts[index].SceneAwake(msInstance);
            }
        }

        void Update()
        {
            if (OnUpdate != null)
            {
                OnUpdate(Time.deltaTime);
            }
            if (OnRealTimeUpdate != null)
            {
                OnRealTimeUpdate(Time.unscaledDeltaTime);
            }
        }
    }
}
