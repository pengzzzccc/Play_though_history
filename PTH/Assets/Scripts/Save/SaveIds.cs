using System.Collections.Generic;
using UnityEngine;

namespace UnknownTechnology
{
    public static class SaveIds
    {
        public static string FromHierarchy(Transform target)
        {
            var segments = new List<string>();
            for (var current = target; current != null; current = current.parent)
            {
                segments.Add(Segment(current));
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        private static string Segment(Transform target)
        {
            var name = target.name;
            var index = 0;
            var count = 0;
            if (target.parent != null)
            {
                foreach (Transform sibling in target.parent)
                {
                    Count(sibling, target, name, ref index, ref count);
                }
            }
            else if (target.gameObject.scene.IsValid())
            {
                foreach (var root in target.gameObject.scene.GetRootGameObjects())
                {
                    Count(root.transform, target, name, ref index, ref count);
                }
            }

            return count > 1 ? name + "#" + index : name;
        }

        private static void Count(Transform sibling, Transform target, string name, ref int index, ref int count)
        {
            if (sibling.name != name)
            {
                return;
            }

            if (sibling == target)
            {
                index = count;
            }

            count++;
        }
    }
}
