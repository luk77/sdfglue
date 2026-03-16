//-------------------------------------------------------------------------------
// Polar camera:
//-------------------------------------------------------------------------------

// TODO: remove references to this include file.
// Replace setPolarCamera() with sdfg_setupCamera() (camera operators)

mat2 setPolarCamera_Rot(float a)
{
    float s = sin(a), c = cos(a);
    return mat2(c, -s, s, c);
}

void setPolarCamera(out vec3 pos, out vec3 ray, in vec3 origin, in vec2 rotation, in float distance, in float zoom, in vec2 fragCoord)
{
    // TODO: optimise...
    
    // input
    float cameraRotationRoll  = 0.0;
    float cameraRotationPitch = rotation.x;
    float cameraRotationYaw   = rotation.y;
    vec3  cameraTargetPosition= origin;
    
    // ray in view space
    ray.xy = fragCoord.xy - iResolution.xy * 0.5;
    ray.z = iResolution.y*zoom;
    
    // roll rotation
    ray.xy = SDFG_MUL(setPolarCamera_Rot(cameraRotationRoll), ray.xy);
    
    // pitch rotation
    ray.yz = SDFG_MUL(setPolarCamera_Rot(cameraRotationPitch), ray.yz);

    // yaw rotation
    ray.xz = SDFG_MUL(setPolarCamera_Rot(-cameraRotationYaw), ray.xz);

    ray = normalize(ray);

    // calculate offset (from camera pos to target)
    vec3 offset = vec3(0.0, 0.0, 1.0);
    
    // roll rotation
    offset.xy = SDFG_MUL(setPolarCamera_Rot(cameraRotationRoll), offset.xy);

    // pitch rotation
    offset.yz = SDFG_MUL(setPolarCamera_Rot(cameraRotationPitch), offset.yz);

    // yaw rotation
    offset.xz = SDFG_MUL(setPolarCamera_Rot(-cameraRotationYaw), offset.xz);
    
    offset = normalize(offset);

    // camera position (ray origin)
    pos = cameraTargetPosition - distance * offset;
}

