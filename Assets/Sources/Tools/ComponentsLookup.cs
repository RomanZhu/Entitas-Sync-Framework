using System;
using System.Collections.Generic;

namespace Sources.Tools
{
    public static class ComponentsLookup
    {
        private static readonly Dictionary<Type, int> _typeToIndex;

        static ComponentsLookup()
        {
            _typeToIndex = new Dictionary<Type, int>(GameComponentsLookup.componentTypes.Length);
            for (var i = 0; i < GameComponentsLookup.componentTypes.Length; i++)
                _typeToIndex.Add(GameComponentsLookup.componentTypes[i], i);
        }

        public static int GetIndex(Type componentType)
        {
            return _typeToIndex[componentType];
        }
    }
}