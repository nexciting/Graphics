using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UnityEditor.Rendering
{
    /// <summary>
    /// Helper methods for overriding contextual menus
    /// </summary>
    public static class ContextualMenuDispatcher
    {
        public static void RemoveComponent<T, U>(MenuCommand command)
            where T : Component
            where U : Component
        {
            T comp = command.context as T;
            string error;

            if (!DispatchRemoveComponent<T, U>(comp))
            {
                //preserve built-in behavior
                if (CanRemoveComponent(comp, out error))
                    Undo.DestroyObjectImmediate(command.context);
                else
                    EditorUtility.DisplayDialog("Can't remove component", error, "Ok");
            }
        }

        static bool DispatchRemoveComponent<T, U>(T component)
            where T : Component
            where U : Component
        {
            Type type = RenderPipelineEditorUtility.FetchFirstCompatibleTypeUsingScriptableRenderPipelineExtension<IRemoveAdditionalDataContextualMenu<T, U>>();
            if (type != null)
            {
                IRemoveAdditionalDataContextualMenu<T, U> instance = (IRemoveAdditionalDataContextualMenu<T, U>)Activator.CreateInstance(type);
                instance.RemoveComponent(component, ComponentDependencies(component));
                return true;
            }
            return false;
        }

        static IEnumerable<Component> ComponentDependencies(Component component)
            => component.gameObject
            .GetComponents<Component>()
            .Where(c => c != component
                && c.GetType()
                    .GetCustomAttributes(typeof(RequireComponent), true)
                    .Count(att => att is RequireComponent rc
                        && (rc.m_Type0 == component.GetType()
                            || rc.m_Type1 == component.GetType()
                            || rc.m_Type2 == component.GetType())) > 0);

        static bool CanRemoveComponent(Component component, out string error)
        {
            var dependencies = ComponentDependencies(component);
            if (dependencies.Count() == 0)
            {
                error = null;
                return true;
            }

            Component firstDependency = dependencies.First();
            error = $"Can't remove {component.GetType().Name} because {firstDependency.GetType().Name} depends on it.";
            return false;
        }
    }

    /// <summary>
    /// Interface that should be used with [ScriptableRenderPipelineExtension(type))] attribute to dispatch ContextualMenu calls on the different SRPs
    /// </summary>
    /// <typeparam name="T">This must be a component that require AdditionalData in your SRP</typeparam>
    [Obsolete("Use the IRemoveAdditionalDataContextualMenu<T, U> instead.")]
    public interface IRemoveAdditionalDataContextualMenu<T>
        where T : Component
    {
        /// <summary>
        /// Remove the given component
        /// </summary>
        /// <param name="component">The component to remove</param>
        /// <param name="dependencies">Dependencies.</param>
        void RemoveComponent(T component, IEnumerable<Component> dependencies);
    }

    /// <summary>
    /// Interface that should be used with [ScriptableRenderPipelineExtension(type))] attribute to dispatch ContextualMenu calls on the different SRPs
    /// </summary>
    /// <typeparam name="T">This must be a component that require AdditionalData in your SRP</typeparam>
    /// <typeparam name="U">This is the AdditionalData</typeparam>
    public interface IRemoveAdditionalDataContextualMenu<T, U>
        where T : Component
        where U : Component
    {
        //The call is delayed to the dispatcher to solve conflict with other SRP
        public void RemoveComponent(T component, IEnumerable<Component> dependencies)
        {
            // do not use keyword is to remove the additional data. It will not work
            dependencies = dependencies.Where(c => c.GetType() != typeof(U)).ToList();
            if (dependencies.Any())
            {
                EditorUtility.DisplayDialog("Can't remove component", $"Can't remove {typeof(T)} because {dependencies.First().GetType().Name} depends on it.", "Ok");
                return;
            }

            var isAssetEditing = EditorUtility.IsPersistent(component);
            try
            {
                if (isAssetEditing)
                {
                    AssetDatabase.StartAssetEditing();
                }
                Undo.SetCurrentGroupName($"Remove {typeof(U)}");
                var additionalDataComponent = component.GetComponent<U>();
                if (additionalDataComponent != null)
                {
                    Undo.DestroyObjectImmediate(additionalDataComponent);
                }
                Undo.DestroyObjectImmediate(component);
            }
            finally
            {
                if (isAssetEditing)
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }
    }
}
