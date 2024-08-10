#if false

#ifndef RAY_MARCHING_LIBRARY_DATATYPES_INCLUDED
#define RAY_MARCHING_LIBRARY_DATATYPES_INCLUDED

#include "RayMarchingShaderLibraryOP.cginc"

//STACK TOOLS
#define MAX_STACK_DEPTH 100
#define STACK_NAME(name) name##_stack
#define STACK_DEPTH(name) name##_depth

#define STACK(name, type) \
    int STACK_DEPTH(name) = 0; \
    type STACK_NAME(name)[MAX_STACK_DEPTH];

#define STACK_CLEAR(name)         STACK_DEPTH(name) = 0
#define STACK_PUSH(name, value)   STACK_NAME(name)[STACK_DEPTH(name)++] = value
#define STACK_TOP(name)           STACK_NAME(name)[STACK_DEPTH(name)-1]
#define STACK_POP(name)           STACK_NAME(name)[--STACK_DEPTH(name)]
#define STACK_IS_EMPTY(name)      (STACK_DEPTH(name)==0)

//BUFFER TOOLS
#define SB(type) _RMParams_##type
#define SBCount(type) _RMParams_##type##Count
#define PARAMS_BUFFER(type) SBUFFER(type, SB(type))
#define SET_TYPE(type, pos, i) SB(type)[_BufferOffset[pos]+i]
#define SBUFFER(type, name) \
    uniform int name##Count; \
    uniform StructuredBuffer<type> name;

#define RWSBUFFER(type, name) \
    uniform int name##Count; \
    uniform RWStructuredBuffer<type> name;

#define BUFFER(type, name) \
    uniform int name##Count; \
    uniform Buffer<type> name;

#define RWBUFFER(type, name) \
    uniform int name##Count; \
    uniform RWBuffer<type> name;

//OFFSETS BUFFER INITIALIZATION
SBUFFER(int, _BufferOffset)

//DEFINE DataBuffer INFORMATION
#define TorusParameters(i) SET_TYPE(float, 1, i)
#define CappedTorusParameters(i) SET_TYPE(float2, 2, i)
#define BoxFrameParameters(i) SET_TYPE(float, 3, i)
#define ReferenceParameters(i) SET_TYPE(int, 4, i)
#define RMOperation(i) SET_TYPE(int2, 5, i)


#define IS_ROOT_OBJECT(objectData) (objectData.parentIndex == ROOT_OBJECT_PARENT_INDEX)
/* ||
objectData.operation == RMOperationTypeRAdd ||\
objectData.operation == RMOperationTypeRSub ||\
objectData.operation == RMOperationTypeRInt)*/

struct RMObjectData
{
    float4x4 objectToWorldMatrix;
    float4x4 worldToObjectMatrix;
    float3 scale;

    int parentIndex;

    int operation;
    float operationBlend;
    float operationSoftness;

    int volumeType;
    int paramsId;
};

struct RMOperationData
{

};

#endif

#endif