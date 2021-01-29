#version 330 core
out vec4 FragColor;
in vec4 circleColor;
in float size;
in float cOpacity;
in vec3 centerPoint;

uniform vec2 viewPortSize;
uniform vec4 aColor;
void main()
{
    float normalizedSize = size/2;
    vec2 normalizedCenterPoint = (centerPoint.xy +1)/2;
    vec2 normalizedCoordPoint = gl_FragCoord.xy/viewPortSize;
    float opacity = max(normalizedSize/2-length(normalizedCenterPoint.xy - normalizedCoordPoint.xy),0)/(normalizedSize/2);
    FragColor = vec4(aColor.r, aColor.g, aColor.b, aColor.a*opacity*cOpacity);
    //FragColor = vec4(aColor.r, aColor.g, aColor.b, opacity);
    //FragColor = vec4(1,normalizedCoordPoint.x,normalizedCenterPoint.x,1);
    //FragColor = vec4(1,0,0,opacity);
}