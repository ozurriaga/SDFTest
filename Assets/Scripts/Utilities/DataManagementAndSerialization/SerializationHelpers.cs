using System;
using System.IO;
using UnityEngine;

public static class SerializationHelpers
{
    public static Texture2D SetFromRenderTexture(this Texture2D tex, RenderTexture rTex)
    {
        var old_rt = RenderTexture.active;
        RenderTexture.active = rTex;

        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();

        RenderTexture.active = old_rt;
        return tex;
    }
}
