#version 330 core
layout (location = 0) in vec2 aPosition;//vertex attribute position
layout (location = 1) in vec3 aSize;//vertex attribute sizes
layout (location = 2) in vec3 aCorner;//vertex attribute corner
layout (location = 3) in vec3 aOpacity;//vertex attribute sizes

uniform int layer;
//uniform float pointSizeMin;
//uniform float pointSizeMax;

out float size;
out float cOpacity;
out vec3 centerPoint;
void main()
{
    size = aSize[layer];
    cOpacity = aOpacity[layer];
    centerPoint = vec3(aPosition*2-1,0);
    gl_Position = vec4(aCorner*aSize[layer] + vec3(aPosition*2-1, 0), 1.0);
    //gl_PointSize = (pointSizeMax-pointSizeMin)*aSize[layer]+pointSizeMin;
}