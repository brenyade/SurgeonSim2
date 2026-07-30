using UnityEngine;

namespace TraumaSurgeon.Visuals
{
    /// <summary>
    /// Helper for assembling placeholder models from Unity primitives.
    /// Every visual in the game is built through this so real art can be swapped in later by
    /// replacing the builder call sites with prefab instantiation.
    /// </summary>
    public static class PrimitiveFactory
    {
        public static GameObject Create(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.transform.localRotation = Quaternion.identity;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            if (!collider)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null)
                {
                    Object.Destroy(c);
                }
            }

            return go;
        }

        public static GameObject Create(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material,
            bool collider = true)
        {
            GameObject go = Create(type, name, parent, localPosition, localScale, material, collider);
            go.transform.localRotation = localRotation;
            return go;
        }

        public static GameObject Empty(string name, Transform parent, Vector3 localPosition = default)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            go.transform.localPosition = localPosition;
            return go;
        }

        /// <summary>Creates a thin box that acts as a floor/wall panel.</summary>
        public static GameObject Panel(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            return Create(PrimitiveType.Cube, name, parent, position, size, material);
        }

        /// <summary>Destroys all colliders in a hierarchy - used for decorative props.</summary>
        public static void StripColliders(GameObject root)
        {
            foreach (Collider c in root.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(c);
            }
        }

        /// <summary>Marks a hierarchy as non-shadow-casting for cheap decorative geometry.</summary>
        public static void DisableShadows(GameObject root)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }
    }
}
