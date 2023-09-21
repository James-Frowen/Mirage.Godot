using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Mirage
{
    internal static class NodeHelper
    {
        public static T GetComponent<T>(this Node node)
        {
            return node.GetParent().GetChildren().OfType<T>().FirstOrDefault();
        }
        public static bool TryGetComponent<T>(this Node node, out T result)
        {
            result = node.GetParent().GetChildren().OfType<T>().FirstOrDefault();
            return result != null;
        }

        public static T[] GetComponentsInChildren<T>(this Node node)
        {
            return GetComponentsInChildrenEnumerable<T>(node).ToArray();
        }
        public static IEnumerable<T> GetComponentsInChildrenEnumerable<T>(this Node node)
        {
            // todo can we use find_children instead?
            if (node is T comp)
                yield return comp;

            foreach (var child in node.GetChildren())
            {
                foreach (var obj in GetComponentsInChildrenEnumerable<T>(child))
                    yield return obj;
            }
        }

        public static T GetComponentInParent<T>(this Node node)
        {
            // any silbins?
            if (TryGetComponent<T>(node, out var result))
            {
                return result;
            }

            return GetComponentInParent<T>(node.GetParent());
        }

        public static NetworkIdentity TryGetNetworkIdentity<T>(this T node) where T : Node, INetworkNode
        {
            var _identity = node.GetComponentInParent<NetworkIdentity>();

            // do this 2nd check inside first if so that we are not checking == twice on unity Object
            if (_identity is null)
            {
                throw new InvalidOperationException($"Could not find NetworkIdentity for {node.Name}.");
            }
            return _identity;
        }

        internal static IEnumerable<NetworkIdentity> GetAllNetworkIdentities(this Node node)
        {
            return node.GetTree().GetNodesInGroup(nameof(NetworkIdentity)).OfType<NetworkIdentity>();
        }
    }
}

