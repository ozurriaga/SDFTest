using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class RMObjectComponent : SerializedMonoBehaviour
{
    private static Mesh cylinderMesh; // For gizmo renedring

    [MenuItem("GameObject/Ray Marching/Volume Object", priority = 1)]
    private static void CreateObjectInHierarchy(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("New RM Object", typeof(RMObjectComponent));
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }

    public int ReferenceId { get; set; }

    [Range(0.0f, 20.0f)]
    public float normalSharpness = DEFAULT_NORMAL_SHARPNESS;
    private const float DEFAULT_NORMAL_SHARPNESS = 5.0f;

    public RMVolumeType volumeType;

    [ShowInInspector, NonSerialized, OdinSerialize, InlineProperty, LabelText(""), HideReferenceObjectPicker]
    private DataSelector<RMVolumeType, IParameterData> parameterSelector;

    private void Awake()
    {
        parameterSelector = parameterSelector ?? new DataSelector<RMVolumeType, IParameterData>();
    }

    static RMObjectComponent()
    {
        DataSelector<RMVolumeType, IParameterData>.constructorFunc = NewParameterData;
    }

    private static IParameterData NewParameterData(RMVolumeType volumeType)
    {
        switch (volumeType)
        {
            case RMVolumeType.Link:
            case RMVolumeType.Torus: 
                return new ParameterData<TorusParameters>();
            case RMVolumeType.CappedTorus: 
                return new ParameterData<CappedTorusParameters>();
            case RMVolumeType.BoxFrame: 
                return new ParameterData<BoxFrameParameters>();
            case RMVolumeType.Reference:
                return new ReferenceParametersSelector();
            case RMVolumeType.Cone:                
            case RMVolumeType.None:
            case RMVolumeType.Sphere:
            case RMVolumeType.Plane:
            case RMVolumeType.Box:
            case RMVolumeType.Cylinder:
            case RMVolumeType.Capsule:
            case RMVolumeType.SDF:
            default: break;
        }
        return null;
    }

    public static RMVolumeData GetEmptyObjectBufferData()
    {
        RMVolumeData bufferData = new RMVolumeData()
        {
            volumeType = RMVolumeType.None, 
            normalSharpness = DEFAULT_NORMAL_SHARPNESS
        };

        bufferData.SetTransformAndScale(Matrix4x4.identity, Matrix4x4.identity, Vector3.one);
        bufferData.paramsId = -1;
        
        return bufferData;
    }

    public RMVolumeData GetBufferData()
    {
        RMVolumeData bufferData = new RMVolumeData();

        bufferData.SetTransformAndScale(transform.localToWorldMatrix, transform.worldToLocalMatrix, transform.lossyScale);
        bufferData.volumeType = volumeType;
        
        bufferData.paramsId = parameterSelector?.Parameter?.AddToBuffer()?? -1;

        bufferData.normalSharpness = normalSharpness;

        return bufferData;
    }

    public virtual void OnValidate()
    {
        RMObjectsManager.Instance.HasToRefresh = true;
        RMMaterialsManager.Instance.HasToRefresh = true;

        if (parameterSelector != null)
        {
            parameterSelector.Selection = volumeType;
        }
    }

    void OnEnable()
    {
        RMObjectsManager.Instance.HasToRefresh = true;
        RMMaterialsManager.Instance.HasToRefresh = true;
    }

    void OnDisable()
    {
        RMObjectsManager.Instance.HasToRefresh = true;
        RMMaterialsManager.Instance.HasToRefresh = true;
    }

    private void InitializeCylinderMesh()
    {
        if (!cylinderMesh)
        {
            GameObject cylinderGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinderMesh = cylinderGO.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(cylinderGO);
        }
    }

    private void OnDrawGizmos()
    {
        InitializeCylinderMesh();

        // To make it selectable
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.clear;
        switch (volumeType)
        {
            case RMVolumeType.None: break;
            case RMVolumeType.Sphere: Gizmos.DrawSphere(Vector3.zero, 1); break;
            case RMVolumeType.Plane: Gizmos.DrawCube(Vector3.zero, new Vector3(15, 0, 15)); break;
            case RMVolumeType.Cylinder: Gizmos.DrawMesh(cylinderMesh); break;
            default: Gizmos.DrawCube(Vector3.zero, Vector3.one); break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        InitializeCylinderMesh();

        Gizmos.color = new Color(0.0f, 0.75f, 0.0f, 0.2f);
        Gizmos.matrix = transform.localToWorldMatrix;

        switch (volumeType)
        {
            case RMVolumeType.None : break;
            case RMVolumeType.Sphere : Gizmos.DrawWireSphere(Vector3.zero, 1); break;
            case RMVolumeType.Plane : Gizmos.DrawWireCube(Vector3.zero, new Vector3(15, 0, 15)); break;
            case RMVolumeType.Cylinder : Gizmos.DrawWireMesh(cylinderMesh); break;
            default : Gizmos.DrawWireCube(Vector3.zero, Vector3.one); break;
        }
    }
}
