namespace P3k.UIPointerProjector.Editor
{
   using P3k.UIPointerProjector.Adapters.Components;

   using System.Linq;

   using UnityEditor;

   using UnityEngine;

   /// <summary>
   ///    Editor utility for creating a UIPointerProjector quad and assigning a material.
   /// </summary>
   public static class UIProjectorScreenEditor
   {
      [MenuItem("GameObject/UIProjectorScreen", false, 10)]
      private static void CreateUIPointerProjector(MenuCommand menuCommand)
      {
         var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
         quad.name = "UIProjectorScreen";
         quad.transform.localScale = new Vector3(7f, 4f, 1f);

         Undo.RegisterCreatedObjectUndo(quad, "Create UIProjectorScreen");

         // Replace the primitive's default MeshCollider with a BoxCollider.
         if (quad.TryGetComponent(out MeshCollider meshCollider))
         {
            Undo.DestroyObjectImmediate(meshCollider);
         }

         var boxCollider = quad.GetComponent<BoxCollider>();
         if (!boxCollider)
         {
            boxCollider = Undo.AddComponent<BoxCollider>(quad);
         }

         boxCollider.isTrigger = true;

         // Quad is in the XY plane, so we give it a small thickness on Z.
         var size = boxCollider.size;
         size.z = 0.05f;
         boxCollider.size = size;

         quad.AddComponent<ProjectorPointer>();

         Selection.activeGameObject = quad;
      }
   }
}
