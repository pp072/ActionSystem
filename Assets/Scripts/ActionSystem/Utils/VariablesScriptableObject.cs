using System.Collections.Generic;
using ActionSystem;
using UnityEngine;

namespace Variable
{
    [CreateAssetMenu(fileName = "GlobalVariables", menuName = "ActionSystem/GlobalVariables")]
    public class VariablesScriptableObject : ScriptableObject
    {
        public List<LocalVariable> Variables = new List<LocalVariable>();

        public LocalVariable GetVariable(int index)
        {
            if (index >= 0 && index < Variables.Count)
                return Variables[index];
            return null;
        }

        public LocalVariable GetVariable(string name)
        {
            foreach (var v in Variables)
            {
                if (v.Name == name)
                    return v;
            }
            return null;
        }
    }
}