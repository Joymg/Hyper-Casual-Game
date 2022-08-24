using UnityEngine;
using UnityEditor;

public static class MouseUtility
{
    public static Vector3 GetMouseWorldPosition(float depthFor3DSpace = 10){
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Vector3 worldMouse = Physics.Raycast(mouseRay, out RaycastHit hitInfo,depthFor3DSpace *2) ? 
            hitInfo.point : mouseRay.GetPoint(depthFor3DSpace);

        return worldMouse;
    }

}
