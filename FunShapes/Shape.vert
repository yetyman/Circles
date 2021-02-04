#version 330 core
layout (location = 0) in vec2 aPosition;//vertex attribute position
layout (location = 1) in vec3 aCorner;//vertex attribute corner
uniform float aZoom;//vertex attribute corner
uniform float aSize;//vertex attribute corner
uniform vec3 aRowOffset;//vertex attribute corner
uniform vec3 aRowScale;//vertex attribute corner
uniform int aColumnCount;//vertex attribute corner

out vec3 centerPoint;
void main()
{
    int odd = ((gl_InstanceID/aColumnCount)%2);
    centerPoint = vec3(aPosition*2-1,0);
    gl_Position = vec4(aCorner*aSize*vec3((-2*odd+1),1,1) + vec3(aPosition*2-1, 0)*aRowScale + odd * aRowOffset, 1.0)*aZoom;
}