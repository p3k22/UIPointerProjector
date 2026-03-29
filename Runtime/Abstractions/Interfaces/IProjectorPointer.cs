
namespace P3k.UIPointerProjector.Abstractions.Interfaces
{
   using P3k.UIPointerProjector.Abstractions.Enums;

   using System;
   using System.Linq;

   using UnityEngine;

   public interface IProjectorPointer
   {
      bool IsActive { get; }

      void Activate(PointerMode mode, Camera pointerCamera, bool useMainCamera = false);

      void Deactivate();

      GameObject CurrentHovered { get; }

      event Action<GameObject> PointerHoverEnter;

      event Action PointerHoverExit;

      event Action PointerPress;
   }
}
