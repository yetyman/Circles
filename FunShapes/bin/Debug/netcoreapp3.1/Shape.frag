#version 430 core
out vec4 FragColor;
in vec3 normCenterPoint;
in float vertexId;
in vec2 texCoord;

uniform vec2 viewPortSize;
uniform vec2 texFrame;
uniform vec2 texOnePixel;
uniform int tex_id;
uniform sampler2D texture0;
void main()
{
    int odd = tex_id%2;
    vec2 texOffset = vec2((texFrame.x+texOnePixel.x*2)/2* tex_id,0);
    vec2 texCo = texCoord * (texFrame-texOnePixel*2) + texOffset+texOnePixel;
    texCo = vec2(texCo.x, odd*(1-texCo.y) + (1-odd)*texCo.y);//flip if odd numbered texture

    //float normalizedSize = size/2;
    vec2 normalizedCoordPoint = gl_FragCoord.xy/viewPortSize;
    FragColor = texture(texture0, texCo); // vec4(0, texCo.x, texCo.y,1); 
}
