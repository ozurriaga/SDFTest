using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class RMMaterialsManager : MonoBehaviour
{
    public List<RMMaterialSO> materials = new List<RMMaterialSO>();

    public static RMMaterialsManager Instance => instance ?? new RMMaterialsManager();

    public bool HasToRefresh { get; internal set; }

    private static RMMaterialsManager instance;

    // inspector utilities
    public bool setAsSingletonInstance = false;
    public bool refresh = false;

    public RMMaterialsManager()
    {
        instance = this;
    }

    private void Update()
    {
        if (setAsSingletonInstance)
        {
            setAsSingletonInstance = false;
            instance = this;
        }

        if (refresh)
        {
            HasToRefresh = true;
            refresh = false;
        }

        if (HasToRefresh)
        {
            RefreshMaterialsList();
        }
    }

    public void RefreshMaterialsList()
    {
        materials.Clear();
        foreach (var rootGO in gameObject.scene.GetRootGameObjects())
        {
            foreach (var objectComponent in rootGO.GetComponentsInChildren<RMObjectComponent>())
            {
                //var material = objectComponent.material;
                //if (material != null && !materials.Contains(material)) materials.Add(material);
            }
        }
        HasToRefresh = false;
    }

    public ComputeBuffer GetBufferData()
    {
        /*computeShader.SetConstantBuffer()

        RayMarchableObjectBufferData bufferData = new RayMarchableObjectBufferData();

        bufferData.SetTransformMatrix(transform.localToWorldMatrix);
        bufferData.volumeType = volumeType;

        bufferData.operation = operation;
        bufferData.operationBlend = operationBlend;

        //bufferData.modifier = modifier;
        */
        return null;
    }
}
