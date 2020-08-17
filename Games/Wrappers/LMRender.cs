using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.OpenGL;
using OpenTK;

namespace NextLevelLibrary
{
    public class LMRender : ModelRenderer
    {
        public LMRender(STGenericModel model) : base(model)
        {
        }

        public override void PrepareShaders()
        {
            if (ShaderProgram != null)
                return;

            ShaderProgram = new ShaderProgram(
                new VertexShader(VertexShaderBasic),
                new FragmentShader(FragmentShaderBasic));

            PrepareDebugShaders();
        }


        private static string FragmentShaderBasic = @"
            #version 330

            uniform vec4 highlight_color;

            uniform vec4 diffuseColor;

            //Samplers
            uniform sampler2D tex_Diffuse;
            uniform sampler2D tex_ShadowMap;

            uniform int hasDiffuse;
            uniform int hasShadowMap;
            uniform int renderVertColor;

            uniform int isAmbientMap;

            in vec2 f_texcoord0;
            in vec2 f_texcoord1;
            in vec3 fragPosition;

            in vec4 vertexColor;
            in vec3 normal;
            in vec3 boneWeightsColored;
            in vec4 tangent;
            in vec4 binormal;

            out vec4 FragColor;

            void main(){
                vec3 displayNormal = (normal.xyz * 0.5) + 0.5;
                float hc_a   = highlight_color.w;

                vec4 color = vec4(0.8f);
                if (hasDiffuse == 1)
                    color = texture(tex_Diffuse,f_texcoord0);
                if (isAmbientMap == 1)
                {
                    float diffuse = texture(tex_Diffuse,f_texcoord0).r;
                    float specular = texture(tex_Diffuse,f_texcoord0).g;
                    float ambient = texture(tex_Diffuse,f_texcoord0).b;
                    color.rgb = vec3(diffuse) * ambient;
                }

                color *= diffuseColor;

                float halfLambert = max(displayNormal.y,0.5);
                vec4 colorComb = vec4(color.rgb * (1-hc_a) + highlight_color.rgb * hc_a, color.a);

	            vec3 lightDir = vec3(0, 0, 1);
	            float light = 0.6 + abs(dot(normal, lightDir)) * 0.8;

                if (hasShadowMap == 1)
                     colorComb.rgb = colorComb.rgb *  texture(tex_ShadowMap,f_texcoord1).rrr;

                FragColor = vec4(colorComb.rgb * light, colorComb.a);

                if (renderVertColor == 1)
                    FragColor *= min(vertexColor, vec4(1));

                FragColor.rgb *= min(boneWeightsColored, vec3(1));
         }";

        private static string VertexShaderBasic = @"
            #version 330

            layout(location = 0) in vec3 vPosition;
            layout(location = 1) in vec3 vNormal;
            layout(location = 2) in vec2 vTexCoord;
            layout(location = 3) in vec4 vColor;
            layout(location = 4) in vec4 vBone;
            layout(location = 5) in vec4 vWeight;
            layout(location = 6) in vec4 vTangent;
            layout(location = 7) in vec4 vBinormal;
            layout(location = 8) in vec2 vTexCoord1;

            uniform mat4 mtxMdl;
            uniform mat4 mtxCam;

            // Skinning uniforms
            uniform mat4 bones[230];

            // Bone Weight Display
            uniform sampler2D weightRamp1;
            uniform sampler2D weightRamp2;
            uniform int selectedBoneIndex;
            uniform int debugOption;

            uniform int RigidSkinning;
            uniform int SingleBoneIndex;
            uniform int NoSkinning;
            uniform int HasSkeleton;

            out vec2 f_texcoord0;
            out vec2 f_texcoord1;

            out vec4 vertexColor;
            out vec3 normal;
            out vec3 boneWeightsColored;
            out vec4 tangent;
            out vec4 binormal;

            vec4 skin(vec3 pos, ivec4 index)
            {
                vec4 newPosition = vec4(pos.xyz, 1.0);

                newPosition = bones[index.x] * vec4(pos, 1.0) * vWeight.x;
                newPosition += bones[index.y] * vec4(pos, 1.0) * vWeight.y;
                newPosition += bones[index.z] * vec4(pos, 1.0) * vWeight.z;
                if (vWeight.w < 1) //Necessary. Bones may scale weirdly without
		            newPosition += bones[index.w] * vec4(pos, 1.0) * vWeight.w;

                return newPosition;
            }

            vec3 skinNRM(vec3 nr, ivec4 index)
            {
                vec3 newNormal = vec3(0);

	            newNormal =  mat3(bones[index.x]) * nr * vWeight.x;
	            newNormal += mat3(bones[index.y]) * nr * vWeight.y;
	            newNormal += mat3(bones[index.z]) * nr * vWeight.z;
	            newNormal += mat3(bones[index.w]) * nr * vWeight.w;

                return newNormal;
            }

            vec3 BoneWeightColor(float weights)
            {
	            float rampInputLuminance = weights;
	            rampInputLuminance = clamp((rampInputLuminance), 0.001, 0.999);
                if (debugOption == 1) // Greyscale
                    return vec3(weights);
                else if (debugOption == 2) // Color 1
	               return texture(weightRamp1, vec2(1 - rampInputLuminance, 0.50)).rgb;
                else // Color 2
                    return texture(weightRamp2, vec2(1 - rampInputLuminance, 0.50)).rgb;
            }

            float BoneWeightDisplay(ivec4 index)
            {
                float weight = 0;
                if (selectedBoneIndex == index.x)
                    weight += vWeight.x;
                if (selectedBoneIndex == index.y)
                    weight += vWeight.y;
                if (selectedBoneIndex == index.z)
                    weight += vWeight.z;
                if (selectedBoneIndex == index.w)
                    weight += vWeight.w;

                if (selectedBoneIndex == index.x && RigidSkinning == 1)
                    weight = 1;
               if (selectedBoneIndex == SingleBoneIndex && NoSkinning == 1)
                    weight = 1;

                return weight;
            }

            void main(){
                f_texcoord0 = vTexCoord;
                f_texcoord1 = vTexCoord1;
                vertexColor = vColor;
                normal = vNormal;

                ivec4 index = ivec4(vBone);
                normal = vNormal;
                tangent = vTangent;
                binormal = vBinormal;

                vec4 objPos = mtxMdl * vec4(vPosition.xyz, 1.0);
	            if (vBone.x != -1.0 && HasSkeleton == 1)
		            objPos = skin(objPos.xyz, index);
	            if(vBone.x != -1.0 && HasSkeleton == 1)
		            normal = normalize((skinNRM(vNormal.xyz, index)).xyz);

                gl_Position = mtxCam*objPos;

                float totalWeight = BoneWeightDisplay(index);
                boneWeightsColored = BoneWeightColor(totalWeight).rgb;
            }";


        public override void SetMaterialUniforms(ShaderProgram shader, STGenericMaterial material, STGenericMesh mesh) {
            var mat = (LMMaterial)material;
            if (mat == null) return;

            shader.SetBool("isAmbientMap", mat.IsAmbientMap);

            base.SetMaterialUniforms(shader, material, mesh);
        }

        public override void RenderMaterials(ShaderProgram shader,
      STGenericMesh mesh, STPolygonGroup group, STGenericMaterial material, Vector4 highlight_color)
        {
            shader.SetVector4("highlight_color", highlight_color);
            shader.SetBoolToInt("hasShadowMap", false);

            SetTextureUniforms(shader);
            SetMaterialUniforms(shader, material, mesh);
            if (material == null) return;

            int textureUintID = 1;
            foreach (var textureMap in material.TextureMaps)
            {
                var tex = textureMap.GetTexture();
                if (textureMap.Type == STTextureType.Diffuse)
                {
                    shader.SetBoolToInt("hasDiffuse", true);
                    BindTexture(shader, Runtime.TextureCache, textureMap, textureUintID);
                    shader.SetInt($"tex_Diffuse", textureUintID);
                }
                if (textureMap.Type == STTextureType.Shadow)
                {
                    shader.SetBoolToInt("hasShadowMap", true);
                    BindTexture(shader, Runtime.TextureCache, textureMap, textureUintID);
                    shader.SetInt($"tex_ShadowMap", textureUintID);
                }

                textureUintID++;
            }
        }
    }
}
