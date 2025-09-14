#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;

// Input uniform values
uniform sampler2D texture0;
uniform sampler2D texture1;
uniform float ratio;

// Output fragment color
out vec4 finalColor;

// NOTE: Add here your custom variables

void main()
{
    // Texel color fetching from texture sampler
    vec4 tex0Color = texture(texture0, fragTexCoord);
	vec4 tex1Color = texture(texture1, fragTexCoord);

    finalColor = mix(tex0Color, tex1Color, ratio);
}

