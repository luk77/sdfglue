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


//__generated_code__



//-------------------------------------------------------------------------------
//-------------------------------------------------------------------------------
//-------------------------------------------------------------------------------
void main(void)
{
    // Pixel perfect discard test
    //if (abs(mod(texCoord.x * iResolution.x, 2.0)) <= 1.0)
    //    discard;
    //
    //if (abs(mod(texCoord.y * iResolution.y, 2.0)) <= 1.0)
    //    discard;

    vec2 fragCoord = texCoord * iResolution.xy;
    
    vec4 col = vec4(0.0);
    mainImage(col, fragCoord);
    col = clamp(col, vec4(0.0), vec4(1.0));

    // iFrame test
    //col.x = (int(iFrame) % 100) * 0.01;

    outputColor = col;
}

