using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace SerializeReferenceDropdown.Editor
{
    public class SerializeReferenceDropdownAdvancedDropdown : AdvancedDropdown
    {
        private readonly IEnumerable<string> typeNames;
        private readonly IEnumerable<Type> types;
        private readonly Action<int> onSelectedTypeIndex;

        private readonly Dictionary<AdvancedDropdownItem, int> itemAndIndexes =
            new Dictionary<AdvancedDropdownItem, int>();
        
        public SerializeReferenceDropdownAdvancedDropdown(AdvancedDropdownState state, IEnumerable<Type> types,
            Action<int> onSelectedNewType) :
            base(state)
        {
            this.types = types;
            onSelectedTypeIndex = onSelectedNewType;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Actions");
            itemAndIndexes.Clear();

            var index = 0;
            foreach (var type in types)
            {
                var name = SerializeReferenceDropdownPropertyDrawer.GetTypeName(type);
                var path = SerializeReferenceDropdownPropertyDrawer.GetTypeMenuPath(type);
                var actionName = SerializeReferenceDropdownPropertyDrawer.GetActionName(type);
                
                if (actionName != String.Empty)
                    name = actionName;
                
                var item = new AdvancedDropdownItem(name);
                itemAndIndexes.Add(item, index);
                if (path != String.Empty)
                {
                    AdvancedDropdownItem newRoot;
                    var isExists = root.children.Any(x => x.name == path);
                    if (isExists)
                    {
                        newRoot = root.children.First(x => x.name == path);
                        newRoot.AddChild(item);
                    }
                    else
                    {
                        newRoot = new AdvancedDropdownItem(path);
                        newRoot.AddChild(item);
                        root.AddChild(newRoot);
                    }
                }
                else
                {
                    root.AddChild(item);
                }
                index++;
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (itemAndIndexes.TryGetValue(item, out var index))
            {
                onSelectedTypeIndex.Invoke(index);
            }
        }
    }
}