namespace P3k.UIPointerProjector.Editor
{
   using System.IO;
   using System.Linq;

   using UnityEditor;

   using UnityEngine;
   using UnityEngine.Rendering;

   public static class RenderTextureAssetCreator
   {
      internal static RenderTexture CreateRenderTextureAsset(string title, string defaultName, string selectedFolder)
      {
         var rtPath = EditorUtility.SaveFilePanelInProject(
         title,
         defaultName,
         "renderTexture",
         "Select location for the RenderTexture",
         selectedFolder);

         if (string.IsNullOrEmpty(rtPath))
         {
            return null;
         }

         var desc = new RenderTextureDescriptor(800, 600)
                       {
                          graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                          depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat,
                          msaaSamples = 1
                       };

         var renderTexture = new RenderTexture(desc) {name = defaultName};

         AssetDatabase.CreateAsset(renderTexture, rtPath);
         AssetDatabase.SaveAssets();
         AssetDatabase.Refresh();

         return renderTexture;
      }

      internal static Material CreateUnlitMaterialAssetFor(RenderTexture renderTexture)
      {
         if (renderTexture == null)
         {
            return null;
         }

         // Determine render pipeline
         var isHDRP = GraphicsSettings.currentRenderPipeline != null
                      && GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline");

         Shader shader;

         if (isHDRP)
         {
            shader = Shader.Find("HDRP/Unlit");
         }
         else
         {
            shader = Shader.Find("Unlit/Texture");
         }

         if (shader == null)
         {
            Debug.LogError("Failed to find an appropriate Unlit shader.");
            return null;
         }

         var material = new Material(shader) {name = "UIPointerProjector_Unlit"};

         if (isHDRP)
         {
            material.SetTexture("_BaseColorMap", renderTexture);
         }
         else
         {
            material.mainTexture = renderTexture;
         }

         var rtPath = AssetDatabase.GetAssetPath(renderTexture);
         var matPath = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(rtPath, ".mat"));

         AssetDatabase.CreateAsset(material, matPath);
         AssetDatabase.SaveAssets();
         AssetDatabase.Refresh();

         return material;
      }

      [MenuItem("Assets/Create/UIPointerProjector/RenderTexture + Unlit Material", false, 11)]
      private static void CreateRenderTextureAndMaterial()
      {
         var selectedFolder = GetSelectedProjectFolderPath();

         var renderTexture = CreateRenderTextureAsset(
         title: "Create UIPointerProjector RenderTexture",
         defaultName: "UIPointerProjector_RT",
         selectedFolder: selectedFolder);

         if (renderTexture == null)
         {
            return;
         }

         var material = CreateUnlitMaterialAssetFor(renderTexture);

         if (material == null)
         {
            return;
         }

         Selection.activeObject = material;
      }

      [MenuItem("Assets/Create/UIPointerProjector/RenderTexture + Unlit Material", true)]
      private static bool CreateRenderTextureAndMaterial_Validate()
      {
         // Enable only when Project window selection is a folder (or nothing selected).
         var folder = GetSelectedProjectFolderPath();
         return !string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder);
      }

      private static string GetSelectedProjectFolderPath()
      {
         var obj = Selection.GetFiltered(typeof(Object), SelectionMode.Assets).FirstOrDefault();

         if (obj == null)
         {
            return "Assets";
         }

         var path = AssetDatabase.GetAssetPath(obj);

         if (string.IsNullOrEmpty(path))
         {
            return "Assets";
         }

         if (AssetDatabase.IsValidFolder(path))
         {
            return path;
         }

         var dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
         return string.IsNullOrEmpty(dir) ? "Assets" : dir;
      }
   }
}
