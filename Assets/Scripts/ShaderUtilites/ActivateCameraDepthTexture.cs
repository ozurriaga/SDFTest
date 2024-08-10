using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ActivateCameraDepthTexture : MonoBehaviour
{
    void OnUpdate()
    {
        if (Camera.main.depthTextureMode != DepthTextureMode.Depth) Camera.main.depthTextureMode = DepthTextureMode.Depth;
    }
}
