using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[Serializable]
public class DataSelector<TSelector, TData> where TData : class
{
    private bool showParameterInInspector = false;
    public delegate TData ConstructorDelegate(TSelector selection);

    public static ConstructorDelegate constructorFunc;

    [SerializeField, HideInInspector]
    private TSelector selectedParameterType;

    [OdinSerialize, HideInInspector]
    Dictionary<TSelector, TData> parametersInfo = new Dictionary<TSelector, TData>();

    [ShowIf("showParameterInInspector"), ShowInInspector, InlineProperty, LabelText(""), HideReferenceObjectPicker]
    private TData currentParameter;

    public TSelector Selection
    {
        get => selectedParameterType;
        set
        {
            selectedParameterType = value;
            if (parametersInfo.ContainsKey(value))
            {
                currentParameter = parametersInfo[selectedParameterType];
            }
            else
            {
                Parameter = constructorFunc?.Invoke(value);
            }
            showParameterInInspector = currentParameter != null;
        }
    }

    public TData Parameter
    {
        get => currentParameter == null ? null : currentParameter;
        set
        {
            if (!parametersInfo.TryAdd(Selection, value)) parametersInfo[Selection] = value;
            currentParameter = parametersInfo[Selection];
            showParameterInInspector = currentParameter != null;
        }
    }
}