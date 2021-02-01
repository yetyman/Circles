﻿#version 430 core
out float FragColor;
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
    FragColor = aColor.a*opacity*cOpacity;
}