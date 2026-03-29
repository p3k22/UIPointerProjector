namespace P3k.UIPointerProjector.Editor
{
   using System.Linq;

   using UnityEditor;

   using UnityEngine;
   using UnityEngine.EventSystems;
   using UnityEngine.InputSystem.UI;
   using UnityEngine.UI;

   public static class UIProjectorEditor
   {
      [MenuItem("GameObject/UIProjector", false, 10)]
      private static void CreateUIRenderProjector(MenuCommand menuCommand)
      {
         var root = new GameObject("UIProjector");
         Undo.RegisterCreatedObjectUndo(root, "Create UIProjector");

         // UI Camera
         var cameraGO = new GameObject("ProjectorCamera");
         Undo.RegisterCreatedObjectUndo(cameraGO, "Create ProjectorCamera");
         cameraGO.transform.SetParent(root.transform, false);

         var camera = cameraGO.AddComponent<Camera>();
         camera.clearFlags = CameraClearFlags.SolidColor;
         camera.backgroundColor = Color.clear;
         camera.farClipPlane = 100f;
         camera.orthographic = true;

         // Canvas
         var canvasGO = new GameObject("ProjectorCanvas");
         Undo.RegisterCreatedObjectUndo(canvasGO, "Create ProjectorCanvas");
         canvasGO.transform.SetParent(root.transform, false);

         var canvas = canvasGO.AddComponent<Canvas>();
         canvas.renderMode = RenderMode.ScreenSpaceCamera;
         canvas.worldCamera = camera;

         canvasGO.AddComponent<CanvasGroup>();
         canvasGO.AddComponent<GraphicRaycaster>();

         var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
         canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
         canvasScaler.referenceResolution = new Vector2(800, 600);
         canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

         EnsureEventSystemWithInputSystemUiModuleExists();

         GameObjectUtility.SetParentAndAlign(root, menuCommand.context as GameObject);
         Selection.activeGameObject = root;

         // RenderTexture options
         var choice = EditorUtility.DisplayDialogComplex(
         "RenderTexture",
         "Add a RenderTexture to new UICamera",
         "Create + Add",
         "None",
         "Add Existing");

         // 0 = ok, 1 = cancel, 2 = alt
         if (choice == 0)
         {
            var renderTexture = RenderTextureAssetCreator.CreateRenderTextureAsset(
            "Create UIPointerProjector RenderTexture",
            "UIPointerProjector_RT",
            "Assets");

            if (renderTexture != null)
            {
               var material = RenderTextureAssetCreator.CreateUnlitMaterialAssetFor(renderTexture);

               Undo.RecordObject(camera, "Assign RenderTexture");
               camera.targetTexture = renderTexture;
               EditorUtility.SetDirty(camera);

               // If there's at least one Graphic on the canvas, assign its material as a convenience.
               if (material != null)
               {
                  var graphic = canvasGO.GetComponentInChildren<Graphic>();
                  if (graphic != null)
                  {
                     Undo.RecordObject(graphic, "Assign UIPointerProjector Material");
                     graphic.material = material;
                     EditorUtility.SetDirty(graphic);
                  }

                  Selection.activeObject = material;
               }
               else
               {
                  Selection.activeObject = renderTexture;
               }
            }
         }
         else if (choice == 2)
         {
            UIProjectorRTPickerWindow.Open(
            rt =>
               {
                  if (!camera)
                  {
                     return;
                  }

                  Undo.RecordObject(camera, "Assign RenderTexture");
                  camera.targetTexture = rt;
                  EditorUtility.SetDirty(camera);
               },
            null);
         }
      }

      private static void EnsureEventSystemWithInputSystemUiModuleExists()
      {
      #if ENABLE_INPUT_SYSTEM
         var existingEventSystem = Object.FindFirstObjectByType<EventSystem>();
         if (existingEventSystem == null)
         {
            var eventSystemGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem");
            existingEventSystem = eventSystemGO.AddComponent<EventSystem>();
         }

         var inputModule = existingEventSystem.GetComponent<InputSystemUIInputModule>();
         if (inputModule == null)
         {
            Undo.AddComponent<InputSystemUIInputModule>(existingEventSystem.gameObject);
         }
      #endif
      }
   }
}
