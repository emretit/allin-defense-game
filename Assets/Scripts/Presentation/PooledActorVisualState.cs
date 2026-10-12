using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.Presentation
{
    // Captured once after construction; pooled actors restore the original visual contract.
    internal sealed class PooledActorVisualState
    {
        private readonly Transform root;
        private readonly Transform[] transforms;
        private readonly Vector3[] positions;
        private readonly Vector3[] scales;
        private readonly Quaternion[] rotations;
        private readonly bool[] active;
        private readonly Renderer[] renderers;
        private readonly Material[][] materials;
        private readonly MaterialPropertyBlock[] blocks;
        private readonly MaterialPropertyBlock[][] materialBlocks;
        private readonly bool[] rendererEnabled;
        private readonly Canvas[] canvases;
        private readonly Collider[] colliders;
        private readonly bool[] canvasEnabled;
        private readonly bool[] colliderEnabled;
        private readonly Image[] images;
        private readonly Color[] imageColors;

        public PooledActorVisualState(GameObject host)
        {
            root = host.transform;
            transforms = host.GetComponentsInChildren<Transform>(true);
            positions = new Vector3[transforms.Length];
            scales = new Vector3[transforms.Length];
            rotations = new Quaternion[transforms.Length];
            active = new bool[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
            {
                positions[i] = transforms[i].localPosition;
                scales[i] = transforms[i].localScale;
                rotations[i] = transforms[i].localRotation;
                active[i] = transforms[i].gameObject.activeSelf;
            }
            renderers = host.GetComponentsInChildren<Renderer>(true);
            materials = new Material[renderers.Length][];
            blocks = new MaterialPropertyBlock[renderers.Length];
            materialBlocks = new MaterialPropertyBlock[renderers.Length][];
            rendererEnabled = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                materials[i] = renderers[i].sharedMaterials;
                blocks[i] = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(blocks[i]);
                materialBlocks[i] = new MaterialPropertyBlock[materials[i].Length];
                for (int slot = 0; slot < materials[i].Length; slot++)
                {
                    materialBlocks[i][slot] = new MaterialPropertyBlock();
                    renderers[i].GetPropertyBlock(materialBlocks[i][slot], slot);
                }
                rendererEnabled[i] = renderers[i].enabled;
            }
            canvases = host.GetComponentsInChildren<Canvas>(true);
            colliders = host.GetComponentsInChildren<Collider>(true);
            canvasEnabled = new bool[canvases.Length];
            colliderEnabled = new bool[colliders.Length];
            for (int i = 0; i < canvases.Length; i++) canvasEnabled[i] = canvases[i].enabled;
            for (int i = 0; i < colliders.Length; i++) colliderEnabled[i] = colliders[i].enabled;
            images = host.GetComponentsInChildren<Image>(true);
            imageColors = new Color[images.Length];
            for (int i = 0; i < images.Length; i++) imageColors[i] = images[i].color;
        }

        public void Reset()
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform child = transforms[i];
                if (child == null) continue;
                if (child != root)
                {
                    child.localPosition = positions[i];
                    child.gameObject.SetActive(active[i]);
                }
                child.localScale = scales[i];
                child.localRotation = rotations[i];
            }
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].sharedMaterials = materials[i];
                renderers[i].SetPropertyBlock(blocks[i]);
                for (int slot = 0; slot < materialBlocks[i].Length; slot++)
                    renderers[i].SetPropertyBlock(materialBlocks[i][slot].isEmpty ? null : materialBlocks[i][slot], slot);
                renderers[i].enabled = rendererEnabled[i];
            }
            for (int i = 0; i < canvases.Length; i++)
                if (canvases[i] != null) canvases[i].enabled = canvasEnabled[i];
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = colliderEnabled[i];
            for (int i = 0; i < images.Length; i++)
                if (images[i] != null) images[i].color = imageColors[i];
        }
    }
}
