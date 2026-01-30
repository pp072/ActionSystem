using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPath("Template/Test")]
    public class ActionTest : ActionItemBase
    {
        [SerializeField] private GameObject _gameObject;
        [SerializeField] private Color _color;
        [SerializeField] private bool Enable = true;
        //[SerializeField] private FloatField _floatField;

        public override async UniTask<bool> Run()
        {
            Debug.Log("test");
            //_gameObject.SetActive(Enable);
            return true;
        }
    }
}
