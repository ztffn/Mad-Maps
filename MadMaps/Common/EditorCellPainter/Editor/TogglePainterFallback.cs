#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MadMaps.Common.Painter
{
 public static class TogglePainterFallback {
 
     private const string MENU_NAME = "Tools/Mad Maps/Utilities/Force Fallback Painter";
 
     [MenuItem(TogglePainterFallback.MENU_NAME)]
     private static void ToggleAction() {
         PerformAction( !EditorCellHelper.ForceCPU );
     }

     [MenuItem(TogglePainterFallback.MENU_NAME, true)]
    static bool ValidateLogSelectedTransformName()
    {
        Menu.SetChecked(TogglePainterFallback.MENU_NAME, EditorCellHelper.ForceCPU);
        return true;
    }
 
     public static void PerformAction(bool enabled) 
     {   
         if(EditorCellHelper.ForceCPU == enabled)
         {
            return;
         }
         EditorCellHelper.ForceCPU = enabled;
         Menu.SetChecked(TogglePainterFallback.MENU_NAME, EditorCellHelper.ForceCPU);
         Debug.Log(string.Format("Set Painter Fallback override to {0}", enabled));
     }
 }
}
#endif