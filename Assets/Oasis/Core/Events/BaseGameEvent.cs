using System;
using UnityEngine;

namespace Oasis.Core
{
    public abstract class BaseGameEvent<T> : ScriptableObject
    {
        public Action<T> subscribers;

        public void Invoke(T t)
        {
            subscribers?.Invoke(t);
        }
    }
}