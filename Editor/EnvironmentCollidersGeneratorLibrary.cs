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
        /// </summary>
        public SerializableDictionary<Tag, PhysicMaterial> TagToPhysicMaterial =
            new SerializableDictionary<Tag, PhysicMaterial>();
    }
}