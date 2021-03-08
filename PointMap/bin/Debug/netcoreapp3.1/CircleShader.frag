#version 430 core
out vec4 FragColor;
in vec4 circleColor;
in float size;
in float circleOpacity;
in vec3 centerPoint;

uniform vec2 viewPortSize;
uniform vec4 aColor;
void main()
{
    float normalizedSize = size/2;
    vec2 normalizedCenterPoint = ((centerPoint.xy +1)/2)*viewPortSize;
    vec2 normalizedCoordPoint = gl_FragCoord.xy;
    if(length(normalizedCenterPoint - normalizedCoordPoint)<.5)
        FragColor = vec4(1,1,1,1);
    else
        FragColor = vec4(0,0,0,0);
}