////////////////////////////////////////////////////////////////////////////////
// Filename: pbr.vs
////////////////////////////////////////////////////////////////////////////////
#version 400


/////////////////////
// INPUT VARIABLES //
/////////////////////
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec3 inputTangent;
in vec3 inputBinormal;


//////////////////////
// OUTPUT VARIABLES //
//////////////////////
out vec3 fragPosition;
out vec2 fragTexCoord;
out vec3 fragNormal;
out vec3 tangent;
out vec3 binormal;
out vec3 viewDirection;


///////////////////////
// UNIFORM VARIABLES //
///////////////////////
uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 matView;

uniform vec3 viewPos;


////////////////////////////////////////////////////////////////////////////////
// Vertex Shader
////////////////////////////////////////////////////////////////////////////////
void main(void)
{
    fragPosition = vec3(matModel*vec4(vertexPosition, 1.0));

    // Calculate the position of the vertex against the world, view, and projection matrices.
	gl_Position = mvp*vec4(vertexPosition, 1.0);

    // Store the texture coordinates for the pixel shader.
    fragTexCoord = vertexTexCoord;

    // Calculate the normal vector against the world matrix only and then normalize the final value.
    fragNormal = vertexNormal * mat3(matModel);
    fragNormal = normalize(fragNormal);

    // Calculate the tangent vector against the world matrix only and then normalize the final value.
    tangent = inputTangent * mat3(matModel);
    tangent = normalize(tangent);

    // Calculate the binormal vector against the world matrix only and then normalize the final value.
    binormal = inputBinormal * mat3(matModel);
    binormal = normalize(binormal);

    // Calculate the position of the vertex in the world.
	vec4 worldPosition;
	worldPosition = vec4(vertexPosition, 1.0f) * matModel;

    // Determine the viewing direction based on the position of the camera and the position of the vertex in the world.
    viewDirection = viewPos - worldPosition.xyz;

    // Normalize the viewing direction vector.
    viewDirection = normalize(viewDirection);
}
