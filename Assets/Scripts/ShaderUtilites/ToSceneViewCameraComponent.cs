using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ToSceneViewCameraComponent : MonoBehaviour
{
#if UNITY_EDITOR
    bool hasToUpdate = false;
    public virtual void OnValidate() => hasToUpdate = true;
    static ToSceneViewCameraComponent() => SceneView.beforeSceneGui += CheckForCameraComponentsToUpdate;

    static void CheckForCameraComponentsToUpdate(SceneView sceneView)
    {
        Camera cameraReference = Camera.main;
        if (Event.current.type != EventType.Layout || !cameraReference)
            return;

        ToSceneViewCameraComponent[] cameraComponents = cameraReference.GetComponents<ToSceneViewCameraComponent>();
        ToSceneViewCameraComponent[] sceneComponents = sceneView.camera.GetComponents<ToSceneViewCameraComponent>();

        bool hasToRebuildComponents = cameraComponents.Length != sceneComponents.Length;
        for (int i = 0; i < cameraComponents.Length && !hasToRebuildComponents; i++)
        {
            hasToRebuildComponents |= cameraComponents[i].GetType() != sceneComponents[i].GetType();
        }

        if (hasToRebuildComponents)
        {
            ToSceneViewCameraComponent componentToDestroy;
            while (componentToDestroy = sceneView.camera.GetComponent<ToSceneViewCameraComponent>())
                DestroyImmediate(componentToDestroy);
            
            foreach (ToSceneViewCameraComponent componentToCopy in cameraComponents)
                (sceneView.camera.gameObject.AddComponent(componentToCopy.GetType()) as ToSceneViewCameraComponent).hasToUpdate = true;
        } else
        {
            for (int i = 0; i < cameraComponents.Length; i++)
            {
                if (cameraComponents[i].hasToUpdate || (sceneComponents[i].enabled != cameraComponents[i].enabled))
                {
                    EditorUtility.CopySerialized(cameraComponents[i], sceneComponents[i]);
                    cameraComponents[i].hasToUpdate = false;
                }
            }
        }
    }
#endif
}
