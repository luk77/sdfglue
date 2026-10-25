//-------------------------------------------------------------------------------
// Basic raymarching functions
//-------------------------------------------------------------------------------

// definitions
#define DISTANCE_BIAS           (1.0)
#define OUTLINE_EPSILON         (0.0001)

// forward declarations
float getDist(vec3 p);

//-------------------------------------------------------------------------------
float rayMarch(in vec3 ro, in vec3 rd, in float minDist, in float maxDist)
{
    float   dist    = minDist;
    float   d       = 0.0;
    vec3    pos     = vec3(0.0);

    for(int i=0; i<MAX_STEPS; i++)
    {
        pos = ro + dist * rd;
        d = getDist(pos);
        dist += d * DISTANCE_BIAS;
        if (dist < minDist)
            break;
        
        if (dist > maxDist)
            return maxDist;
    }
    
    return dist;
}

//-------------------------------------------------------------------------------
float rayMarchWithOutline(in vec3 ro, in vec3 rd, in float minDist, in float maxDist, in float outlineWidth, out float resultOutline)
{
    float   dist    = minDist;
    float   d       = 0.0;
    vec3    pos     = vec3(0.0);
    
    resultOutline = 0.0;
    float prevDist = 1e10;
    
    for(int i=0; i<MAX_STEPS; i++)
    {
        pos = ro + dist * rd;
        d = getDist(pos);
        dist += d * DISTANCE_BIAS;

        if (dist<minDist)
            break;
        
        if (dist>maxDist)
            return maxDist;
        
        if (prevDist < outlineWidth && d > prevDist + OUTLINE_EPSILON) 
        {
            resultOutline = 1.0;
            break;
        }

        prevDist = d;
    }
    
    return dist;
}

//-------------------------------------------------------------------------------
float shadowMarch(in vec3 ro, in vec3 rd, in float shadowSharpness)
{
    float dist = 0.0;
    
    float res = 1.0;
    
    for(int i=0; i<MAX_STEPS; i++)
    {
        vec3 pos = ro + dist * rd;
        
        float d = getDist(pos);
        if (d < 0.01)
            return 0.0;
        
        dist += d * DISTANCE_BIAS;
        
        res = min(res, shadowSharpness * d / dist);
        
        if (dist<MIN_DIST || dist>MAX_DIST)
            break;
    }

    return res;
}

