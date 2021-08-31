using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    internal static class RemoveComponentUtils
    {
        public static IEnumerable<Component> ComponentDependencies(Component component)
           => component.gameObject
           .GetComponents<Component>()
           .Where(c => c != component
               && c.GetType()
                   .GetCustomAttributes(typeof(RequireComponent), true)
                   .Count(att => att is RequireComponent rc
                       && (rc.m_Type0 == component.GetType()
                           || rc.m_Type1 == component.GetType()
                           || rc.m_Type2 == component.GetType())) > 0);

        public static bool CanRemoveComponent(Component component, IEnumerable<Component> dependencies)
        {
            if (dependencies.Count() == 0)
                return true;

            Component firstDependency = dependencies.First();
            string error = $"Can't remove {component.GetType().Name} because {firstDependency.GetType().Name} depends on it.";
            EditorUtility.DisplayDialog("Can't remove component", error, "Ok");
            return false;
        }
    }

    /// <summary>
    /// Helper methods for overriding contextual menus
    /// </summary>
    [InitializeOnLoad]
    static class ContextualMenuDispatcher
    {
        static Action<string, string, bool, int, Action, Func<bool>> GetAddMenuItemFunction()
        {
            MethodInfo addMenuItemMethodInfo = typeof(Menu).GetMethod("AddMenuItem", BindingFlags.Static | BindingFlags.NonPublic);

            //AddMenuItem(string name, string shortcut, bool @checked, int priority, System.Action execute, System.Func<bool> validate);
            var nameParam = Expression.Parameter(typeof(string), "name");
            var shortcutParam = Expression.Parameter(typeof(string), "shortcut");
            var checkedParam = Expression.Parameter(typeof(bool), "checked");
            var priorityParam = Expression.Parameter(typeof(int), "priority");
            var executeParam = Expression.Parameter(typeof(Action), "execute");
            var validateParam = Expression.Parameter(typeof(Func<bool>), "validate");

            var expressionCall = Expression.Call(null, addMenuItemMethodInfo,
                        nameParam,
                        shortcutParam,
                        checkedParam,
                        priorityParam,
                        executeParam,
                        validateParam);

            return Expression.Lambda<Action<string, string, bool, int, Action, Func<bool>>>(
                Expression.Block(expressionCall),
                nameParam,
                shortcutParam,
                checkedParam,
                priorityParam,
                executeParam,
                validateParam).Compile();
        }

        static Action<string, string, bool, int, Action, Func<bool>> s_AddMenuItem = GetAddMenuItemFunction();

        static HashSet<string> s_RegisteredMenuItems = new HashSet<string>();

        static void OverrideRemoveComponentMenuItems()
        {
            foreach (var additionalDataComponentType in TypeCache.GetTypesWithAttribute(typeof(AdditionalComponentData)))
            {
                var componentType = additionalDataComponentType.GetCustomAttribute<AdditionalComponentData>().componentType;
                var componentTypeName = componentType.Name;

                // Register the additional data menu item
                s_AddMenuItem($"CONTEXT/{additionalDataComponentType.Name}/Remove Component",
                    string.Empty,
                    false,
                    0,
                    () => EditorUtility.DisplayDialog($"Remove {additionalDataComponentType.Name} is blocked", $"You can not delete this component, you will have to remove the {componentType}.", "OK"),
                    () => { return true; });
            }
        }

        static ContextualMenuDispatcher()
        {
            OverrideRemoveComponentMenuItems();
        }

        [MenuItem("CONTEXT/ReflectionProbe/Remove Component")]
        static void RemoveReflectionProbeComponent(MenuCommand command)
        {
            RemoveComponent<ReflectionProbe>(command);
        }

        [MenuItem("CONTEXT/Light/Remove Component")]
        static void RemoveLightComponent(MenuCommand command)
        {
            RemoveComponent<Light>(command);
        }

        [MenuItem("CONTEXT/Camera/Remove Component")]
        static void RemoveCameraComponent(MenuCommand command)
        {
            RemoveComponent<Camera>(command);
        }

        static void RemoveComponent<T>(MenuCommand command)
            where T : Component
        {
            T comp = command.context as T;

            if (!DispatchRemoveComponent<T>(comp))
            {
                //preserve built-in behavior
                if (RemoveComponentUtils.CanRemoveComponent(comp, RemoveComponentUtils.ComponentDependencies(comp)))
                    Undo.DestroyObjectImmediate(command.context);
            }
        }

        static bool DispatchRemoveComponent<T>(T component)
            where T : Component
        {
            try
            {
                var instance = new RemoveAdditionalDataContextualMenu<T>();
                instance.RemoveComponent(component, RemoveComponentUtils.ComponentDependencies(component));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Interface that should be used with [ScriptableRenderPipelineExtension(type))] attribute to dispatch ContextualMenu calls on the different SRPs
    /// </summary>
    /// <typeparam name="T">This must be a component that require AdditionalData in your SRP</typeparam>
    [Obsolete("The menu items are handled automatically for components with the AdditionalComponentData attribute", false)]
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

    internal class RemoveAdditionalDataContextualMenu<T>
        where T : Component
    {
        /// <summary>
        /// Remove the given component
        /// </summary>
        /// <param name="component">The component to remove</param>
        /// <param name="dependencies">Dependencies.</param>
        public void RemoveComponent(T component, IEnumerable<Component> dependencies)
        {
            var additionalDatas = dependencies
                .Where(c => c != component && c.GetType()
                    .GetCustomAttributes(typeof(AdditionalComponentData), true)
                    .Where(att => ((AdditionalComponentData)att).componentType == component.GetType()).Any())
                .ToList();

            if (!RemoveComponentUtils.CanRemoveComponent(component, dependencies.Where(c => !additionalDatas.Contains(c))))
                return;

            var isAssetEditing = EditorUtility.IsPersistent(component);
            try
            {
                if (isAssetEditing)
                {
                    AssetDatabase.StartAssetEditing();
                }
                Undo.SetCurrentGroupName($"Remove {typeof(T)} additional data components");

                // The components with RequireComponent(typeof(T)) also contain the AdditionalData attribute, proceed with the remove
                foreach (var additionalDataComponent in additionalDatas)
                {
                    if (additionalDataComponent != null)
                    {
                        Undo.DestroyObjectImmediate(additionalDataComponent);
                    }
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
