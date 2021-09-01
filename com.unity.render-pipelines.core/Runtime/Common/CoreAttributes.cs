using System;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Attribute used to customize UI display.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DisplayInfoAttribute : Attribute
    {
        /// <summary>Display name used in UI.</summary>
        public string name;
        /// <summary>Display order used in UI.</summary>
        public int order;
    }

    /// <summary>
    /// Attribute used to customize UI display to allow properties only be visible when "Show Additional Properties" is selected
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AdditionalPropertyAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute to specify that a class is additional data of another component
    /// Currently is being used for HDRP/URP additional Camera and Light data
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AdditionalComponentData : Attribute
    {
        /// <summary>
        /// Summary the component type of is additional data
        /// </summary>
        public Type componentType;

        /// <summary>
        /// Summary the additional component type
        /// </summary>
        public Type additionalDataComponentType;

        /// <summary>
        /// Constructor of the attribute
        /// </summary>
        /// <param name="componentType">The component type</param>
        public AdditionalComponentData(Type componentType, Type additionalDataComponentType)
        {
            this.componentType = componentType;
            this.additionalDataComponentType = additionalDataComponentType;

#if UNITY_EDITOR
            CoreUtils.RegisterMenu($"CONTEXT/{additionalDataComponentType.Name}/Remove Component",
                () => EditorUtility.DisplayDialog($"Remove {additionalDataComponentType.Name} is blocked", $"You can not delete this component, you will have to remove the {componentType.Name}.", "OK"));
#endif
        }

    }
}
