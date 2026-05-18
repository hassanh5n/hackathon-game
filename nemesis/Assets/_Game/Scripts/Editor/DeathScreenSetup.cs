#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Nemesis.Editor
{
    public class DeathScreenSetup : EditorWindow
    {
        [MenuItem("Nemesis/Setup/2. Build Death Screen UI")]
        public static void BuildDeathScreen()
        {
            Scene scene = EditorSceneManager.GetActiveScene();

            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create Death Panel
            GameObject deathPanel = new GameObject("DeathPanel");
            deathPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = deathPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            Image panelImage = deathPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.85f); // Dark translucent background

            // YOU FELL Text
            GameObject titleTextObj = new GameObject("TitleText");
            titleTextObj.transform.SetParent(deathPanel.transform, false);
            TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "YOU FELL";
            titleText.fontSize = 80;
            titleText.color = Color.red;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
            RectTransform titleRect = titleTextObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.8f);
            titleRect.anchorMax = new Vector2(0.5f, 0.8f);
            titleRect.sizeDelta = new Vector2(800, 150);
            titleRect.anchoredPosition = Vector2.zero;

            // Taunt Text
            GameObject tauntTextObj = new GameObject("TauntText");
            tauntTextObj.transform.SetParent(deathPanel.transform, false);
            TextMeshProUGUI tauntText = tauntTextObj.AddComponent<TextMeshProUGUI>();
            tauntText.text = "The forest is analyzing your failure...";
            tauntText.fontSize = 40;
            tauntText.color = Color.white;
            tauntText.alignment = TextAlignmentOptions.Center;
            tauntText.textWrappingMode = TextWrappingModes.Normal;
            RectTransform tauntRect = tauntTextObj.GetComponent<RectTransform>();
            tauntRect.anchorMin = new Vector2(0.5f, 0.5f);
            tauntRect.anchorMax = new Vector2(0.5f, 0.5f);
            tauntRect.sizeDelta = new Vector2(800, 200);
            tauntRect.anchoredPosition = Vector2.zero;

            // Buttons
            Button riseButton = CreateButton("RiseAgainButton", "RISE AGAIN", deathPanel.transform, new Vector2(0.5f, 0.3f));
            Button sanctumButton = CreateButton("SanctumButton", "RETURN TO SANCTUM", deathPanel.transform, new Vector2(0.5f, 0.2f));

            // Attach Script to Canvas
            DeathScreenUI deathScript = canvasObj.GetComponent<DeathScreenUI>();
            if (deathScript == null) deathScript = canvasObj.AddComponent<DeathScreenUI>();

            // Wire references via reflection
            var deathPanelField = typeof(DeathScreenUI).GetField("deathPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (deathPanelField != null) deathPanelField.SetValue(deathScript, deathPanel);

            var tauntTextField = typeof(DeathScreenUI).GetField("tauntText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (tauntTextField != null) tauntTextField.SetValue(deathScript, tauntText);

            var riseBtnField = typeof(DeathScreenUI).GetField("riseAgainButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (riseBtnField != null) riseBtnField.SetValue(deathScript, riseButton);

            var sanctumBtnField = typeof(DeathScreenUI).GetField("returnToSanctumButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (sanctumBtnField != null) sanctumBtnField.SetValue(deathScript, sanctumButton);

            deathPanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("✅ Death Screen UI successfully added to Canvas!");
        }

        private static Button CreateButton(string name, string textStr, Transform parent, Vector2 anchorPos)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorPos;
            rect.anchorMax = anchorPos;
            rect.sizeDelta = new Vector2(300, 60);
            rect.anchoredPosition = Vector2.zero;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            Button btn = btnObj.AddComponent<Button>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmpro = textObj.AddComponent<TextMeshProUGUI>();
            tmpro.text = textStr;
            tmpro.fontSize = 24;
            tmpro.color = Color.white;
            tmpro.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return btn;
        }
    }
}
#endif
