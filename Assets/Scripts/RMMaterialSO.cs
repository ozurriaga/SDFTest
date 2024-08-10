using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RMMat", menuName = "Ray Marching/Ray Marching Material", order = 1)]
public class RMMaterialSO : ScriptableObject
{
    public Color color = Color.white;
    public Texture2D albedoTex;
    public Texture2D normalMap;
    public Texture2D heightMap;

    /*public virtual void OnValidate()
    {
        RMMaterialsManager.Instance.RefreshMaterialsList();
    }*/

    /*public RMMaterialBufferData GetBufferData()
    {
        RMMaterialBufferData bufferData = new RMMaterialBufferData();

        bufferData.SetColor(color);
        //bufferData.albedoTexIndex = 

        //bufferData.modifier = modifier;

        return bufferData;
    }*/
}
