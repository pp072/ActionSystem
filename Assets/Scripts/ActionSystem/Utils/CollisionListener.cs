using System;
using UnityEngine;

namespace ActionSystem
{
    public class CollisionListener : MonoBehaviour
    {
        public event Action<Collision> OnEnter;
        public event Action<Collision> OnExit;
        public event Action<Collision> OnStay;

        private void OnCollisionEnter(Collision other) => OnEnter?.Invoke(other);
        private void OnCollisionExit(Collision other) => OnExit?.Invoke(other);
        private void OnCollisionStay(Collision other) => OnStay?.Invoke(other);
    }
}
