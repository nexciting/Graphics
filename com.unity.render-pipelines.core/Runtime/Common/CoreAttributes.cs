using System;

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
        /// Constructor of the attribute
        /// </summary>
        /// <param name="componentType">The component type</param>
        public AdditionalComponentData(Type componentType)
        {
            this.componentType = componentType;
        }
    }
}
