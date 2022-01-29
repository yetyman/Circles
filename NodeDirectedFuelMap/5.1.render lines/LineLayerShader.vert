#version 330 core
layout (location = 0) in vec2 aPosition;//vertex attribute position

uniform int layer;

out vec4 color;

float rand(vec2 co){
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}
void main()
{
    gl_Position = vec4(aPosition,0,1);
    color = vec4(rand(aPosition+.2), rand(aPosition+.1), rand(aPosition), 1);
    //gl_PointSize = (pointSizeMax-pointSizeMin)*aSize[layer]+pointSizeMin;
}
