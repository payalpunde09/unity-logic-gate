using UnityEngine;

namespace DigitalLogicLab.Input
{
    public interface IInputProvider
    {
        bool GetSelectDown();
        Vector2 GetPointerPosition();
        Ray GetPointerRay(Camera cam);
        int GetNumericShortcut();
        bool GetMenuRequested();
    }
}
