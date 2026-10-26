using TMPro;
using UnityEngine;

// Bobs each letter of a TextMeshPro text up and down, one after another, so the word ripples like a
// wave. Moves in whole steps for a pixel-art look, and keeps going while the game is paused.
[RequireComponent(typeof(TMP_Text))]
public class WavyText : MonoBehaviour
{
    [Tooltip("How far a letter moves up and down, in screen units.")]
    public float amplitude = 10f;
    [Tooltip("Waves per second.")]
    public float speed = 1.2f;
    [Tooltip("How far behind its neighbor each letter is, as a fraction of a wave.")]
    public float letterDelay = 0.08f;
    [Tooltip("Letters snap to multiples of this, for chunky pixel steps; 0 moves them smoothly.")]
    public float step = 4f;

    private TMP_Text text;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void LateUpdate()
    {
        text.ForceMeshUpdate();
        TMP_TextInfo info = text.textInfo;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible) continue;

            float phase = (Time.unscaledTime * speed - i * letterDelay) * Mathf.PI * 2f;
            float offset = Mathf.Sin(phase) * amplitude;
            if (step > 0f)
                offset = Mathf.Round(offset / step) * step;

            Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
            for (int v = 0; v < 4; v++)
                vertices[character.vertexIndex + v].y += offset;
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
