namespace P3k.UIPointerProjector.Editor
{
   using System;
   using System.Linq;

   using UnityEditor;

   using UnityEngine;

   internal sealed class UIProjectorRTPickerWindow : EditorWindow
   {
      private Action _onCancel;

      private Action<RenderTexture> _onOk;

      private RenderTexture[] _renderTextures = Array.Empty<RenderTexture>();

      private Vector2 _scroll;

      private int _selectedIndex = -1;

      private void OnGUI()
      {
         EditorGUILayout.LabelField("RenderTextures in project", EditorStyles.boldLabel);
         EditorGUILayout.Space();

         using (new EditorGUILayout.HorizontalScope())
         {
            if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
            {
               Refresh();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"Found: {_renderTextures.Length}", GUILayout.Width(90f));
         }

         EditorGUILayout.Space();

         if (_renderTextures.Length == 0)
         {
            EditorGUILayout.HelpBox("No RenderTextures were found in the project.", MessageType.Info);
            DrawButtons(canOk: false);
            return;
         }

         _scroll = EditorGUILayout.BeginScrollView(_scroll);
         {
            for (var i = 0; i < _renderTextures.Length; i++)
            {
               var rt = _renderTextures[i];
               if (rt == null)
               {
                  continue;
               }

               var assetPath = AssetDatabase.GetAssetPath(rt);

               using (new EditorGUILayout.HorizontalScope())
               {
                  var isSelected = i == _selectedIndex;

                  // Toggle-style row with path sublabel
                  if (GUILayout.Toggle(isSelected, rt.name, "Button"))
                  {
                     _selectedIndex = i;
                  }

                  if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                  {
                     EditorGUIUtility.PingObject(rt);
                     Selection.activeObject = rt;
                  }
               }

               EditorGUILayout.LabelField(assetPath, EditorStyles.miniLabel);
               EditorGUILayout.Space(2);
            }
         }
         EditorGUILayout.EndScrollView();

         DrawButtons(canOk: _selectedIndex >= 0 && _selectedIndex < _renderTextures.Length);
      }

      internal static void Open(Action<RenderTexture> onOk, Action onCancel)
      {
         var window = GetWindow<UIProjectorRTPickerWindow>(true, "Select RenderTexture", true);
         window.minSize = new Vector2(420f, 320f);
         window._onOk = onOk;
         window._onCancel = onCancel;
         window.Refresh();
         window.ShowUtility();
      }

      private void DrawButtons(bool canOk)
      {
         EditorGUILayout.Space();

         using (new EditorGUILayout.HorizontalScope())
         {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(90f)))
            {
               _onCancel?.Invoke();
               Close();
            }

            using (new EditorGUI.DisabledScope(!canOk))
            {
               if (GUILayout.Button("OK", GUILayout.Width(90f)))
               {
                  var rt = _selectedIndex >= 0 && _selectedIndex < _renderTextures.Length ?
                              _renderTextures[_selectedIndex] :
                              null;
                  _onOk?.Invoke(rt);
                  Close();
               }
            }
         }
      }

      private void Refresh()
      {
         _renderTextures = AssetDatabase.FindAssets("t:RenderTexture").Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<RenderTexture>).Where(rt => rt != null).OrderBy(rt => rt.name)
            .ToArray();

         if (_renderTextures.Length == 0)
         {
            _selectedIndex = -1;
            return;
         }

         // Keep selection valid if list changed
         if (_selectedIndex < 0 || _selectedIndex >= _renderTextures.Length)
         {
            _selectedIndex = 0;
         }
      }
   }
}
