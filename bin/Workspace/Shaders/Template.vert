#version 330 core

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec2 inUvCoord;
out vec2 texCoord;

void main(void)
{
    texCoord = inUvCoord;

    gl_Position = vec4(inPosition, 1.0);
}