using UnityEngine;
using WhateverDevs.Core.Runtime.Common;
using WhateverDevs.Core.Runtime.DataStructures;

namespace WhateverDevs.EnvironmentCollidersGenerator.Editor
{
    /// <summary>
    /// Library that contains configuration and references to be used by the environment colliders generator tool.
    /// </summary>
    public class EnvironmentCollidersGeneratorLibrary : LoggableScriptableObject<EnvironmentCollidersGeneratorLibrary>
    {
        /// <summary>
        /// Relation between tags and physic materials to add to the colliders created with those tags.
        /// TODO: Change this string to a tag reference or something so only tags can be selected. This should be done in the core.
        /// </summary>
        public SerializableDictionary<string, PhysicMaterial> TagToPhysicMaterial =
            new SerializableDictionary<string, PhysicMaterial>();
    }
}