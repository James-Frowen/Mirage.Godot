using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Mirage.Logging;

namespace Mirage
{
    public static class NodeHelper
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(NodeHelper));

        public static T GetSibling<T>(Node node) where T : class
        {
            return TryGetChild<T>(node.GetParent(), out var t) ? t : null;
        }
        public static bool TryGetSibling<T>(Node node, out T result) where T : class
        {
            return TryGetChild(node.GetParent(), out result);
        }

        /// <summary>
        /// Returns the direct child
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        public static T GetChild<T>(Node node) where T : class
        {
            return TryGetChild<T>(node, out var t) ? t : null;
        }
        /// <summary>
        /// Returns the direct child
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool TryGetChild<T>(Node node, out T result) where T : class
        {
            var children = node.GetChildren();
            foreach (var child in children)
            {
                if (child is T t)
                {
                    result = t;
                    return true;
                }
            }
            result = default;
            return false;
        }

        /// <summary>returns a depth-first list of nodes that are of type T</summary>
        public static List<T> GetAllChild<T>(Node node) where T : class
        {
            var results = new List<T>();
            GetAllChildInternal(node, results);
            return results;
        }
        /// <summary>returns a depth-first list of nodes that are of type T. List will be cleared before adding new items</summary>
        public static void GetAllChild<T>(Node node, List<T> results) where T : class
        {
            results.Clear();
            GetAllChildInternal(node, results);
        }
        private static void GetAllChildInternal<T>(Node node, List<T> results) where T : class
        {
            var children = node.GetChildren();
            foreach (var child in children)
            {
                if (child is T t)
                    results.Add(t);

                // check grandchildren 
                GetAllChildInternal(child, results);
            }
        }

        public static T GetParent<T>(Node node, bool includeSiblings) where T : class
        {
            return TryGetParent(node, includeSiblings, out T result) ? result : null;
        }

        /// <summary>
        /// tries to get a node of type T in the parent, if <paramref name="includeSiblings"/> is true it will include siblings on each parent as well
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="includeSiblings"></param>
        /// <returns></returns>
        public static bool TryGetParent<T>(Node node, bool includeSiblings, out T result) where T : class
        {
            var parent = node.GetParent();
            // not more parents, failed to find matching node
            if (parent == null)
            {
                result = null;
                return false;
            }

            // if the parent is T, then return it
            if (parent is T t)
            {
                result = t;
                return true;
            }

            // if the node had any Sibling of type T, then return it
            // note: TryGetChild(node.GetParent()) and TryGetSibling(node) are the same
            if (includeSiblings)
            {
                if (TryGetChild<T>(parent, out var t1))
                {
                    result = t1;
                    return true;
                }
            }

            // didn't find out, check next parent
            return TryGetParent(parent, includeSiblings, out result);
        }

        /// <summary>
        /// Return all <see cref="NetworkIdentity"/>  in the current level 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal static IEnumerable<NetworkIdentity> GetAllNetworkIdentities(this Node node)
        {
            return node.GetTree().GetNodesInGroup(nameof(NetworkIdentity)).OfType<NetworkIdentity>();
        }

        /// <summary>
        /// Gets NetworkIdentity in first level of child
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        public static NetworkIdentity GetNetworkIdentity(Node node, bool includeChild)
        {
            return GetNetworkIdentityInternal(node, includeChild);
        }

        /// <summary>
        /// Gets NetworkIdentity in first level of child
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        public static NetworkIdentity GetNetworkIdentity<T>(this T node)
            // extension method will show up if the class is INetworkNode
            where T : Node, INetworkNode
        {
            return GetNetworkIdentityInternal(node, includeChild: false);
        }

        private static NetworkIdentity GetNetworkIdentityInternal(Node node, bool includeChild)
        {
            NetworkIdentity identity;
            var found = TryGetParent<NetworkIdentity>(node, includeSiblings: true, out identity)
                || (includeChild && TryGetChild(node, out identity));

            if (logger.LogEnabled())
            {
                if (found)
                    logger.Log($"Found {identity}");
                else
                    logger.Log($"Failed to find NetworkIdentity");
            }
            return identity;
        }


        public static INetworkNode[] FindNetworkBehaviours(NetworkIdentity identity)
        {
            // we write component index as byte
            // check if components are in byte.MaxRange just to be 100% sure that we avoid overflows
            return FindNetworkBehaviours(identity.Root, byte.MaxValue);
        }

        /// <summary>
        /// Finds all NetworkBehaviours in child nodes, and then removes any that belong to another NetworkIdentity from the components array
        /// <para>
        ///     If there are nested NetworkIdentities then Behaviour that belong to those Identities will be found by GetComponentsInChildren if the child object is added
        ///     before the Array is intialized. This method will check each Behaviour to make sure that the Identity is the same as the current Identity, and if it is not
        ///     remove it from the array.
        /// </para>
        /// </summary>
        /// <param name="components"></param>
        public static INetworkNode[] FindNetworkBehaviours(Node searchRoot, int maxAllowed)
        {
            var found = FindNetworkBehavioursInternal<INetworkNode>(searchRoot, maxAllowed);
            return found.ToArray();
        }

        /// <summary>
        /// Finds all NetworkBehaviours in child nodes, and then removes any that belong to another NetworkIdentity from the components array
        /// <para>
        ///     If there are nested NetworkIdentities then Behaviour that belong to those Identities will be found by GetComponentsInChildren if the child object is added
        ///     before the Array is intialized. This method will check each Behaviour to make sure that the Identity is the same as the current Identity, and if it is not
        ///     remove it from the array.
        /// </para>
        /// </summary>
        /// <param name="components"></param>
        public static List<T> FindNetworkBehavioursInternal<T>(Node searchRoot, int maxAllowed) where T : class
        {
            var found = GetAllChild<T>(searchRoot);

            // start at last so we can remove from end of array instead of start
            for (var i = found.Count - 1; i >= 0; i--)
            {
                var item = found[i];
                // get the identity that the node will be using itself
                var identity = GetNetworkIdentityInternal((Node)(object)item, false);
                if (identity == null)
                {
                    logger.LogError($"Child node {item} found not find its NetworkIdentity");
                    found.RemoveAt(i);
                }
                else if (identity.Root != searchRoot)
                {
                    if (logger.LogEnabled()) logger.Log($"Child node {item} had different NetworkIdentity. SearchRoot:{searchRoot},  Child field:{identity}");
                    found.RemoveAt(i);
                }
            }

            if (found.Count > maxAllowed)
                throw new InvalidOperationException("Only 255 NetworkBehaviours are allowed per GameObject.");

            if (logger.LogEnabled())
            {
                var list = found.Select(x => $"- {x}");
                logger.Log($"Found {found.Count} children: {string.Join("\n", list)}");
            }

            return found;
        }
    }
}

