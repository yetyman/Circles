#version 330 core
in vec2 texCoord;
uniform sampler2D opacityMap;
uniform vec4 windowBounds;
uniform vec2 minmax;

uniform vec4 LayerColor;

out vec4 FragColor;

void main()
{
	//modify this to use a min and max, calculated so far this frame in other shaders. 
	//this frag shader will be called multiple times for different portions of the screen
	
	vec4 clr = vec4(0,0,0,0);
	if(texCoord.x > windowBounds.x && texCoord.x < (windowBounds.x+windowBounds.z) && texCoord.y > windowBounds.y && texCoord.y < (windowBounds.y+windowBounds.w)){
		vec2 sampleCoord = (texCoord-windowBounds.xy)/(windowBounds.zw);
		clr = (texture(opacityMap, sampleCoord)-vec4(minmax.x,minmax.x,minmax.x,minmax.x))/(minmax.y-minmax.x);
	}
	FragColor = clr;
}