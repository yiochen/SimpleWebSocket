using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SimpleWebSocket
{
    // implementation from https://stackoverflow.com/a/41333540/3429675
    internal class UnityThread : MonoBehaviour
    {
        //our (singleton) instance
        private static UnityThread instance = null;

        //Holds actions received from another Thread. Will be coped to processingActions then executed from there
        private static List<Action> queuedActions = new List<Action>();

        //holds Actions copied from queuedActions to be executed
        List<Action> processingActions = new List<Action>();

        // Used to know if whe have new Action function to execute. This prevents the use of the lock keyword every frame
        private volatile static bool noActionQueued = true;

        //Used to initialize UnityThread. Call once before any function here
        public static void initUnityThread(bool visible = false)
        {
            if (instance != null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                // add an invisible game object to the scene
                GameObject obj = new GameObject("MainThreadExecuter");
                if (!visible)
                {
                    obj.hideFlags = HideFlags.HideAndDontSave;
                }

                DontDestroyOnLoad(obj);
                instance = obj.AddComponent<UnityThread>();
            }
        }

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        public static void executeCoroutine(IEnumerator action)
        {
            if (instance != null)
            {
                executeInUpdate(() => instance.StartCoroutine(action));
            }
        }

        public static void executeInUpdate(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            lock (queuedActions)
            {
                queuedActions.Add(action);
                noActionQueued = false;
            }
        }

        public void Update()
        {
            if (noActionQueued)
            {
                return;
            }

            //Clear the old actions from the processingActions queue
            processingActions.Clear();
            lock (queuedActions)
            {
                //Copy queuedActions to the processingActions variable
                processingActions.AddRange(queuedActions);
                //Now clear the queuedActions since we've done copying it
                queuedActions.Clear();
                noActionQueued = true;
            }

            // Loop and execute the functions from the processingActions
            foreach (Action action in processingActions)
            {
                action.Invoke();
            }
        }

        public void OnDisable()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}