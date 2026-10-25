//-------------------------------------------------------------------------------
// Smooth "mod"
// (used by "Smooth Repeat" operators)
//-------------------------------------------------------------------------------

float modSaw(float x, float spacing)
{
    return abs(mod(x - spacing, 2.0 * spacing) - spacing);
}

float modEx(float x, float spacing, float smoothness)
{
//    return mod(x, spacing);
//    return modSaw(x, spacing);
    
    float hx = 0.5*x;
    float z = 0.5*spacing*smoothness;
    float s = spacing;

    float f1 = sqrt(max(2.0*z*z - modSaw(x+s, s) * modSaw(x+s, s), 0.0 )) + s - 2.0*z;
    float f2 =-sqrt(max(2.0*z*z - modSaw(x  , s) * modSaw(x  , s), 0.0 ))     + 2.0*z;
    float f3 = modSaw(x, spacing);
    
    float s1 = 0.5 + 0.5 * sign(-abs(mod(x  , 2.0 * s) - s) + z);
    float s2 = 0.5 + 0.5 * sign(-abs(mod(x-s, 2.0 * s) - s) + z);
    float s3 = 1.0 - (s1 + s2);

    return f1*s1 + f2*s2 + f3*s3;
}

vec2 modEx(vec2 val, vec2 spacing, vec2 smoothness)
{
    return vec2(modEx(val.x, spacing.x, smoothness.x),
                modEx(val.y, spacing.y, smoothness.y));
}

vec3 modEx(vec3 val, vec3 spacing, vec3 smoothness)
{
    return vec3(modEx(val.x, spacing.x, smoothness.x),
                modEx(val.y, spacing.y, smoothness.y),
                modEx(val.z, spacing.z, smoothness.z));
}

