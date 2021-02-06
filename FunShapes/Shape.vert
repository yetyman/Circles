#version 330 core
layout (location = 0) in vec2 aPosition;//vertex attribute position
layout (location = 1) in vec2 aTexCoord;//vertex attribute position
uniform float aZoom;//vertex attribute corner
uniform float aSize;//vertex attribute corner
uniform vec3 aRowOffset;//vertex attribute corner
uniform vec3 aRowScale;//vertex attribute corner
uniform int aColumnCount;//vertex attribute corner
uniform vec3 corners[9];
uniform int aCornerOffset;

out vec3 normCenterPoint;
out vec2 texCoord;
void main()
{
    texCoord = aTexCoord;

    int odd = ((gl_InstanceID/aColumnCount)%2);
    vec3 centerPoint = vec3(aPosition*2-1,0);
    normCenterPoint = vec3((centerPoint.xy +1)/2,0);
    
    int cornerIndex = gl_VertexID+aCornerOffset * min(gl_VertexID,1);//accounts for 0-8
    int isindex9 = cornerIndex/9;
    cornerIndex = cornerIndex%9 + 1 * isindex9;//accounting for the last vertex of the last triangle

    gl_Position = vec4(corners[cornerIndex]*aSize*vec3((-2*odd+1),1,1) + vec3(aPosition*2-1, 0)*aRowScale + odd * aRowOffset, 1.0)*aZoom;
}