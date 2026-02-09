using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem.EnhancedTouch; 
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch; 

public class ARPlacementManager : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    [Header("Prefabs")]
    public GameObject reticlePrefab; 
    public GameObject objectToSpawnPrefab;

    [Header("Behavior Settings")]
    [Tooltip("If true, moving the finger moves the existing object. If false, every tap spawns a NEW object.")]
    public bool enableRepositioning = true; 

    // Internal state variables
    private GameObject visualReticle;
    private GameObject lastSpawnedObject; // Tracks the single object if repositioning is on
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Awake()
    {
        // Instantiate the reticle but keep it hidden until a plane is found
        visualReticle = Instantiate(reticlePrefab);
        visualReticle.SetActive(false);
    }

    private void OnEnable()
    {
        // Enable the Enhanced Touch system
        EnhancedTouchSupport.Enable();
        Touch.onFingerDown += OnFingerDown;
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        Touch.onFingerDown -= OnFingerDown;
    }

    private void Update()
    {
        UpdateReticle();
    }

    /// <summary>
    /// Casts a ray from screen center to detect planes and updates the Reticle position.
    /// </summary>
    private void UpdateReticle()
    {
        var screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        
        // Raycast against detected planes (Polygon type is more accurate for boundaries)
        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            var hitPose = hits[0].pose;
            
            // Update visual reticle position
            visualReticle.transform.position = hitPose.position;
            visualReticle.transform.rotation = hitPose.rotation; 
            
            if (!visualReticle.activeInHierarchy)
            {
                visualReticle.SetActive(true);
            }
        }
        else
        {
            // Hide reticle if no plane is detected
            if (visualReticle.activeInHierarchy)
            {
                visualReticle.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Handles touch input to spawn or move objects.
    /// </summary>
    private void OnFingerDown(Finger finger)
    {
        // Interaction is only allowed if the Reticle is currently visible
        if (!visualReticle.activeInHierarchy) return;

        if (enableRepositioning)
        {
            // LOGIC A: Reposition Mode
            if (lastSpawnedObject == null)
            {
                // First time placement
                SpawnObject(visualReticle.transform.position, visualReticle.transform.rotation);
            }
            else
            {
                // Move existing object
                MoveObject(lastSpawnedObject, visualReticle.transform.position, visualReticle.transform.rotation);
            }
        }
        else
        {
            // LOGIC B: Multi-Spawn Mode
            SpawnObject(visualReticle.transform.position, visualReticle.transform.rotation);
        }
    }

    private void SpawnObject(Vector3 position, Quaternion rotation)
    {
        GameObject newObj = Instantiate(objectToSpawnPrefab, position, rotation);
        
        // Ensure object faces the camera (UX improvement)
        OrientObjectToCamera(newObj, position);
        
        // Keep reference if we need to move it later
        lastSpawnedObject = newObj;
    }

    private void MoveObject(GameObject obj, Vector3 position, Quaternion rotation)
    {
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        
        OrientObjectToCamera(obj, position);

        // Retrigger the "Pop" animation for feedback
        var animScript = obj.GetComponent<ObjectSpawnerEffect>();
        if (animScript != null)
        {
            animScript.PlayAnimation();
        }
    }

    /// <summary>
    /// Rotates the object to face the main camera, but only on the Y-axis (to keep it upright).
    /// </summary>
    private void OrientObjectToCamera(GameObject obj, Vector3 position)
    {
        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 directionToCamera = cameraPos - position;
        
        // Check if the object's Up vector aligns with World Up (mostly for floors)
        // If it's a wall (dot product close to 0), we might not want this rotation logic
        if (Vector3.Dot(obj.transform.up, Vector3.up) > 0.5f) 
        {
            // Flatten direction to XZ plane so object doesn't tilt up/down
            obj.transform.forward = new Vector3(directionToCamera.x, 0, directionToCamera.z).normalized;
        }
    }
}