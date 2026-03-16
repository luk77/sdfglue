//---------------------------------------------------------------------------
// Dithering
//---------------------------------------------------------------------------
#if USE_DITHERING

#define DITHERING_STEPS 5

// Simple pseudo-random generator based on pixel coordinates
float ditheringHash(in vec2 p)
{
    return fract(sin(dot(p.xy ,vec2(12.9898,78.233))) * 43758.5453123);
}

vec3 applyDither(in vec3 color, in vec2 fragCoord, in vec2 seed)
{
    // Combine fragCoord and optional seed
    //vec2 coord = fragCoord + seed;
    vec2 coord = ( (fragCoord.xy + seed) / iResolution.xy );

    // Generate noise in range [-1/255, +1/255]
    //float noise = (ditheringHash(coord) - 0.5) / 255.0;
    float noise = ditheringHash(coord) - 0.5;
    noise += ditheringHash(coord + 0.34567);
    noise -= ditheringHash(coord + 0.67891);
    noise /= 255.0;

    // Apply noise to each color channel
    vec3 result = color + vec3(noise);

    // Clamp to [0,1] to avoid overflow
    return clamp(result, 0.0, 1.0);
}

#endif
//---------------------------------------------------------------------------
// Dithering (end)
//---------------------------------------------------------------------------

