using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum RMOperationType
{
    Group = 0,
    Add = 1,
    Sub = 2,
    Int = 3
}

public enum RMModifierType
{
    None
}

public enum RMVolumeType
{
    None  = 0,
    Sphere = 1,
    Plane = 2,
    Box = 3,
    BoxFrame = 4,
    Cylinder = 5,
    Torus = 6,
    CappedTorus = 7,
    Link = 8,
    Cone = 9,
    Capsule = 10,
    SDF = 20,
    Reference = 50
}

public struct RMVolumeData
{
    public Matrix4x4 objectToWorldMatrix;
    public Matrix4x4 worldToObjectMatrix;
    public Vector3 scale;

    public RMVolumeType volumeType;
    public int paramsId;

    [Range(0.0f, 20.0f)]
    public float normalSharpness;
    
    public void SetTransformAndScale(Matrix4x4 lw, Matrix4x4 wl, Vector3 s)
    {
        objectToWorldMatrix = lw;
        worldToObjectMatrix = wl;
        scale = s;
    }
}

[Serializable]
public struct RMOperationData
{
    public RMOperationType operationType;
    public int operationInfo;
    public float operationBlend;
    public float operationSoftness;

    public override string ToString() => $"T: {operationType} I: {operationInfo} S/B: {operationSoftness}/{operationBlend}";
}

[Serializable]
public struct TorusParameters
{
    [Range(0.0f, 0.5f)]
    public float sectionRadius;
}

[Serializable]
public struct CappedTorusParameters
{
    [Range(0.0f, 0.5f)]
    public float sectionRadius;

    [Range(0.0f, Mathf.PI)]
    public float arcRadiants;
}

[Serializable]
public struct TwistModifierParameters
{
    public float TwistSpeed;
    public float amount;
}

[Serializable]
public struct BoxFrameParameters
{
    public float frameSize;
}

[Serializable]
public struct ReferenceParameters
{
    public int referenceId;
}

public class ReferenceParametersSelector : IParameterData
{
    [HideInInspector]
    ParameterData<ReferenceParameters> shaderData = new ParameterData<ReferenceParameters>();

    [SerializeReference, ShowInInspector]
    public RMOperationGroupComponent referencedRMObject;

    public int AddToBuffer()
    {
        if (shaderData == null)
        {
            shaderData = new ParameterData<ReferenceParameters>();
        }
        int referencePosition = 0;
        if (!(referencedRMObject is null))
        {
            shaderData.data.referenceId = referencedRMObject.ReferenceId;
            referencePosition = shaderData.AddToBuffer();
        }
        return referencePosition;
    }
}

/*public struct RMMaterialBufferData
{
    public Vector4 color;
    public int albedoTexIndex;
    public int normalMapIndex;
    public int heightMapIndex;

    public void SetColor(Color color)
    {
        this.color.x = color.r;
        this.color.y = color.g;
        this.color.z = color.b;
        this.color.w = color.a;
    }
}*/
