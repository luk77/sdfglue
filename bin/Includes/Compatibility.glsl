//---------------------------------------------------------------------------
// Compatibility macros
//---------------------------------------------------------------------------

#define USE_GLSL        (1)


#if USE_GLSL

    // GLSL
    #define SDFG_LERP           mix
    #define SDFG_ATAN           atan
    #define SDFG_FRACT          fract
    
    // Matrix/vector multiplication
    #define SDFG_MUL(xxx, yyy)  yyy * xxx
    
#else

    // HLSL
    #define SDFG_LERP           lerp
    #define SDFG_ATAN           atan2
    #define SDFG_FRACT          frac

    // Matrix/vector multiplication
    // Multiplies x and y using matrix math. The inner dimension x-columns and y-rows must be equal.
    // https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-mul
    #define SDFG_MUL(xxx, yyy)  mul(xxx, yyy);

#endif

