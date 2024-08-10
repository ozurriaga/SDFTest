using System;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class FullScreenCameraRMHelper : ToSceneViewCameraComponent
{
    private enum FrustumCorners : int
    {
        TopLeft = 0,
        TopRight = 1,        
        BottomLeft = 2,
        BottomRight = 3
    };

    [SerializeField]
    private Shader rayMarchingShader;

    [Range(1, 500)]
    public int maxSteps = 100;

    [Range(0.00001f, 0.1f)]
    public float surfaceDistance = 0.001f;

    [NonSerialized]
    public Texture2D renderToTexture;

   /* public Texture2D[] materials;
    private Texture2DArray materialsArray;*/

    public Material RayMarchingMaterial
    {
        get
        {
            if (!rayMarchingMaterial && rayMarchingShader)
            {
                rayMarchingMaterial = new Material(rayMarchingShader);
                rayMarchingMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            return rayMarchingMaterial;
        }
    }
    private Material rayMarchingMaterial;

    public Camera CurrentCamera => currentCamera ?? (currentCamera = GetComponent<Camera>());

    public object RMBufferManagerComponent { get; private set; }

    private Camera currentCamera;

    void OnEnable() => Camera.main.depthTextureMode = DepthTextureMode.Depth;

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!RayMarchingMaterial)
        {
            Graphics.Blit(source, destination);
            return;
        }

        //int objectsCount = 0;
        RMObjectsManager.Instance.FillObjectsAndOperationsDataBuffers();
        //materialsBuffer = RMMaterialsManager.Instance.GetBufferData();
        RMBuffersManagerComponent.Instance.SetBuffers(rayMarchingMaterial);
        
        /*rayMarchingMaterial.SetBuffer("_RMMaterials", RMMaterialsManager.Instance.GetBufferData());
        if (materialsArray == null || materials == null || materialsArray.depth != materials.Length)
        {
            materialsArray = new Texture2DArray(512, 512, materials != null ? materials.Length : 0, TextureFormat.RGB24, false);
            for (int i = 0; i< materials.Length; i++)
            {
                materialsArray.SetPixels(materials[i].GetPixels(0), i, 0);
            }
            materialsArray.Apply();
        }*/

        // Rendered scene
        RayMarchingMaterial.SetTexture("_MainTex", source);

        // Camera Parameters
        RayMarchingMaterial.SetFloat("_FarPlane", CurrentCamera.farClipPlane);
        RayMarchingMaterial.SetMatrix("_CameraFrustumCornersMatrixCS", GetFrustumCornersMatrix(CurrentCamera));
        
        // Ray Marching Parameters
        RayMarchingMaterial.SetInteger("_MaxSteps", maxSteps);
        RayMarchingMaterial.SetFloat("_SurfaceDistance", surfaceDistance);

        RayMarchingMaterial.SetInteger("_SceneStartObject", RMObjectsManager.Instance.SceneStartObject);

        // Material Parameters
        //RayMarchingMaterial.SetTexture("_Materials", materialsArray);

        Graphics.Blit(source, destination, RayMarchingMaterial);

        if (renderToTexture != null)
        {
            RenderTexture renderTexture = new RenderTexture(renderToTexture.width, renderToTexture.height, 32);
            Graphics.Blit(destination, renderTexture);
            renderToTexture.SetFromRenderTexture(renderTexture);
            renderToTexture = null;
        }
    }

    private void OnPostRender()
    {
        RMBuffersManagerComponent.Instance.Clear();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Matrix4x4 corners = GetFrustumCornersMatrix(CurrentCamera);
        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(CurrentCamera.transform.position, (Vector3)corners.GetRow(i) + CurrentCamera.transform.position);
    }

    private Matrix4x4 GetFrustumCornersMatrix(Camera camera)
    {
        float normalizedHalfHeight = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 rightCameraSpace = Vector3.right * normalizedHalfHeight * camera.aspect;
        Vector3 topCameraSpace = Vector3.up * normalizedHalfHeight;
        Vector3 forwardCameraSpace = Vector3.forward;

        Matrix4x4 frustumMatrix = new Matrix4x4();
        frustumMatrix.SetRow((int)FrustumCorners.TopLeft, camera.transform.rotation * (forwardCameraSpace + topCameraSpace - rightCameraSpace));
        frustumMatrix.SetRow((int)FrustumCorners.TopRight, camera.transform.rotation * (forwardCameraSpace + topCameraSpace + rightCameraSpace));
        frustumMatrix.SetRow((int)FrustumCorners.BottomRight, camera.transform.rotation * (forwardCameraSpace - topCameraSpace + rightCameraSpace));
        frustumMatrix.SetRow((int)FrustumCorners.BottomLeft, camera.transform.rotation * (forwardCameraSpace - topCameraSpace - rightCameraSpace));
        return frustumMatrix;
    }
}