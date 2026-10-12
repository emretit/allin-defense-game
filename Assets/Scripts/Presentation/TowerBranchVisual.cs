using AllinDefense.Core;
using AllinDefense.Data;
using UnityEngine;

namespace AllinDefense.Presentation
{
    public static class TowerBranchVisual
    {
        private static Material gold, ice;
        public static void Apply(GreyboxOutpost tower)
        {
            if (tower.Specialization == OutpostSpecialization.None) return;
            var old = tower.transform.Find("Specialization crest");
            if (old != null) Object.Destroy(old.gameObject);
            var root = new GameObject("Specialization crest").transform;
            root.SetParent(tower.transform,false); root.localPosition = Vector3.up*2.5f;
            gold ??= CreateMaterial(new Color(1,.7f,.12f));
            ice ??= CreateMaterial(new Color(.3f,.9f,1));
            switch(tower.Specialization)
            {
                case OutpostSpecialization.RapidFire:
                    Part(root,PrimitiveType.Cube,new(-.18f,0,0),new(.1f,.65f,.1f));
                    Part(root,PrimitiveType.Cube,new(.18f,0,0),new(.1f,.65f,.1f)); break;
                case OutpostSpecialization.Sharpshooter:
                    Part(root,PrimitiveType.Cylinder,Vector3.zero,new(.12f,.6f,.12f)); break;
                case OutpostSpecialization.ShieldGuard:
                    Part(root,PrimitiveType.Cube,new(-.2f,0,0),new(.3f,.5f,.12f));
                    Part(root,PrimitiveType.Cube,new(.2f,0,0),new(.3f,.5f,.12f)); break;
                case OutpostSpecialization.AssaultGuard:
                    Part(root,PrimitiveType.Cube,Vector3.zero,new(.12f,.8f,.12f)).localRotation=Quaternion.Euler(0,0,35);
                    Part(root,PrimitiveType.Cube,Vector3.zero,new(.12f,.8f,.12f)).localRotation=Quaternion.Euler(0,0,-35); break;
                case OutpostSpecialization.Blast:
                    Part(root,PrimitiveType.Sphere,Vector3.zero,Vector3.one*.6f); break;
                case OutpostSpecialization.Frost:
                    Part(root,PrimitiveType.Cube,Vector3.zero,Vector3.one*.4f,true).localRotation=Quaternion.Euler(0,0,45); break;
            }
        }
        private static Material CreateMaterial(Color color) => new(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default")) { color=color };
        private static Transform Part(Transform parent,PrimitiveType shape,Vector3 at,Vector3 size,bool frost=false)
        {
            var go=GameObject.CreatePrimitive(shape); Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent,false); go.transform.localPosition=at; go.transform.localScale=size;
            var renderer=go.GetComponent<Renderer>(); renderer.sharedMaterial=frost?ice:gold;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }
    }
}
