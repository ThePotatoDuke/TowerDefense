using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Add Shop Description Windows (with the game scene open): puts the description window board behind
// each shop card's description text, fitted to the text's box, sliced so it stretches, with its art pixels
// the same size as the card's. Running it again refits the ones already there.
public static class ShopDescriptionWindows
{
    private const string SpritePath = "Assets/3rd Party/UI/description_window.png";
    private const string WindowName = "DescriptionWindow";
    // Room round the text's box, in art pixels: sideways, then up and down (negative = smaller than the box)
    private static readonly Vector2 Padding = new(1f, 0f);

    [MenuItem("Tools/Add Shop Description Windows")]
    private static void Add()
    {
        Shop shop = Object.FindAnyObjectByType<Shop>(FindObjectsInactive.Include);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (shop == null || sprite == null)
        {
            Debug.LogWarning($"Shop description windows: needs the Shop in the open scene and a sprite at {SpritePath}.");
            return;
        }

        int added = 0;
        SerializedProperty cards = new SerializedObject(shop).FindProperty("cards");
        for (int i = 0; i < cards.arraySize; i++)
        {
            SerializedProperty card = cards.GetArrayElementAtIndex(i);
            var text = card.FindPropertyRelative("descriptionText").objectReferenceValue as TMP_Text;
            var button = card.FindPropertyRelative("button").objectReferenceValue as Button;
            if (text == null) continue;

            Transform existing = text.transform.parent.Find(WindowName);
            GameObject window;
            if (existing != null)
            {
                window = existing.gameObject;
                Undo.RecordObjects(new Object[] { window.GetComponent<RectTransform>(), window.GetComponent<Image>() }, "Fit Shop Description Windows");
            }
            else
            {
                window = new GameObject(WindowName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(window, "Add Shop Description Windows");
                window.transform.SetParent(text.transform.parent, false);
                window.transform.SetSiblingIndex(text.transform.GetSiblingIndex()); // behind the text
            }
            var rect = window.GetComponent<RectTransform>();

            // The card's own pixel size, so the window's pixels match it
            Image board = button != null ? button.targetGraphic as Image : null;
            float pixel = board != null ? UIPixels.PixelSize(board) : UIPixels.Scale;
            Vector2 grow = Padding * pixel * 2f;

            RectTransform textRect = text.rectTransform;
            rect.anchorMin = textRect.anchorMin;
            rect.anchorMax = textRect.anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = textRect.sizeDelta + grow;
            // Centred on the text's box
            Vector2 textCentre = textRect.anchoredPosition + (new Vector2(0.5f, 0.5f) - textRect.pivot) * textRect.sizeDelta;
            rect.anchoredPosition = textCentre;

            var image = window.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            Canvas canvas = text.canvas;
            float referencePixelsPerUnit = canvas != null ? canvas.rootCanvas.referencePixelsPerUnit : 100f;
            image.pixelsPerUnitMultiplier = referencePixelsPerUnit / (sprite.pixelsPerUnit * pixel);
            added++;
        }

        if (added > 0)
            EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
        Debug.Log($"Shop description windows: fitted {added}. Adjust them as wanted, then save the scene.");
    }
}
