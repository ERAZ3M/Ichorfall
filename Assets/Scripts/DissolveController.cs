using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DissolveController : MonoBehaviour
{
    [Tooltip("Time in seconds to fully dissolve")]
    public float dissolveDuration = 1.5f;

    private List<Renderer> renderers = new List<Renderer>();
    private List<MaterialPropertyBlock> propBlocks = new List<MaterialPropertyBlock>();
    private string cutoffProperty = "_Dissolve";

    private void Awake()
    {
        // Get ALL renderers in this GameObject AND all children (including sword)
        GetComponentsInChildren<Renderer>(true, renderers);
        
        if (renderers.Count == 0)
        {
            Debug.LogError($"DissolveController: No Renderers found on {gameObject.name} or its children.");
            return;
        }

        Debug.Log($"DissolveController found {renderers.Count} renderers to dissolve.");

        // Initialize property blocks for each renderer
        foreach (var rend in renderers)
        {
            var block = new MaterialPropertyBlock();
            rend.GetPropertyBlock(block);
            block.SetFloat(cutoffProperty, 0f);
            rend.SetPropertyBlock(block);
            propBlocks.Add(block);
        }
    }

    public void StartDissolve(System.Action onComplete = null)
    {
        StartCoroutine(DissolveRoutine(onComplete));
    }

    private IEnumerator DissolveRoutine(System.Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dissolveDuration);
            SetCutoffOnAll(t);
            yield return null;
        }
        SetCutoffOnAll(1f);
        onComplete?.Invoke();
    }

    private void SetCutoffOnAll(float value)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
            {
                propBlocks[i].SetFloat(cutoffProperty, value);
                renderers[i].SetPropertyBlock(propBlocks[i]);
            }
        }
    }

    public void ResetDissolve()
    {
        SetCutoffOnAll(0f);
    }
}