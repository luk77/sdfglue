#version 330

//-------------------------------------------------------------------------------
// main input / output
//-------------------------------------------------------------------------------
in  vec2 texCoord;
out vec4 outputColor;

//-------------------------------------------------------------------------------
// global parameters (using Shadertoy naming conventions)
//-------------------------------------------------------------------------------
uniform float iTime;
uniform int iFrame;
uniform vec3 iResolution;
uniform vec4 iMouse;
uniform sampler2D iChannel0;
uniform sampler2D iChannel1;
uniform sampler2D iChannel2;
uniform sampler2D iChannel3;


//-------------------------------------------------------------------------------
//#define PI                      3.14159265
//#define DISTANCE_BIAS           (1.0)
//-------------------------------------------------------------------------------


//-------------------------------------------------------------------------------
// UV / FragCoords conversion functions
// (for better compatibility with Shadertoy)
// uv - range: 0.0 - 1.0
// frag coords - range: 0.0 - iResolution.xy
//-------------------------------------------------------------------------------
vec2 convertUvToFragCoords(vec2 uv)
{
    uv *= iResolution.xy;
    return uv;
}
vec2 convertFragCoordsToUv(vec2 fragCoords)
{
    fragCoords /= iResolution.xy;
    return fragCoords;
}

//__generated_definitions__
//__common_functions__
//__generated_camera_data__
//__generated_materials__
//__generated_distance_functions__
//__generated_mixop_functions__
//__generated_posop_functions__
//__generated_distop_functions__
//__generated_map_uniforms__
//__generated_map_function__
//__generated_materials_function__
//__generated_renderer_uniforms__
//__generated_backdrop_function__
//__generated_renderer_code__



//-------------------------------------------------------------------------------
//-------------------------------------------------------------------------------
//-------------------------------------------------------------------------------
void main(void)
{
    //__generated_call_init_materials__

    // Pixel perfect discard test
    //if (abs(mod(texCoord.x * iResolution.x, 2.0)) <= 1.0)
    //    discard;
    //
    //if (abs(mod(texCoord.y * iResolution.y, 2.0)) <= 1.0)
    //    discard;

    vec2 fragCoord = convertUvToFragCoords(texCoord);
    
    vec4 col = vec4(0.0);
    mainImage(col, fragCoord);
    col = clamp(col, vec4(0.0), vec4(1.0));

    // iFrame test
    //col.x = (int(iFrame) % 100) * 0.01;

    outputColor = col;
}

