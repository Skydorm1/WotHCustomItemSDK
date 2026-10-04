using UnityEngine;

namespace WotHCustomItemSDK
{
    internal sealed class CustomItemUnlockWindow
    {
        private bool showWindow;
        private string itemIdInput = string.Empty;
        private Rect windowRect = new Rect(20f, 20f, 400f, 180f);

        internal void Tick()
        {
            if (Input.GetKeyDown(KeyCode.F2))
                showWindow = !showWindow;
        }

        internal void Draw()
        {
            if (!showWindow)
                return;

            windowRect = GUI.Window(92831, windowRect, DrawWindow, "WotH Custom Item SDK");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.Label("Enter the Item ID you want to unlock:");
            GUILayout.Space(5);
            itemIdInput = GUILayout.TextField(itemIdInput, GUILayout.Height(25));
            GUILayout.Space(10);

            if (GUILayout.Button("Unlock Item", GUILayout.Height(30)))
                CustomItemService.UnlockItem(itemIdInput);

            GUILayout.Space(10);
            GUILayout.Label("Example: 100000");
            GUILayout.Space(5);
            GUILayout.Label("Press F2 to close this window.");
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
