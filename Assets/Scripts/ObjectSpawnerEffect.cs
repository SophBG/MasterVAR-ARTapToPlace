using UnityEngine;
using System.Collections;

public class ObjectSpawnerEffect : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Variable to store the true size of the object
    private Vector3 baseScale; 

    private void Awake()
    {
        // Capture the prefab's scale immediately before any animation modifies it
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        PlayAnimation();
    }

    public void PlayAnimation()
    {
        StopAllCoroutines(); // Cancels previous animation to prevent overlap
        StartCoroutine(AnimateScale());
    }

    private IEnumerator AnimateScale()
    {
        // Always start from zero (invisible)
        transform.localScale = Vector3.zero;

        float timer = 0f;
        
        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / animationDuration;
            
            float curveValue = scaleCurve.Evaluate(progress);
            
            // FIX: Multiply the curve by baseScale (fixed), not transform.localScale (dynamic)
            transform.localScale = baseScale * curveValue;

            yield return null;
        }

        // Ensure it ends exactly at the original intended size
        transform.localScale = baseScale;
    }
}