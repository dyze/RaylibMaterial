////////////////////////////////////////////////////////////////////////////////
// Filename: pbr.ps
////////////////////////////////////////////////////////////////////////////////
#version 400


/////////////////////
// INPUT VARIABLES //
/////////////////////
in vec3 fragPosition;
in vec2 fragTexCoord;
in vec3 fragNormal;
in vec3 tangent;
in vec3 binormal;
in vec3 viewDirection;


//////////////////////
// OUTPUT VARIABLES //
//////////////////////
out vec4 finalColor;

//////////////////////
// UNIFORM VARIABLES //
///////////////////////
uniform sampler2D texture0;
uniform sampler2D texture2;
uniform sampler2D texture3;
uniform vec3 lightDirection;


////////////////////////////////////////////////////////////////////////////////
// Pixel Shader
////////////////////////////////////////////////////////////////////////////////
void main(void)
{
    vec3 lightDir;
    vec3 albedo, rmColor, bumpMap;
    vec3 bumpNormal;
    float roughness, metallic;
    vec3 F0;
    vec3 halfDirection;
    float NdotH, NdotV, NdotL, HdotV;
    float roughnessSqr, roughSqr2, NdotHSqr, denominator, normalDistribution;
    float smithL, smithV, geometricShadow;
    vec3 fresnel;
    vec3 specularity;
    vec4 color;


    // Invert the light direction for calculations.
    lightDir = -lightDirection;

    // Sample the textures.
    albedo = texture(texture0, fragTexCoord).rgb;
    rmColor = texture(texture3, fragTexCoord).rgb;
    bumpMap = texture(texture2, fragTexCoord).rgb;

    // Calculate the normal using the normal map.
    bumpMap = (bumpMap * 2.0f) - 1.0f;
    bumpNormal = (bumpMap.x * tangent) + (bumpMap.y * binormal) + (bumpMap.z * fragNormal);
    bumpNormal = normalize(bumpNormal);

    // Get the metalic and roughness from the roughness/metalness texture.
    roughness = rmColor.r;
    metallic = rmColor.b;

    // Surface reflection at zero degress. Combine with albedo based on metal. Needed for fresnel calculation.
    F0 = vec3(0.04f, 0.04f, 0.04f);
    F0 = mix(F0, albedo, metallic);
    
    // Setup the vectors needed for lighting calculations.
    halfDirection = normalize(viewDirection + lightDir); 
    NdotH = max(0.0f, dot(bumpNormal, halfDirection));
    NdotV = max(0.0f, dot(bumpNormal, viewDirection));
    NdotL = max(0.0f, dot(bumpNormal, lightDir));
    HdotV = max(0.0f, dot(halfDirection, viewDirection));

    // GGX normal distribution calculation.
    roughnessSqr = roughness * roughness;
    roughSqr2 = roughnessSqr * roughnessSqr;
    NdotHSqr = NdotH * NdotH;
    denominator = (NdotHSqr * (roughSqr2 - 1.0f) + 1.0f);
    denominator = 3.14159265359f * (denominator * denominator);
    normalDistribution = roughSqr2 / denominator;

    // Schlick geometric shadow calculation.
    smithL = NdotL / (NdotL * (1.0f - roughnessSqr) + roughnessSqr);
    smithV = NdotV / (NdotV * (1.0f - roughnessSqr) + roughnessSqr);
    geometricShadow = smithL * smithV;

    // Fresnel shlick approximation for fresnel term calculation.
    fresnel = F0 + (1.0f - F0) * pow(1.0f - HdotV, 5.0f);

    // Now calculate the bidirectional reflectance distribution function.
    specularity = (normalDistribution * fresnel * geometricShadow) / ((4.0f * (NdotL * NdotV)) + 0.00001f);

    // Final light equation.
    color.rgb = albedo + specularity;
NdotL=0.7;
    color.rgb = color.rgb * NdotL;

    // Set the alpha to 1.0f.
      color = vec4(color.rgb, 1.0f);

      finalColor = color;
}
