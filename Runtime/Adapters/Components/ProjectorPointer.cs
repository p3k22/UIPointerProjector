namespace P3k.UIPointerProjector.Adapters.Components
{
   using P3k.UIPointerProjector.Abstractions.Enums;
   using P3k.UIPointerProjector.Abstractions.Interfaces;

   using System;
   using System.Collections;
   using System.Collections.Generic;
   using System.Linq;

   using UnityEngine;
   using UnityEngine.EventSystems;
   using UnityEngine.InputSystem;
   using UnityEngine.InputSystem.UI;
   using UnityEngine.UI;

   [RequireComponent(typeof(BoxCollider))]
   public sealed class ProjectorPointer : MonoBehaviour, IProjectorPointer
   {
      private bool _lastInputSystemState;

      private BoxCollider _collider;

      private Camera _pointerCamera;

      private Camera _uiCamera;

      private Canvas _canvas;

      private CanvasGroup _canvasGroup;

      private EventSystem _eventSystem;

      private GameObject _pressed;

      private GraphicRaycaster _rayCaster;

      private InputSystemUIInputModule _inputSystem;

      private LayerMask _pointerLayer;

      private Vector2 _lastPointerPosition;

      public GameObject CurrentHovered { get; private set; }

      public bool IsActive { get; private set; }

      public event Action<GameObject> PointerHoverEnter;

      public event Action PointerHoverExit;

      public event Action PointerPress;

      private void Awake()
      {
         _collider = GetComponent<BoxCollider>();
         _collider.isTrigger = true;

         if (!_renderProjectorParent)
         {
            Debug.LogWarning($"{nameof(_renderProjectorParent)} is not assigned.");
            enabled = false;
            return;
         }

         _canvas = _renderProjectorParent.GetComponentInChildren<Canvas>();
         _uiCamera = _renderProjectorParent.GetComponentInChildren<Camera>();

         if (_canvas)
         {
            _canvasGroup = _canvas.GetComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
         }

         _pointerLayer = 1 << gameObject.layer;

         EnsureEventSystem();
      }

      private IEnumerator Start()
      {
         yield return null;
         yield return null;
         _uiCamera.gameObject.SetActive(false);
         _canvasGroup.alpha = 0;
      }

      private void Update()
      {
         // Recover from scene changes / domain reloads.
         EnsureEventSystem();

         if (!IsActive)
         {
            return;
         }

         if (!_pointerCamera || !_uiCamera || !_canvas)
         {
            ClearHover();
            return;
         }

         if (!_eventSystem || !_rayCaster)
         {
            ClearHover();
            return;
         }

         switch (_pointerMode)
         {
            case PointerMode.CameraForward:
               UpdateCameraForwardPointer();
               break;

            case PointerMode.MouseScreen:
               UpdateMouseScreenPointer();
               break;
         }
      }

      public void Activate(PointerMode mode, Camera pointerCamera, bool useMainCamera = false)
      {
         IsActive = true;
         _pointerMode = mode;
         _pointerCamera = pointerCamera;
         if (useMainCamera)
         {
            DebugAssignPointerCamera();
         }

         if (_inputSystem)
         {
            _lastInputSystemState = _inputSystem.enabled;
            _inputSystem.enabled = false;
         }

         _canvasGroup.blocksRaycasts = true;
         _uiCamera.gameObject.SetActive(true);
         _canvasGroup.alpha = 1;
      }

      public void Deactivate()
      {
         IsActive = false;
         _canvasGroup.blocksRaycasts = false;
         _uiCamera.gameObject.SetActive(false);
         _canvasGroup.alpha = 0;
         if (_inputSystem)
         {
            _inputSystem.enabled = _lastInputSystemState;
         }

         ClearHover();
      }

      private void ClearHover()
      {
         _pressed = null;

         if (!CurrentHovered)
         {
            return;
         }

         if (!_eventSystem)
         {
            CurrentHovered = null;
            return;
         }

         var pointer = new PointerEventData(_eventSystem);
         ExecuteEvents.Execute(CurrentHovered, pointer, ExecuteEvents.pointerExitHandler);
         CurrentHovered = null;

         _eventSystem.SetSelectedGameObject(null);
      }

      [ContextMenu("Debug/Activate Pointer")]
      private void DebugActivatePointer()
      {
         Activate(_pointerMode, Camera.main);
      }

      [ContextMenu("Debug/Assign Pointer Camera")]
      private void DebugAssignPointerCamera()
      {
         _pointerCamera = Camera.main;
      }

      [ContextMenu("Debug/Deactivate Pointer")]
      private void DebugDeactivatePointer()
      {
         Deactivate();
      }

      private void EnsureEventSystem()
      {
         // Only cache references; avoid doing work if we already have everything.
         if (_eventSystem && _rayCaster)
         {
            return;
         }

         // EventSystem: reuse an existing one if present, otherwise create a minimal one.
         if (!_eventSystem)
         {
            _eventSystem = FindFirstObjectByType<EventSystem>();
            if (!_eventSystem)
            {
               var esGo = new GameObject("EventSystem");
               _eventSystem = esGo.AddComponent<EventSystem>();
            }
         }

         // GraphicRaycaster: must be on the Canvas we are targeting.
         if (!_rayCaster)
         {
            if (_canvas)
            {
               if (!_canvas.TryGetComponent(out _rayCaster))
               {
                  _rayCaster = _canvas.gameObject.AddComponent<GraphicRaycaster>();
               }
            }
            else
            {
               _rayCaster = null;
            }
         }

         if (!_inputSystem)
         {
            _inputSystem = FindFirstObjectByType<InputSystemUIInputModule>();
         }
      }

      private Vector2 GetUvFromHit(RaycastHit hit)
      {
         // Convert hit point to the collider local space.
         var local = transform.InverseTransformPoint(hit.point);

         // BoxCollider is axis-aligned in local space.
         var size = _collider.size;
         var center = _collider.center;

         // Projector quad is in XY plane. Map X,Y to 0..1.
         var minX = center.x - (size.x * 0.5f);
         var minY = center.y - (size.y * 0.5f);

         // If collider is degenerate, fall back.
         if (size.x <= 0.0001f || size.y <= 0.0001f)
         {
            return hit.textureCoord;
         }

         var u = (local.x - minX) / size.x;
         var v = (local.y - minY) / size.y;

         return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
      }

      private void HandleHover(GameObject current, PointerEventData pointer)
      {
         if (current == CurrentHovered)
         {
            return;
         }

         if (CurrentHovered)
         {
            ExecuteEvents.Execute(CurrentHovered, pointer, ExecuteEvents.pointerExitHandler);
            PointerHoverExit?.Invoke();
         }

         if (current)
         {
            ExecuteEvents.Execute(current, pointer, ExecuteEvents.pointerEnterHandler);
            PointerHoverEnter?.Invoke(current);
         }

         CurrentHovered = current;
      }

      private void HandlePress(GameObject pressCandidate, GameObject currentHover, PointerEventData pointer)
      {
         var mouse = Mouse.current;
         if (mouse == null)
         {
            _pressed = null;
            return;
         }

         if (mouse.leftButton.wasPressedThisFrame)
         {
            _pressed = pressCandidate;
            pointer.pressPosition = pointer.position;
            pointer.pointerPressRaycast = pointer.pointerCurrentRaycast;

            if (_pressed)
            {
               _eventSystem.SetSelectedGameObject(_pressed);
               ExecuteEvents.Execute(_pressed, pointer, ExecuteEvents.pointerDownHandler);
               ExecuteEvents.Execute(_pressed, pointer, ExecuteEvents.initializePotentialDrag);
            }
            else
            {
               _eventSystem.SetSelectedGameObject(null);
            }

            return;
         }

         if (!mouse.leftButton.wasReleasedThisFrame)
         {
            return;
         }

         if (_pressed)
         {
            ExecuteEvents.Execute(_pressed, pointer, ExecuteEvents.pointerUpHandler);

            var clickHandler = currentHover ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentHover) : null;
            if (clickHandler && clickHandler == _pressed)
            {
               ExecuteEvents.Execute(_pressed, pointer, ExecuteEvents.pointerClickHandler);
               PointerPress?.Invoke();
            }
         }

         _pressed = null;
      }

      private void ProcessRay(Ray ray)
      {
         if (!Physics.Raycast(ray, out var hit, _maxReach, _pointerLayer, QueryTriggerInteraction.Collide))
         {
            ClearHover();
            return;
         }

         if (hit.collider != _collider)
         {
            ClearHover();
            return;
         }

         // NOTE: RaycastHit.textureCoord is not reliable for BoxCollider hits.
         // Compute UV from the hit point in local space.
         var uv = GetUvFromHit(hit);

         // Conditionally correct mirroring based on surface vs UI camera orientation.
         if (ShouldFlipUvX())
         {
            uv.x = 1f - uv.x;
         }

         if (ShouldFlipUvY())
         {
            uv.y = 1f - uv.y;
         }

         var rt = _uiCamera.targetTexture;
         float width;
         float height;
         var x0 = 0f;
         var y0 = 0f;

         if (rt)
         {
            width = rt.width;
            height = rt.height;
         }
         else
         {
            var pr = _uiCamera.pixelRect;
            x0 = pr.x;
            y0 = pr.y;
            width = pr.width;
            height = pr.height;
         }

         var uiPos = new Vector2(x0 + (uv.x * width), y0 + (uv.y * height));

         var pointer = new PointerEventData(_eventSystem)
                          {
                             position = uiPos,
                             delta = uiPos - _lastPointerPosition,
                             button = PointerEventData.InputButton.Left,
                             clickCount = 1
                          };

         _lastPointerPosition = uiPos;

         var results = new List<RaycastResult>();
         _rayCaster.Raycast(pointer, results);

         RaycastResult topResult = default;
         GameObject topGo = null;

         if (results.Count > 0)
         {
            topResult = results[0];
            topGo = topResult.gameObject;
            pointer.pointerCurrentRaycast = topResult;
         }

         var current = topGo ? ExecuteEvents.GetEventHandler<IPointerEnterHandler>(topGo) : null;
         var pressCandidate = topGo ? ExecuteEvents.GetEventHandler<IPointerDownHandler>(topGo) : null;

         pointer.pointerEnter = current;
         pointer.hovered.Clear();
         if (current)
         {
            pointer.hovered.Add(current);
         }

         HandleHover(current, pointer);
         HandlePress(pressCandidate, current, pointer);
      }

      private bool ShouldFlipUvX()
      {
         // If the surface "right" opposes UI camera "right", horizontal mapping is mirrored.
         return Vector3.Dot(transform.right, _uiCamera.transform.right) < 0f;
      }

      private bool ShouldFlipUvY()
      {
         // If the surface "up" opposes UI camera "up", vertical mapping is mirrored.
         return Vector3.Dot(transform.up, _uiCamera.transform.up) < 0f;
      }

      private void UpdateCameraForwardPointer()
      {
         var origin = _pointerCamera.transform.position;
         var dir = _pointerCamera.transform.forward;

         var ray = new Ray(origin, dir);

         if (_debugDrawRay)
         {
            Debug.DrawRay(ray.origin, ray.direction * _maxReach, Color.cyan);
         }

         ProcessRay(ray);
      }

      private void UpdateMouseScreenPointer()
      {
         var mouse = Mouse.current;
         if (mouse == null)
         {
            ClearHover();
            return;
         }

         var screenCamera = _pointerCamera ? _pointerCamera : Camera.main;
         if (!screenCamera)
         {
            ClearHover();
            return;
         }

         var ray = screenCamera.ScreenPointToRay(mouse.position.ReadValue());

         if (_debugDrawRay)
         {
            Debug.DrawRay(ray.origin, ray.direction * _maxReach, Color.magenta);
         }

         ProcessRay(ray);
      }

   #region Serialized Fields

      [SerializeField]
      private PointerMode _pointerMode;

      [SerializeField]
      private Transform _renderProjectorParent;

      [SerializeField]
      private float _maxReach = 100f;

      [Header("Debug")]
      [SerializeField]
      private bool _debugDrawRay;

   #endregion
   }
}
