//
// Copyright (c) 2009 Joseph A. Zupko
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 

// #define DISABLE_MESH_COMBINATION
// #define ENABLE_SOPHISTICATED_MESH_COMBINE_METRIC

using KeyFrames = System.Collections.Generic.Dictionary<siat.pipeline.collada.elements.ColladaAnimation, System.Collections.Generic.List<siat.AnimationKeyFrame>>;

using EffectsBySymbol = System.Collections.Generic.Dictionary<string, siat.pipeline.SiatEffectContent>;
using MaterialsBySymbol = System.Collections.Generic.Dictionary<string, siat.pipeline.SiatMaterialContent>;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using siat.pipeline.collada.elements;
using siat.pipeline.collada.elements.fx;
using System.ComponentModel;
namespace siat.pipeline.collada
{
    [ContentProcessor(DisplayName = "Siat XNA COLLADA Processor")]
    public sealed class ColladaProcessor : ContentProcessor<ColladaCOLLADA, SceneContent>
    {
        /// <summary>
        /// Maximum number of skinning matrices.
        /// </summary>
        /// <seealso cref="siat.scene.AnimatedMeshPartNode"/>
        public const uint kSkinningMatricesCount = 72;
        public const uint kSkinningMatricesSize = kSkinningMatricesCount * 3;

        /// <summary>
        /// Maximum number of joints that can influence a single vertex.
        /// </summary>
        public const uint kJointInfluencesPerVertex = 4;

        public const float kLogBase = 1.7f;
        public const float kTooSmallCount = 64.0f;
        public const float kIntersectionCost = 1.0f;
        public const float kLocalizationCost = 1.0f;

        #region Constants
        public const string kMeshPartPostfix = "_part";

        public const string kColladaExtension = ".dae";

        public const string kMayaProfile = "MAYA";
        public const string kMayaDynamicAttributes = "dynamic_attributes";
        public const string kMayaDynamicAttributeBooleanType = "bool";
        public const string kMayaDynamicAttributeStringType = "string";
        public const string kMayaDynamicAttributeTypeAttribute = "type";
        public const string kMayaPortalToAttribute = "PortalTo";
        public const string kMayaSkyAttribute = "IsSky";

        public const string kColorKeyColorParameter = "ColorKeyColor";
        public const string kColorKeyEnabledParameter = "ColorKeyEnabled";
        public const string kGenerateMipmapsParameter = "GenerateMipmaps";
        public const string kResizeToPowerOfTwoParameter = "ResizeToPowerOfTwo";
        public const string kTextureFormatParameter = "TextureFormat";

        public const string kExtraHLSLName = "import";
        public const string kExtraHLSLType = "import";
        public const string kImportHLSLProfileAttribute = "profile";
        public const string kImportHLSLProfileValue = "fx";
        public const string kImportHLSLUrlAttribute = "url";
        public const string kImportHLSLCompilerOptionsAttribute = "compiler_options";
        public const string kXnaTextureId = "Texture";
        public const string kStandardEffectFile = "..\\..\\siat_xna\\siat_xna_cp\\impl\\collada_effect.h";

        public const string kColorPostfix = "_COLOR";
        public const string kTexturePostfix = "_TEXTURE";
        public const string kTexcoordsPostfix = "_TEXCOORDS";
        public const string kAddressUPostfix = "_ADDRESSU";
        public const string kAddressVPostfix = "_ADDRESSV";
        public const string kAddressWPostfix = "_ADDRESSW";
        public const string kMinFilterPostfix = "_MIN_FILTER";
        public const string kMagFilterPostfix = "_MAG_FILTER";
        public const string kMipFilterPostfix = "_MIP_FILTER";
        public const string kBorderColorPostfix = "_BORDER_COLOR";
        public const string kMaxMipLevelPostfix = "_MAX_MIP_LEVEL";
        public const string kMipMapLodBias = "_MIP_MAP_LOD_BIAS";

        public const string kAmbientPrefix = "AMBIENT";
        public const string kDiffusePrefix = "DIFFUSE";
        public const string kEmissionPrefix = "EMISSION";
        public const string kReflectivePrefix = "REFLECTIVE";
        public const string kSpecularPrefix = "SPECULAR";
        public const string kTransparentPrefix = "TRANSPARENT";
        public const string kBumpPrefix = "BUMP";

        public const string kReflectivity = "REFLECTIVITY";
        public const string kShininess = "SHININESS";
        public const string kTransparency = "TRANSPARENCY";

        public const string kAlphaOne = "ALPHA_ONE";
        public const string kRgbZero = "RGB_ZERO";

        public const string kAnimated = "ANIMATED";
        public const string kBlinn = "BLINN";
        public const string kPhong = "PHONG";

        public const string kTexcoordsInput = "Texcoords";

        public const string kColorBlack = " 0, 0, 0, 1 ";
        public const string kTexcoordsChannelCount = "TEXCOORDS_COUNT";

        public const string kEmissionSemanticPrefix = "siat_Emission";
        public const string kReflectiveSemanticPrefix = "siat_Reflective";
        public const string kTransparentSemanticPrefix = "siat_Transparent";
        public const string kAmbientSemanticPrefix = "siat_Ambient";
        public const string kDiffuseSemanticPrefix = "siat_Diffuse";
        public const string kSpecularSemanticPrefix = "siat_Specular";
        public const string kBumpSemanticPrefix = "siat_Bump";

        public const string kReflectivitySemantic = "siat_Reflectivity";
        public const string kTransparencySemantic = "siat_Transparency";
        public const string kShininessSemantic = "siat_Shininess";

        public const string kColorSemanticPostfix = "Color";
        public const string kTextureSemanticPostfix = "Texture";

        public const float kBlackTolerance = 0.05f;
        #endregion

        #region Private members
        private WeakRefContainer<string, JointSceneNodeContent> msJoints = new WeakRefContainer<string, JointSceneNodeContent>(string.Empty);

        private bool mbProcessPhysics = false;
        private string mBaseName = string.Empty;
        private ColladaContent mContent;
        private ContentProcessorContext mContext = null;
        private Dictionary<SiatEffectContent, SiatEffectContent> mEffects = new Dictionary<SiatEffectContent, SiatEffectContent>();
        private Matrix mInverseUpAxisTransform = Matrix.Identity;
        private Dictionary<SiatMaterialContent, SiatMaterialContent> mMaterials = new Dictionary<SiatMaterialContent, SiatMaterialContent>();
        private _ColladaElement.Enums.SamplerFilter mMinFilterWhenNone = _ColladaElement.Enums.SamplerFilter.LinearMipmapLinear;
        private _ColladaElement.Enums.SamplerFilter mMagFilterWhenNone = _ColladaElement.Enums.SamplerFilter.Linear;
        private _ColladaElement.Enums.SamplerFilter mMipFilterWhenNone = _ColladaElement.Enums.SamplerFilter.Linear;
        private SceneContent mScene = new SceneContent();
        private Dictionary<string, ExternalReference<TextureContent>> mTextureCache = new Dictionary<string, ExternalReference<TextureContent>>();
        private uint mTotalTexcoordChannels = 0;
        private Matrix mUpAxisTransform = Matrix.Identity;
        private Dictionary<VertexElement[], VertexElement[]> mVertexDeclarations = new Dictionary<VertexElement[], VertexElement[]>(new PipelineUtilities.VertexDeclarationComparer());
        private static readonly OpaqueDataDictionary mskTextureBuildParameters = new OpaqueDataDictionary();

        static ColladaProcessor()
        {
            mskTextureBuildParameters.Add(kColorKeyEnabledParameter, false);
            mskTextureBuildParameters.Add(kGenerateMipmapsParameter, true);
            mskTextureBuildParameters.Add(kResizeToPowerOfTwoParameter, true);
            mskTextureBuildParameters.Add(kTextureFormatParameter, TextureProcessorOutputFormat.DxtCompressed);
        }

        #region Controller processing
        private struct IwEntry : IComparable<IwEntry>
        {
            public IwEntry(float aIndex, float aWeight)
            {
                Index = aIndex;
                Weight = aWeight;
            }

            public float Index;
            public float Weight;

            // Intentionally sort in descending order.
            public int CompareTo(IwEntry b)
            {
                if (Weight < b.Weight)
                {
                    return 1;
                }
                else if (Weight > b.Weight)
                {
                    return -1;
                }
                else
                {
                    return Index.CompareTo(b.Index);
                }
            }
        };

        private struct JointEntry : IComparable<JointEntry>
        {
            public JointEntry(float aIndex)
            {
                Index = aIndex;
                MaxWeight = 0.0f;
                bInUse = false;
                bExclusiveToVertex = false;
            }

            public float MaxWeight;
            public bool bExclusiveToVertex;
            public bool bInUse;
            public float Index;

            public int CompareTo(JointEntry b)
            {
                if (!bInUse && b.bInUse)
                {
                    return -1;
                }
                else if (bInUse && !b.bInUse)
                {
                    return 1;
                }
                else
                {
                    return MaxWeight.CompareTo(b.MaxWeight);
                }
            }
        }

        private int[] _GetBlendIndicesAndWeights(ColladaSkin aSkin, out float[] arIndices, out float[] arWeights)
        {
            ColladaVertexWeights vertexWeights = aSkin.GetFirst<ColladaVertexWeights>();
            ColladaVcount vcount = vertexWeights.GetFirst<ColladaVcount>();
            ColladaPrimitives v = vertexWeights.GetFirst<ColladaPrimitives>();

            arIndices = new float[vcount.Count * kJointInfluencesPerVertex];
            arWeights = new float[vcount.Count * kJointInfluencesPerVertex];

            ColladaInputGroupB input = vertexWeights.GetFirst<ColladaInputGroupB>(
               delegate(ColladaInputGroupB e)
               {
                   return (e.Semantic == _ColladaElement.Enums.InputSemantic.kWeight);
               });

            ColladaSource source = (ColladaSource)input.Source;
            ColladaFloatArray array = source.GetFirst<ColladaFloatArray>();

            uint outIndex = 0;
            uint vCountIndex = 0;
            uint vIndex = 0;
            uint vSize = v.Count;

            List<IwEntry> buf = new List<IwEntry>();

            bool bLoggedZeroCountWarning = false;
            bool bLoggedInfluenceWarning = false;

            JointEntry[] jointsUsed = new JointEntry[0];

            while (vIndex < vSize)
            {
                #region Get the number of influences for this vertex.
                uint vCount = vcount[vCountIndex++];
                #endregion

                #region Gather and sort the indices and weights for each influence by weight.
                buf.Clear();
                for (int i = 0; i < vCount; i++)
                {
                    IwEntry entry = new IwEntry(v[vIndex++], array[(uint)v[vIndex++]]);
                    if (entry.Weight > 0.0f) buf.Add(entry);
                }
                buf.Sort();
                vCount = (uint)buf.Count;

                if (vCount < 1)
                {
                    if (mContent != null && !bLoggedZeroCountWarning)
                    {
                        mContext.Logger.LogImportantMessage("A vertex of input \"" +
                            source.Id + "\" has no influences.");
                        bLoggedZeroCountWarning = true;
                    }
                }

                if (vCount > kJointInfluencesPerVertex)
                {
                    if (mContext != null && !bLoggedInfluenceWarning)
                    {
                        mContext.Logger.LogImportantMessage("Siat XNA supports only " +
                            kJointInfluencesPerVertex.ToString() + " bones per vertex. A vertex of input \"" +
                            source.Id + "\" is influenced by " + vCount + " bones. The highest weight bones " +
                            "will be used and the others will be ignored.");
                        bLoggedInfluenceWarning = true;
                    }
                }
                #endregion

                #region Populate the output weight and indice buffers with applicable inputs.
                uint count = 0;
                for (int i = 0; i < vCount && count < kJointInfluencesPerVertex; i++)
                {
                    float index = buf[i].Index;
                    float weight = buf[i].Weight;

                    if (index < 0.0f)
                    {
                        throw new Exception("Siat XNA does not support the bind matrix in bone weights. " +
                            "A vertex of input \"" + source.Id + "\" refers to the bind matrix.");
                    }
                    else
                    {
                        int intIndex = (int)index;
                        if (jointsUsed.Length <= intIndex)
                        {
                            int oldLength = jointsUsed.Length;
                            Array.Resize(ref jointsUsed, intIndex + 1);
                            for (int j = oldLength; j < jointsUsed.Length; j++)
                            {
                                jointsUsed[j] = new JointEntry(j);
                            }
                        }
                        else
                        {
                            jointsUsed[intIndex].Index = index;
                        }

                        jointsUsed[intIndex].MaxWeight = Utilities.Max(weight, jointsUsed[intIndex].MaxWeight);
                        jointsUsed[intIndex].bInUse = true;
                        jointsUsed[intIndex].bExclusiveToVertex = (vCount == 1);

                        arIndices[outIndex] = index;
                        arWeights[outIndex++] = weight;
                        count++;
                    }
                }
                outIndex += (kJointInfluencesPerVertex - count);
                #endregion

                #region Normalize the last four weights
                float sum = 0.0f;
                for (uint i = outIndex - kJointInfluencesPerVertex; i < outIndex - 1; i++)
                {
                    sum += arWeights[i];
                }

                if (!Utilities.AboutZero(sum))
                {
                    arWeights[outIndex - 1] = (1.0f - sum);
                }
                #endregion
            }

            #region Adjust the joint references to fit this skeleton in kSkinningMatricsCount
            if (jointsUsed.Length > kSkinningMatricesCount)
            {
                Array.Sort(jointsUsed);

                uint count = ((uint)jointsUsed.Length - kSkinningMatricesCount);
                int[] toRemove = new int[count];
                uint index = 0;
                for (int i = 0; i < jointsUsed.Length && index < count; i++)
                {
                    if (!jointsUsed[i].bInUse || !jointsUsed[i].bExclusiveToVertex)
                    {
                        toRemove[index++] = (int)jointsUsed[i].Index;
                    }
                }

                if (index < count)
                {
                    throw new Exception("Joints of input \"" + source.Id + "\" could not be fitted into " +
                        "the max joint count of " + kSkinningMatricesCount.ToString());
                }

                for (int i = 0; i < arIndices.Length; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        if (arIndices[i] == (float)toRemove[j])
                        {
                            arIndices[i] = 0.0f;
                            arWeights[i] = 0.0f;
                        }
                        else if (arIndices[i] > toRemove[j])
                        {
                            arIndices[i] = arIndices[i] - 1.0f;
                        }
                    }
                }

                return toRemove;
            }
            else
            {
                return new int[0];
            }
            #endregion
        }

        private void _ProcessInstanceController(ColladaNode aNode, int aChildrenCount, ref Matrix aLocalTransform)
        {
            ColladaInstanceController instanceController = aNode.GetFirst<ColladaInstanceController>();
            ColladaController controller = instanceController.Instance;
            ColladaSkin skin = (ColladaSkin)controller.FirstChild;

            float[] indices;
            float[] weights;
            int[] toRemove = _GetBlendIndicesAndWeights(skin, out indices, out weights);

            SiatMeshContent mesh;
            MaterialsBySymbol materials;
            EffectsBySymbol effects;
            _GetMeshAndMaterials(aNode, out mesh, out materials, out effects, indices, weights);

            Matrix bindMatrix = mInverseUpAxisTransform * skin.XnaBindShapeTransform * mUpAxisTransform;
            Matrix[] invBindTransforms = skin.InverseBindTransforms;
            int invBindCount = invBindTransforms.Length;
            for (int i = 0; i < invBindCount; i++)
            {
                invBindTransforms[i] = mInverseUpAxisTransform * invBindTransforms[i] * mUpAxisTransform;
            }

            _ColladaArray<object> colladaJoints = skin.Joints;
            string rootJoint = mBaseName + ((ColladaNode)instanceController.SkeletonRoot).Id;

            uint jointCount = colladaJoints.Count;
            string[] joints = new string[jointCount];
            for (uint i = 0; i < jointCount; i++)
            {
                ColladaNode node = (ColladaNode)colladaJoints[i];
                joints[i] = mBaseName + node.Id;
            }

            #region Remove any joints and inv bind transforms for those joints that are not going to be used.
            if (invBindTransforms.Length != joints.Length)
            {
                throw new Exception("The number of inverse bind transforms and joints for controller node \"" +
                    aNode.Id + "\" is not the same.");
            }

            Array.Sort(toRemove);
            for (int i = 0; i < toRemove.Length; i++)
            {
                Utilities.Remove(ref invBindTransforms, toRemove[i] - i);
                Utilities.Remove(ref joints, toRemove[i] - i);
            }

            // This can happen if the number of joints was larger than the number of joints influencing vertices.
            if (invBindTransforms.Length > kSkinningMatricesCount)
            {
                Array.Resize(ref invBindTransforms, (int)kSkinningMatricesCount);
                Array.Resize(ref joints, (int)kSkinningMatricesCount);
            }
            #endregion

            MeshSceneNodeContent meshNode = new MeshSceneNodeContent(
                mBaseName + aNode.Id, aChildrenCount + mesh.Parts.Count, ref aLocalTransform,
                mesh);
            mScene.Nodes.Add(meshNode);

            int count = 0;
            foreach (SiatMeshContent.Part e in mesh.Parts)
            {
                AnimatedMeshPartSceneNodeContent meshPartNode = new
                    AnimatedMeshPartSceneNodeContent(mBaseName + aNode.Id + kMeshPartPostfix + count.ToString(),
                    0, ref Utilities.kIdentity, effects[e.Effect],
                    materials[e.Effect], e, ref bindMatrix, invBindTransforms, rootJoint, joints);

                mScene.Nodes.Add(meshPartNode);
                count++;
            }
        }
        #endregion

        #region Joint processing
        private int _ProcessJointHelper(List<AnimationKeyFrame> aKeyFrames, AnimationKeyFrame aNewFrame)
        {
            int count = aKeyFrames.Count;
            for (int i = 0; i < count; i++)
            {
                if (Utilities.AboutEqual(aKeyFrames[i].Time, aNewFrame.Time, Utilities.kLooseToleranceFloat))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Parses animation into a joint.
        /// </summary>
        /// <param name="aNode"></param>
        /// <param name="childrenCount"></param>
        /// <param name="aLocalTransform"></param>
        /// <returns></returns>
        /// <remarks>
        /// COLLADA's animation specification is poorly defined with regards to clustering
        /// of animations. There is no standard for grouping, for example, a set of animations
        /// into a "jump" animation. This function currently guesses and may have to be changed
        /// in the long run.
        /// </remarks>
        private void _ProcessJoint(ColladaNode aNode, int childrenCount, ref Matrix aLocalTransform)
        {
            // TODO: this function will not handle oddly mixed animation channels properly.
            string animationId = mBaseName + aNode.Id + "_animation";

            #region Validate key frames
            uint keyFramesCount = 0;
            foreach (_ColladaTransformElement e in aNode.GetEnumerable<_ColladaTransformElement>())
            {
                uint currentCount = (uint)e.GetKeyFrames(0).Length;
                if (keyFramesCount == 0)
                {
                    keyFramesCount = currentCount;
                }
                else if (currentCount != 0 && keyFramesCount != currentCount)
                {
                    throw new Exception("<node> \"" + aNode.Id + "\" has transform elements with " +
                        "different numbers of key frames.");
                }
            }
            #endregion

            #region Initialize key frames
            AnimationKeyFrame[] keyFrames = new AnimationKeyFrame[keyFramesCount];
            for (uint i = 0; i < keyFramesCount; i++)
            {
                keyFrames[i] = new AnimationKeyFrame(0.0f, Matrix.Identity);
            }
            #endregion

            #region Combine key frames into one channel of matrix key frames and create node.
            if (keyFramesCount > 0)
            {
                foreach (_ColladaTransformElement e in aNode.GetEnumerable<_ColladaTransformElement>())
                {
                    AnimationKeyFrame[] newFrames = e.GetKeyFrames(0);

                    if (newFrames.Length == 0)
                    {
                        for (uint i = 0; i < keyFramesCount; i++)
                        {
                            keyFrames[i] = new AnimationKeyFrame(keyFrames[i].Time,
                                e.XnaMatrix * keyFrames[i].Key);
                        }
                    }
                    else
                    {
                        for (uint i = 0; i < keyFramesCount; i++)
                        {
                            keyFrames[i] = new AnimationKeyFrame(newFrames[i].Time,
                                newFrames[i].Key * keyFrames[i].Key);
                        }
                    }
                }

                Array.Sort(keyFrames, delegate(AnimationKeyFrame a, AnimationKeyFrame b)
                {
                    if (Utilities.LessThan(a.Time, b.Time))
                    {
                        return -1;
                    }
                    else if (Utilities.GreaterThan(a.Time, b.Time))
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                });
            }
            else
            {
                keyFramesCount = 1;
                keyFrames = new AnimationKeyFrame[keyFramesCount];
                keyFrames[0].Key = mInverseUpAxisTransform * aLocalTransform * mUpAxisTransform;
                keyFrames[0].Time = 0.0f;
            }

            for (int i = 0; i < keyFramesCount; i++)
            {
                keyFrames[i].Key = mInverseUpAxisTransform * keyFrames[i].Key * mUpAxisTransform;
            }

            JointSceneNodeContent jointNode = new JointSceneNodeContent(mBaseName + aNode.Id, childrenCount,
                ref aLocalTransform, new AnimationContent(animationId, keyFrames));

            msJoints.Add(jointNode.Id, jointNode);

            mScene.Nodes.Add(jointNode);
            #endregion
        }
        #endregion

        #region Material processing
        private void _GetMaterials(ColladaBindMaterial aBindMaterial, ref MaterialsBySymbol arMaterials, ref EffectsBySymbol arEffects, bool abAnimated)
        {
            ColladaTechniqueCommonOfBindMaterial techniqueCommon = aBindMaterial.GetFirst<ColladaTechniqueCommonOfBindMaterial>();

            foreach (ColladaInstanceMaterial instanceMaterial in techniqueCommon.GetEnumerable<ColladaInstanceMaterial>())
            {
                BoundEffect boundEffect = new BoundEffect(instanceMaterial);
                string effectId = mBaseName + instanceMaterial.Instance.Id;

                if (!mContent.Effects.ContainsKey(effectId))
                {
                    mContent.Effects[effectId] = new Dictionary<BoundEffect, SiatEffectContent>();
                }

                SiatEffectContent effect = null;
                SiatMaterialContent material = null;

                if (!mContent.Effects[effectId].ContainsKey(boundEffect))
                {
                    if (instanceMaterial.Instance.Effect.HasEffectHLSL)
                    {
                        _ProcessHLSLEffect(instanceMaterial.Instance, boundEffect, out effect, out material);
                    }
                    else
                    {
                        _ProcessProfileCOMMONEffect(instanceMaterial.Instance, boundEffect, out effect, out material, abAnimated);
                    }
                    mContent.Effects[effectId][boundEffect] = effect;
                    mContent.Materials[effectId] = material;
                }
                else
                {
                    effect = mContent.Effects[effectId][boundEffect];
                    material = mContent.Materials[effectId];
                }

                arMaterials[instanceMaterial.Symbol] = material;
                arEffects[instanceMaterial.Symbol] = effect;
            }
        }

        private bool _ProcessColor(_ColladaElement aColor, string aPrefix, List<CompilerMacro> aMacros, string aSemanticPrefix, SiatMaterialContent aMaterial)
        {
            if (aColor is ColladaColor)
            {
                ColladaColor color = (ColladaColor)aColor;
                Vector3 rgb = color.ColorRGB;
                Vector4 rgba = color.ColorRGBA;

                if (!Utilities.AboutZero(ref rgb, kBlackTolerance))
                {
                    aMacros.Add(PipelineUtilities.NewMacro(aPrefix + kColorPostfix, aSemanticPrefix + kColorSemanticPostfix));
                    aMaterial.Parameters.Add(new SiatMaterialContent.Parameter(aSemanticPrefix + kColorSemanticPostfix, ParameterType.kVector4, rgba));
                    return true;
                }
            }

            return false;
        }

        private void _ProcessHLSLEffect(ColladaMaterial aMaterial, BoundEffect aBoundEffect, out SiatEffectContent arEffect, out SiatMaterialContent arMaterial)
        {
            #region Effect
            ColladaEffect colladaEffect = aMaterial.Effect;
            string effectId = mBaseName + aMaterial.Id + aBoundEffect.ToString();

            CompiledEffect compiledEffect = Effect.CompileEffectFromFile(
                colladaEffect.EffectHLSLFilename, null, null,
                CompilerOptions.None, TargetPlatform.Windows);

            if (!compiledEffect.Success)
            {
                throw new Exception("Failed compiling HLSL effect \"" +
                    colladaEffect.EffectHLSLFilename + "\"." + Environment.NewLine +
                    compiledEffect.ErrorsAndWarnings);
            }
            string hashString = compiledEffect.ToString();
            uint hash = Hash.Calculate32(hashString, 0u);

            arEffect = new SiatEffectContent(hash, effectId);
            arEffect.CompiledEffect = compiledEffect;
            #endregion

            #region Material
            ColladaSetparamOfInstanceEffect[] prms = aMaterial.Params;
            arMaterial = new SiatMaterialContent();

            foreach (ColladaSetparamOfInstanceEffect p in prms)
            {
                ParameterType type;

                switch (p.Type)
                {
                    case _ColladaElement.Enums.CoreValueType.kFloat: type = ParameterType.kSingle; break;
                    case _ColladaElement.Enums.CoreValueType.kFloat4x4: type = ParameterType.kMatrix; break;
                    case _ColladaElement.Enums.CoreValueType.kSurface: type = ParameterType.kTexture; break;
                    case _ColladaElement.Enums.CoreValueType.kFloat2: type = ParameterType.kVector2; break;
                    case _ColladaElement.Enums.CoreValueType.kFloat3: type = ParameterType.kVector3; break;
                    case _ColladaElement.Enums.CoreValueType.kFloat4: type = ParameterType.kVector4; break;
                    default:
                        throw new Exception("Material parameter type \"" +
                            Enum.GetName(typeof(_ColladaElement.Enums.CoreValueType), p.Type) +
                            "\" is not yet supported.");
                }

                if (type == ParameterType.kTexture)
                {
                    ColladaImage image = p.GetValue<ColladaSurfaceOfProfileCOMMON>().Image;
                    if (!image.IsLocation || image.Location == string.Empty)
                    {
                        throw new Exception("<image> of texture as defined is not supported.");
                    }

                    string assetName = Utilities.RemoveExtension(image.Location);
                    string textureLocation = image.Location;
                    ExternalReference<TextureContent> textureReference = null;

                    if (!mTextureCache.TryGetValue(textureLocation, out textureReference))
                    {
                        textureReference = new ExternalReference<TextureContent>(textureLocation, mContent.Identity);
                        if (mContext != null)
                        {
                            textureReference = mContext.BuildAsset<TextureContent, TextureContent>(textureReference, typeof(TextureProcessor).Name, mskTextureBuildParameters, null, null);
                        }
                        mTextureCache[textureLocation] = textureReference;
                    }

                    arMaterial.Parameters.Add(new SiatMaterialContent.Parameter(p.Reference,
                        type, textureReference));
                }
                else
                {
                    arMaterial.Parameters.Add(new SiatMaterialContent.Parameter(p.Reference,
                        type, p.Value));
                }
            }
            #endregion
        }

        private void _ProcessSamplerSettings(collada.elements.fx.ColladaSamplerFX aSampler, string aPrefix, List<CompilerMacro> aMacros)
        {
            aMacros.Add(PipelineUtilities.NewMacro(aPrefix + kAddressUPostfix, PipelineUtilities.ColladaSurfaceWrapToHlsl(aSampler.WrapS)));
            aMacros.Add(PipelineUtilities.NewMacro(aPrefix + kAddressVPostfix, PipelineUtilities.ColladaSurfaceWrapToHlsl(aSampler.WrapT)));
            aMacros.Add(PipelineUtilities.NewMacro(aPrefix + kAddressWPostfix, PipelineUtilities.ColladaSurfaceWrapToHlsl(aSampler.WrapP)));
            aMacros.Add(PipelineUtilities.NewMacro(aPrefix + kMinFilterPostfix, PipelineUtilities.ColladaSurfaceFilterToHlsl(aSampler.Minfilter == _ColladaElement.Enums.SamplerFilter.None ? mMinFilterWhenNone : aSampler.Minfilter)));
            aMacros.Add(PipelineUtilities.NewMacro(aPrefix + kMagFilterPostfix, PipelineUtilities.ColladaSurfaceFilterToHlsl(aSampler.Magfilter == _ColladaElement.Enums.SamplerFilter.None ? mMagFilterWhenNone : aSampler.Magfilter)));
            aMacros.Add(PipelineUtilities.NewMacro(aPrefix + kMipFilterPostfix, PipelineUtilities.ColladaSurfaceFilterToHlsl(aSampler.Mipfilter == _ColladaElement.Enums.SamplerFilter.None ? mMipFilterWhenNone : aSampler.Mipfilter)));
            int borderColor = (aSampler.BorderColor > 0.0f) ? int.MaxValue : 0;
            aMacros.Add(PipelineUtilities.NewMacro(aPrefix +  kBorderColorPostfix, borderColor.ToString()));
            aMacros.Add(PipelineUtilities.NewMacro(aPrefix +  kMaxMipLevelPostfix, aSampler.MipmapMaxlevel.ToString()));
            aMacros.Add(PipelineUtilities.NewMacro(aPrefix +  kMipMapLodBias, aSampler.MipmapBias.ToString()));
        }

        private void _ProcessProfileCOMMONEffect(ColladaMaterial aMaterial, BoundEffect aBoundEffect, out SiatEffectContent arEffect, out SiatMaterialContent arMaterial, bool abAnimated)
        {
            List<CompilerMacro> macros = new List<CompilerMacro>();
            mTotalTexcoordChannels = 0;
            collada.elements.fx.ColladaEffectOfProfileCOMMON effect = aMaterial.Effect.EffectCOMMON;

            SiatMaterialContent retMaterial = new SiatMaterialContent();

            #region Bump map
            // can only be a texture.
            _ProcessTexture(aBoundEffect, effect.Bump, kBumpPrefix, macros, kBumpSemanticPrefix, retMaterial);
            #endregion

            #region Emission
            if (!_ProcessTexture(aBoundEffect, effect.Emission, kEmissionPrefix, macros, kEmissionSemanticPrefix, retMaterial))
            {
                _ProcessColor(effect.Emission, kEmissionPrefix, macros, kEmissionSemanticPrefix, retMaterial);
            }
            #endregion

            #region Reflectivity
            if (!Utilities.AboutZero(effect.Reflectivity, kBlackTolerance) &&
                !Utilities.AboutEqual(effect.Reflectivity, 1.0f, kBlackTolerance))
            {
                bool bReflectivity = true;

                if (!_ProcessTexture(aBoundEffect, effect.Reflective, kReflectivePrefix, macros, kReflectiveSemanticPrefix, retMaterial))
                {
                    if (!_ProcessColor(effect.Reflective, kReflectivePrefix, macros, kReflectiveSemanticPrefix, retMaterial))
                    {
                        bReflectivity = false;
                    }
                }

                if (bReflectivity)
                {
                    macros.Add(PipelineUtilities.NewMacro(kReflectivity, kReflectivitySemantic));
                    retMaterial.Parameters.Add(new SiatMaterialContent.Parameter(kReflectivitySemantic, ParameterType.kSingle, effect.Reflectivity));
                }
            }
            #endregion

            #region Transparency
            if (effect.TransparencyType == TransparencyTypes.AlphaOne)
            {
                if (!_ProcessTexture(aBoundEffect, effect.Transparent, kTransparentPrefix, macros, kTransparentSemanticPrefix, retMaterial))
                {
                    Vector4 color = ((ColladaColor)effect.Transparent).ColorRGBA;
                    float transparency = effect.Transparency;
                    float alpha = transparency * color.W;

                    if (!Utilities.AboutEqual(alpha, 1.0f, kBlackTolerance) &&
                        !Utilities.AboutZero(alpha, kBlackTolerance))
                    {
                        macros.Add(PipelineUtilities.NewMacro(kTransparentPrefix + kColorPostfix, kTransparentSemanticPrefix + kColorSemanticPostfix));
                        retMaterial.Parameters.Add(new SiatMaterialContent.Parameter(kTransparentSemanticPrefix + kColorSemanticPostfix, ParameterType.kVector4, color));
                        macros.Add(PipelineUtilities.NewMacro(kTransparency, kTransparencySemantic));
                        retMaterial.Parameters.Add(new SiatMaterialContent.Parameter(kTransparencySemantic, ParameterType.kSingle, effect.Transparency));
                        macros.Add(PipelineUtilities.NewMacro(kAlphaOne, "1"));
                    }
                }
                else
                {
                    macros.Add(PipelineUtilities.NewMacro(kTransparency, kTransparencySemantic));
                    retMaterial.Parameters.Add(new SiatMaterialContent.Parameter(kTransparencySemantic, ParameterType.kSingle, effect.Transparency));
                    macros.Add(PipelineUtilities.NewMacro(kAlphaOne, "1"));
                }
            }
            else
            {
                if (!_ProcessTexture(aBoundEffect, effect.Transparent, kTransparentPrefix, macros, kTransparentSemanticPrefix, retMaterial))
                {
                    Vector4 color = ((ColladaColor)effect.Transparent).ColorRGBA;
                    float luminance = Utilities.GetLuminance(ref color);
                    float transparency = effect.Transparency;

                    if (!Utilities.AboutZero(luminance, kBlackTolerance) &&
                        !Utilities.AboutZero(transparency, kBlackTolerance))
                    {
                        macros.Add(PipelineUtilities.NewMacro(kTransparentPrefix + kColorPostfix, kTransparentSemanticPrefix + kColorSemanticPostfix));
                        retMaterial.Parameters.Add(new SiatMaterialContent.Parameter(kTransparentSemanticPrefix + kColorSemanticPostfix, ParameterType.kVector4, color));
                        macros.Add(PipelineUtilities.NewMacro(kTransparency, kTransparencySemantic));
                        retMaterial.Parameters.Add(new SiatMaterialContent.Parameter(kTransparencySemantic, ParameterType.kSingle, effect.Transparency));
                        macros.Add(PipelineUtilities.NewMacro(kRgbZero, "1"));
                    }
                }
                else
                {
                    macros.Add(PipelineUtilities.NewMacro(kTransparency, kTransparencySemantic));
                    retMaterial.Parameters.Add(new SiatMaterialContent.Parameter(kTransparencySemantic, ParameterType.kSingle, effect.Transparency));
                    macros.Add(PipelineUtilities.NewMacro(kRgbZero, "1"));
                }
            }
            #endregion

            if (effect.Type == _ColladaElement.Enums.EffectType.kLambert ||
                effect.Type == _ColladaElement.Enums.EffectType.kPhong ||
                effect.Type == _ColladaElement.Enums.EffectType.kBlinn)
            {
                #region Ambient and Diffuse
                if (!_ProcessTexture(aBoundEffect, effect.Ambient, kAmbientPrefix, macros, kAmbientSemanticPrefix, retMaterial))
                {
                    _ProcessColor(effect.Ambient, kAmbientPrefix, macros, kAmbientSemanticPrefix, retMaterial);
                }
                if (!_ProcessTexture(aBoundEffect, effect.Diffuse, kDiffusePrefix, macros, kDiffuseSemanticPrefix, retMaterial))
                {
                    _ProcessColor(effect.Diffuse, kDiffusePrefix, macros, kDiffuseSemanticPrefix, retMaterial);
                }
                #endregion

                if (effect.Type == _ColladaElement.Enums.EffectType.kPhong ||
                    effect.Type == _ColladaElement.Enums.EffectType.kBlinn)
                {
                    #region Specular
                    if (!(effect.Shininess < 1.0f))
                    {
                        bool bShininess = true;

                        if (!_ProcessTexture(aBoundEffect, effect.Specular, kSpecularPrefix, macros, kSpecularSemanticPrefix, retMaterial))
                        {
                            if (!_ProcessColor(effect.Specular, kSpecularPrefix, macros, kSpecularSemanticPrefix, retMaterial))
                            {
                                bShininess = false;
                            }
                        }

                        if (bShininess)
                        {
                            macros.Add(PipelineUtilities.NewMacro(kShininess, kShininessSemantic));
                            retMaterial.Parameters.Add(new SiatMaterialContent.Parameter(kShininessSemantic, ParameterType.kSingle, effect.Shininess));
                        }
                    }
                    #endregion

                    #region Identify Blinn or Phong effect for the shader.
                    if (effect.Type == _ColladaElement.Enums.EffectType.kPhong)
                    {
                        macros.Add(PipelineUtilities.NewMacro(kPhong, "1"));
                    }
                    else
                    {
                        macros.Add(PipelineUtilities.NewMacro(kBlinn, "1"));
                    }
                    #endregion
                }
            }
            macros.Add(PipelineUtilities.NewMacro(kTexcoordsChannelCount, mTotalTexcoordChannels.ToString()));

            if (abAnimated)
            {
                macros.Add(PipelineUtilities.NewMacro(kAnimated, "1"));
            }

            #region Generate hash and check for existing
            string effectId = mBaseName + aMaterial.Id + aBoundEffect.ToString();
            string hashString = string.Empty;
            foreach (CompilerMacro m in macros)
            {
                hashString += m.Name + m.Definition;
            }
            uint hash = Hash.Calculate32(hashString, 0u);
            SiatEffectContent retEffect = new SiatEffectContent(hash, effectId);

            if (mEffects.ContainsKey(retEffect))
            {
                arEffect = mEffects[retEffect];
            }
            else
            {
                #region Compile effect
                CompiledEffect compiledEffect = Effect.CompileEffectFromFile(kStandardEffectFile, macros.ToArray(), null, CompilerOptions.None, TargetPlatform.Windows);
                if (!compiledEffect.Success)
                {
                    throw new Exception("Error: standard effect building failed, \"" + compiledEffect.ErrorsAndWarnings + "\"");
                }
                retEffect.CompiledEffect = compiledEffect;
                #endregion
                mEffects[retEffect] = retEffect;
                arEffect = retEffect;
            }
            #endregion

            if (mMaterials.ContainsKey(retMaterial))
            {
                arMaterial = mMaterials[retMaterial];
            }
            else
            {
                mMaterials[retMaterial] = retMaterial;
                arMaterial = retMaterial;
            }
        }

        private bool _ProcessTexture(BoundEffect aBoundEffect, _ColladaElement aElement, string aPrefix, List<CompilerMacro> aMacros, string aSemanticPrefix, SiatMaterialContent aMaterial)
        {
            if (aElement is _ColladaTexture)
            {
                _ColladaTexture texture = (_ColladaTexture)aElement;
                ColladaImage image = texture.Image;
                if (!image.IsLocation || image.Location == string.Empty)
                {
                    throw new Exception("<image> of texture as defined is not supported.");
                }
                uint texCoordsIndex = 0u;
                if (texture.Texcoords != string.Empty)
                {
                    if (!aBoundEffect.FindUsageIndex(texture.Texcoords, ref texCoordsIndex))
                    {
                        throw new Exception("A usage index for texture coordinates \"" + texture.Texcoords +
                            "\" could not be found.");
                    }
                }
                mTotalTexcoordChannels = Utilities.Max(mTotalTexcoordChannels, (texCoordsIndex + 1u));

                string assetName = Utilities.RemoveExtension(image.Location);
                string textureLocation = image.Location;
                ExternalReference<TextureContent> textureReference = null;

                if (!mTextureCache.TryGetValue(textureLocation, out textureReference))
                {
                    textureReference = new ExternalReference<TextureContent>(textureLocation, mContent.Identity);
                    if (mContext != null)
                    {
                        textureReference = mContext.BuildAsset<TextureContent, TextureContent>(textureReference, typeof(TextureProcessor).Name, mskTextureBuildParameters, null, null);
                    }
                    mTextureCache[textureLocation] = textureReference;
                }

                aMacros.Add(PipelineUtilities.NewMacro(aPrefix + kTexcoordsPostfix, kTexcoordsInput + texCoordsIndex.ToString()));
                aMacros.Add(PipelineUtilities.NewMacro(aPrefix + kTexturePostfix, aSemanticPrefix + kTextureSemanticPostfix));
                aMaterial.Parameters.Add(new SiatMaterialContent.Parameter(aSemanticPrefix + kTextureSemanticPostfix, ParameterType.kTexture, textureReference));

                _ProcessSamplerSettings(texture.Sampler, aPrefix, aMacros);

                return true;
            }

            return false;
        }
        #endregion

        #region Mesh processing
        #region Helpers
        private sealed class VertexContainer : IComparable<VertexContainer>
        {
            public float[] Vertices;

            public VertexContainer(uint aSize)
            {
                Vertices = new float[aSize];
            }

            public float this[int i]
            {
                get
                {
                    return Vertices[i];
                }

                set
                {
                    Vertices[i] = value;
                }
            }

            public VertexContainer Clone()
            {
                VertexContainer ret = new VertexContainer((uint)Vertices.Length);
                Array.Copy(Vertices, ret.Vertices, Vertices.Length);

                return ret;
            }

            public int CompareTo(VertexContainer aContainer)
            {
                int count = Utilities.Min(Vertices.Length, aContainer.Vertices.Length);
                float[] a = Vertices;
                float[] b = aContainer.Vertices;

                for (int i = 0; i < count; i++)
                {
                    if (Utilities.LessThan(a[i], b[i], Utilities.kLooseToleranceFloat))
                    {
                        return -1;
                    }
                    else if (Utilities.GreaterThan(a[i], b[i], Utilities.kLooseToleranceFloat))
                    {
                        return 1;
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// Used for validating vertex declaration
        /// </summary>
        /// <remarks>
        /// It is possible for a COLLADA primitive to contain multiple vertex channels with
        /// the same Usage (i.e. COLOR) and usage index. This is invalid - two vertex channels
        /// with the same Usage should have different indices (for example, TEXCOORD0 and
        /// TEXCOORD1). This struct is used to catch this and remove the duplicate channel.
        /// </remarks>
        private struct VertexDeclarationHelper : IComparable<VertexDeclarationHelper>
        {
            public VertexDeclarationHelper(VertexElementUsage aUsage, byte aUsageIndex)
            {
                Usage = aUsage;
                UsageIndex = aUsageIndex;
            }

            public int CompareTo(VertexDeclarationHelper b)
            {
                if (Usage == b.Usage)
                {
                    if (UsageIndex == b.UsageIndex)
                    {
                        return 0;
                    }
                    else if (UsageIndex < b.UsageIndex)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else if (Usage < b.Usage)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }

            public VertexElementUsage Usage;
            public byte UsageIndex;
        }

        private struct VertexChannel
        {
            public VertexChannel(_ColladaInput aInput)
            {
                Data = aInput.GetArray<float>().Array;
                Format = PipelineUtilities.ColladaSemanticToXnaFormat(aInput.Semantic, aInput.Stride);
                StrideInSingles = (short)aInput.Stride;
                Usage = PipelineUtilities.ColladaSemanticToXnaUsage(aInput.Semantic);
                UsageIndex = (byte)aInput.Set;
            }

            public VertexChannel(float[] aData, VertexElementFormat aFormat,
                short aStrideInSingles, VertexElementUsage aUsage,
                byte aUsageIndex)
            {
                Data = aData;
                Format = aFormat;
                StrideInSingles = aStrideInSingles;
                Usage = aUsage;
                UsageIndex = aUsageIndex;
            }

            public readonly float[] Data;
            public readonly VertexElementFormat Format;
            public readonly short StrideInSingles;
            public readonly VertexElementUsage Usage;
            public readonly byte UsageIndex;

            public float this[int i]
            {
                get
                {
                    return Data[i];
                }
            }
        };

        private VertexChannel[] _Convert(_ColladaInput[] aIn)
        {
            VertexChannel[] ret = new VertexChannel[aIn.Length];

            int count = aIn.Length;
            for (int i = 0; i < count; i++)
            {
                ret[i] = new VertexChannel(aIn[i]);
            }

            return ret;
        }
        #endregion

        private int _AddMeshPartSceneNode(MeshPartSceneNodeContent aNode)
        {
            foreach (SceneNodeContent e in mScene.Nodes)
            {
                if (e is MeshPartSceneNodeContent)
                {
                    MeshPartSceneNodeContent p = (MeshPartSceneNodeContent)e;

                    if (_ShouldCombine(p, aNode))
                    {
                        if (_CombineMeshPart(p, aNode))
                        {
                            return 0;
                        }
                    }
                }
            }

            mScene.Nodes.Add(aNode);
            return 1;
        }

        private bool _CombineMeshPart(MeshPartSceneNodeContent a, MeshPartSceneNodeContent b)
        {
            Matrix transform = b.WorldTransform * Matrix.Invert(a.WorldTransform);
            PipelineUtilities.ApplyTransform(b.MeshPart, ref transform);

            int vertexCountA = a.MeshPart.VertexCount;
            for (int i = 0; i < b.MeshPart.Indices.Length; i++)
            {
                b.MeshPart.Indices[i] += vertexCountA;
            }

            a.MeshPart.Indices = Utilities.Combine(a.MeshPart.Indices, b.MeshPart.Indices);
            a.MeshPart.PrimitiveCount += b.MeshPart.PrimitiveCount;
            a.MeshPart.VertexCount += b.MeshPart.VertexCount;
            a.MeshPart.Vertices = Utilities.Combine(a.MeshPart.Vertices, b.MeshPart.Vertices);

            b.MeshPart.Effect = null;
            b.MeshPart.Id = string.Empty;
            b.MeshPart.Indices = null;
            b.MeshPart.PrimitiveCount = 0;
            b.MeshPart.VertexCount = 0;
            b.MeshPart.VertexDeclaration = null;
            b.MeshPart.Vertices = null;

            return true;
        }

        private void _GetMeshAndMaterials(ColladaNode aNode, out SiatMeshContent arMesh,
            out MaterialsBySymbol arMaterials, out EffectsBySymbol arEffects,
            float[] aBoneIndices, float[] aBoneWeights)
        {
            ColladaInstanceController instanceController = aNode.GetFirst<ColladaInstanceController>();
            ColladaController controller = instanceController.Instance;
            ColladaSkin skin = (ColladaSkin)controller.FirstChild;
            ColladaGeometry geometry = (ColladaGeometry)skin.Source;
            string geometryId = mBaseName + geometry.Id;

            EffectsBySymbol effectsBySymbol = new EffectsBySymbol();
            MaterialsBySymbol materialsBySymbol = new MaterialsBySymbol();
            ColladaBindMaterial bindMaterial = instanceController.GetFirstOptional<ColladaBindMaterial>();

            if (bindMaterial != null)
            {
                _GetMaterials(bindMaterial, ref materialsBySymbol, ref effectsBySymbol, true);
            }

            if (!mContent.Meshes.ContainsKey(geometryId))
            {
                mContent.Meshes[geometryId] = _ProcessGeometry(geometry, aBoneIndices, aBoneWeights);
            }

            arEffects = effectsBySymbol;
            arMaterials = materialsBySymbol;
            arMesh = mContent.Meshes[geometryId];
        }

        private void _GetMesh(ColladaInstanceGeometry aInstanceGeometry, out SiatMeshContent arMesh)
        {
            ColladaGeometry geometry = aInstanceGeometry.Instance;
            string geometryId = mBaseName + geometry.Id;

            if (!mContent.Meshes.ContainsKey(geometryId))
            {
                mContent.Meshes[geometryId] = _ProcessGeometry(geometry);
            }

            arMesh = mContent.Meshes[geometryId];
        }

        private void _GetMeshAndMaterials(ColladaNode aNode, out SiatMeshContent arMesh, out MaterialsBySymbol arMaterials, out EffectsBySymbol arEffects)
        {
            ColladaInstanceGeometry instanceGeometry = aNode.GetFirst<ColladaInstanceGeometry>();

            EffectsBySymbol effectsBySymbol = new EffectsBySymbol();
            MaterialsBySymbol materialsBySymbol = new MaterialsBySymbol();
            ColladaBindMaterial bindMaterial = instanceGeometry.GetFirstOptional<ColladaBindMaterial>();

            if (bindMaterial != null)
            {
                _GetMaterials(bindMaterial, ref materialsBySymbol, ref effectsBySymbol, false);
            }

            arEffects = effectsBySymbol;
            arMaterials = materialsBySymbol;
            _GetMesh(instanceGeometry, out arMesh);
        }
        
        private void _ProcessInstanceGeometry(ColladaNode aNode, int aChildrenCount, ref Matrix aLocalTransform, ref Matrix aWorldTransform)
        {
            SiatMeshContent mesh;
            MaterialsBySymbol materials;
            EffectsBySymbol effects;
            _GetMeshAndMaterials(aNode, out mesh, out materials, out effects);

            MeshSceneNodeContent meshNode = new MeshSceneNodeContent(mBaseName + aNode.Id, aChildrenCount + mesh.Parts.Count, ref aLocalTransform, mesh);
            mScene.Nodes.Add(meshNode);

            int count = 0;
            foreach (SiatMeshContent.Part e in mesh.Parts)
            {
                MeshPartSceneNodeContent meshPartNode = new MeshPartSceneNodeContent(mBaseName + aNode.Id + kMeshPartPostfix + count.ToString(), 0, ref Utilities.kIdentity, ref aWorldTransform, effects[e.Effect], materials[e.Effect], e);
                count += _AddMeshPartSceneNode(meshPartNode);
            }
            meshNode.ChildrenCount = count;
        }

        private SiatMeshContent _ProcessGeometry(ColladaGeometry aGeometry, float[] aBoneIndices, float[] aBoneWeights)
        {
            if (!aGeometry.IsMesh)
            {
                throw new Exception(Utilities.kNotImplemented);
            }

            SiatMeshContent siatMesh = new SiatMeshContent();
            ColladaMesh colladaMesh = aGeometry.Mesh;

            int partCount = 0;
            foreach (_ColladaPrimitive p in colladaMesh.GetEnumerable<_ColladaPrimitive>())
            {
                string partId = mBaseName + aGeometry.Id + kMeshPartPostfix + partCount.ToString();
                siatMesh.Parts.Add(_ProcessPrimitives(p, partId, aBoneIndices, aBoneWeights));
                partCount++;
            }

            return siatMesh;
        }

        private SiatMeshContent _ProcessGeometry(ColladaGeometry aGeometry)
        {
            return _ProcessGeometry(aGeometry, null, null);
        }

        private SiatMeshContent.Part _ProcessPrimitives(_ColladaPrimitive p, string aId, float[] aBoneIndices, float[] aBoneWeights)
        {
            SiatMeshContent.Part siatPart = new SiatMeshContent.Part();
            _ProcessPrimitivesHelper(p, ref siatPart, aBoneIndices, aBoneWeights);
            siatPart.Id = aId;
            siatPart.Effect = p.Material;
            siatPart.PrimitiveType = PipelineUtilities.ColladaPrimitiveToXnaPrimitive(p);
            siatPart.PrimitiveCount = Utilities.GetPrimitiveCount(siatPart.PrimitiveType, siatPart.Indices.Length);

            // This normalizes the vertices a mesh to Siat XNA's coordinate frame (Y axis is up).
            PipelineUtilities.ApplyTransform(siatPart, ref mUpAxisTransform);

            return siatPart;
        }

        private void _ProcessPrimitivesHelper(_ColladaPrimitive p, ref SiatMeshContent.Part arOut, float[] aBoneIndices, float[] aBoneWeights)
        {
            uint offsetCount = p.OffsetCount;
            // p.Indices.Count is the total number of indices in the index buffer for this primitive.
            // The number of indices per vertex channel offset is equal to this value / the offset count.
            uint indexCount = p.Indices.Count / offsetCount;
            int[][] rawIndices = new int[offsetCount][];

            // Collada supports individual index buffers per vertex channel. i.e. the
            // TEXCOORD channel can have its own separate index buffer from the POSITION
            // channel (and often does). Real-time rendering only supports one index buffer
            // so the multiple index buffers must be compacted into a single index buffer.
            // This step gathers the indices into a more usable form.
            #region Gather raw indices
            for (uint i = 0; i < offsetCount; i++)
            {
                rawIndices[i] = new int[indexCount];
            }

            for (uint i = 0; i < indexCount; i++)
            {
                for (uint j = 0; j < offsetCount; j++)
                {
                    uint index = (i * offsetCount) + j;
                    rawIndices[j][i] = p.Indices[index];
                }
            }
            #endregion

            // This gathers all the vertex buffers that are matched with an index buffer.
            // Often there is only one vertex buffer per index buffer, but their can be
            // any number > 0. Vertex buffers are represented by <input> elements.
            #region Gather raw vertices
            SortedList<VertexDeclarationHelper, bool> declCache = new SortedList<VertexDeclarationHelper, bool>();
            int totalVertexChannels = 0;
            VertexChannel[][] rawVertices = new VertexChannel[offsetCount][];
            for (uint i = 0; i < offsetCount; i++)
            {
                rawVertices[i] = _Convert(p.FindInputs(i));
                #region Check for redundant channels and remove them.
                int verticesLength = rawVertices[i].Length;
                for (int j = 0; j < verticesLength; j++)
                {
                    VertexDeclarationHelper helper = new VertexDeclarationHelper(rawVertices[i][j].Usage, rawVertices[i][j].UsageIndex);
                    if (declCache.ContainsKey(helper))
                    {
                        VertexChannel[] newVertices = new VertexChannel[verticesLength - 1];
                        Array.Copy(rawVertices[i], 0, newVertices, 0, j);
                        Array.Copy(rawVertices[i], j + 1, newVertices, j, (verticesLength - (j + 1)));
                        rawVertices[i] = newVertices;
                        verticesLength--;
                    }
                    else
                    {
                        declCache[helper] = true;
                    }
                }
                #endregion
                totalVertexChannels += rawVertices[i].Length;
            }
            #endregion

            // This inserts the bone indices and weights if they are not null. This is necessary
            // for skeletal animated meshes.
            #region Insert bone indices and weights
            if (aBoneIndices != null && aBoneWeights != null)
            {
                for (uint i = 0; i < offsetCount; i++)
                {
                    int verticesLength = rawVertices[i].Length;

                    if (verticesLength > 0 && rawVertices[i][0].Usage == VertexElementUsage.Position)
                    {
                        Array.Resize<VertexChannel>(ref rawVertices[i], verticesLength + 2);
                        rawVertices[i][verticesLength + 0] = new VertexChannel(aBoneIndices, VertexElementFormat.Vector4,
                            (short)kJointInfluencesPerVertex, VertexElementUsage.BlendIndices, 0);
                        rawVertices[i][verticesLength + 1] = new VertexChannel(aBoneWeights, VertexElementFormat.Vector4,
                            (short)kJointInfluencesPerVertex, VertexElementUsage.BlendWeight, 0);
                        break;
                    }
                }

                totalVertexChannels += 2;
            }
            #endregion

            // This builds a VertexElement array which describes a single element of the final
            // vertex buffer that will be generated.
            #region Calculate vertex declaration
            arOut.VertexDeclaration = new VertexElement[totalVertexChannels];
            int declarationIndex = 0;
            uint vertexStrideInSingles = 0;
            for (uint i = 0; i < offsetCount; i++)
            {
                int innerCount = rawVertices[i].Length;
                for (uint j = 0; j < innerCount; j++)
                {
                    VertexChannel channel = rawVertices[i][j];
                    arOut.VertexDeclaration[declarationIndex] = new VertexElement(0,
                        (short)(vertexStrideInSingles * sizeof(float)), channel.Format,
                        VertexElementMethod.Default, channel.Usage, channel.UsageIndex);

                    declarationIndex++;
                    vertexStrideInSingles += (uint)channel.StrideInSingles;
                }
            }

            if (mVertexDeclarations.ContainsKey(arOut.VertexDeclaration))
            {
                arOut.VertexDeclaration = mVertexDeclarations[arOut.VertexDeclaration];
            }
            else
            {
                mVertexDeclarations[arOut.VertexDeclaration] = arOut.VertexDeclaration;
            }

            arOut.VertexStrideInSingles = (int)vertexStrideInSingles;
            #endregion

            // Builds a single compact index and vertex buffer. The vertex format is "interleaved"
            // in Open GL terms or a non-FVF vertex buffer in Direct X terms.
            #region Flatten indices and vertices
            List<int> indices = new List<int>();
            List<float> vertices = new List<float>();
            SortedList<VertexContainer, int> cache = new SortedList<VertexContainer, int>();
            VertexContainer vertex = new VertexContainer(vertexStrideInSingles);

            int vertexCount = 0;
            for (uint i = 0; i < indexCount; i++)
            {
                int offset = 0;
                for (uint j = 0; j < offsetCount; j++)
                {
                    uint index = (uint)rawIndices[j][i];
                    uint vertexChannels = (uint)rawVertices[j].Length;
                    for (uint k = 0; k < vertexChannels; k++)
                    {
                        uint stride = (uint)rawVertices[j][k].StrideInSingles;
                        uint start = index * stride;
                        uint end = start + stride;

                        for (uint l = start; l < end; l++)
                        {
                            vertex[offset++] = rawVertices[j][k].Data[l];
                        }
                    }
                }

                #region Add the vertex - if it exists, pointer the index to the existing vertex.
                int existingIndex = -1;
                if (cache.TryGetValue(vertex, out existingIndex))
                {
                    indices.Add(existingIndex);
                }
                else
                {
                    VertexContainer v = vertex.Clone();
                    indices.Add(vertexCount);
                    cache[v] = vertexCount;
                    vertices.AddRange(v.Vertices);
                    vertexCount++;
                }
                #endregion
            }

            // COLLADA vertices are wound Counter-Clockwise while XNA default winding is Clockwise.
            // Siat XNA is configured the same way by default.
            if (Utilities.kWinding == Winding.Clockwise)
            {
                Utilities.FlipWindingOrder(PipelineUtilities.ColladaPrimitiveToXnaPrimitive(p), indices);
            }
            // Direct X (and subsequently XNA) expect texture coordinates to run [0...1] from the
            // top-left corner of the image to the bottom-right. COLLADA follows the Open GL
            // standard of bottom-left to top-right. This requires flipping the V coordinate
            // of any texture coordinates.
            Utilities.FlipTextureV(vertices, arOut.VertexDeclaration, (int)vertexStrideInSingles);

            arOut.Indices = indices.ToArray();
            arOut.Vertices = vertices.ToArray();
            arOut.VertexCount = (int)(vertices.Count / vertexStrideInSingles);
            #endregion
        }

        private bool _ShouldCombine(MeshPartSceneNodeContent a, MeshPartSceneNodeContent b)
        {
#if DISABLE_MESH_COMBINATION
            return false;
#else
            bool bReturn = true;

            bReturn = bReturn && a.Effect.Equals(b.Effect);
            bReturn = bReturn && a.Material.Equals(b.Material);
            bReturn = bReturn && a.MeshPart.PrimitiveType.Equals(b.MeshPart.PrimitiveType);
            bReturn = bReturn && a.MeshPart.VertexDeclaration.Equals(b.MeshPart.VertexDeclaration);

#if ENABLE_SOPHISTICATED_MESH_COMBINE_METRIC
            if (bReturn)
            {
                Matrix transform = b.WorldTransform * (Matrix.Invert(a.WorldTransform));

                BoundingBox aBox = a.MeshPart.AABB;
                BoundingBox bBox = b.MeshPart.AABB; Utilities.Transform(ref bBox, ref transform, out bBox);
                BoundingBox tBox = BoundingBox.CreateMerged(aBox, bBox);

                float aSA = Utilities.GetSurfaceArea(ref aBox);
                float bSA = Utilities.GetSurfaceArea(ref bBox);
                float tSA = Utilities.GetSurfaceArea(ref tBox);

                float aL = Utilities.Max(Utilities.GetExtents(ref aBox));
                float bL = Utilities.Max(Utilities.GetExtents(ref bBox));
                float tL = Utilities.Max(Utilities.GetExtents(ref tBox));

                float aCount = a.MeshPart.PrimitiveCount;
                float bCount = b.MeshPart.PrimitiveCount;
                float tCount = aCount + bCount;

                float aCountCost = (float)Math.Abs(Math.Log(kTooSmallCount / aCount, kLogBase));
                float bCountCost = (float)Math.Abs(Math.Log(kTooSmallCount / bCount, kLogBase));
                float tCountCost = (float)Math.Abs(Math.Log(kTooSmallCount / tCount, kLogBase));

                float tCost = kIntersectionCost * tCountCost;
                // Note: bSA * aCount and aSA * bCount is intentional.
                float cCost = (kLocalizationCost * (tL / (aL + bL))) +
                              (kIntersectionCost * (tSA / ((bSA * aCountCost) + (aSA * bCountCost))));

                bReturn = (Utilities.GreaterThan(tCost, cCost, Utilities.kLooseToleranceFloat));
            }
#endif
            return bReturn;
#endif
        }
        #endregion

        #region Physics processing
        private Vector3[] _Convert(float[] a)
        {
            if (a.Length % 3 != 0) { throw new Exception("Input array is incorrect length."); }
            Vector3[] ret = new Vector3[a.Length / 3];
            int count = a.Length;

            int index = 0;
            for (int i = 0; i < count; i += 3)
            {
                ret[index++] = new Vector3(a[i + 0], a[i + 1], a[i + 2]);
            }

            return ret;
        }

        private bool _ExtractPhysicsIndexPosition(_ColladaPrimitive p, out int[] arIndices, out Vector3[] arPositions)
        {
            _ColladaInput input = p.FindInput(_ColladaElement.Enums.InputSemantic.kVertex);

            uint offset = ((ColladaInputGroupB)input).Offset;
            uint offsetCount = p.OffsetCount;

            input = p.FindInput(_ColladaElement.Enums.InputSemantic.kPosition);

            if (input.Stride == 3)
            {
                arIndices = p.Indices.GetSparse(offset, offsetCount);
                arPositions = _Convert(input.GetArray<float>().Array);

                Utilities.FlipWindingOrder(PipelineUtilities.ColladaPrimitiveToXnaPrimitive(p), arIndices);

                return true;
            }

            arIndices = null;
            arPositions = null;
            return false;
        }

        private struct Entry
        {
            public int[] Indices;
            public Vector3[] Positions;
        }

        private TriangleTree _ProcessPhysics(ColladaCOLLADA aRoot)
        {
            Dictionary<_ColladaPrimitive, Entry> cache = new Dictionary<_ColladaPrimitive, Entry>();

            List<Triangle> triangles = new List<Triangle>();

            aRoot.Apply<ColladaInstanceGeometry>(_ColladaElement.ApplyType.RecurseDown,
                _ColladaElement.ApplyStop.Delegate, delegate(ColladaInstanceGeometry instance)
                {
                    ColladaNode parent = (ColladaNode)instance.Parent;
                    ColladaGeometry geom = instance.Instance;
                    if (!geom.IsMesh) { return false; }
                    ColladaMesh mesh = geom.Mesh;

                    foreach (_ColladaPrimitive e in mesh.GetEnumerable<_ColladaPrimitive>())
                    {
                        Entry entry;

                        if (cache.ContainsKey(e)) { entry = cache[e]; }
                        else
                        {
                            if (_ExtractPhysicsIndexPosition(e, out entry.Indices, out entry.Positions)) { cache.Add(e, entry); }
                            else { continue; }
                        }

                        Matrix m = parent.WorldXnaTransform * mUpAxisTransform;

                        int count = entry.Indices.Length;
                        for (int i = 0; i < count; i += 3)
                        {
                            int i0 = entry.Indices[i+0];
                            int i1 = entry.Indices[i+1];
                            int i2 = entry.Indices[i+2];

                            Vector3 p0 = Vector3.Transform(entry.Positions[i0], m);
                            Vector3 p1 = Vector3.Transform(entry.Positions[i1], m);
                            Vector3 p2 = Vector3.Transform(entry.Positions[i2], m);

                            Triangle tri = new Triangle(p0, p1, p2);
                            if (!tri.IsDegenerate) { triangles.Add(tri); }
                        }
                    }

                    return false;
                });

            TriangleTree ret = new TriangleTree();
            ret.Build(triangles);

            return ret;
        }
        #endregion

        private void _ProcessInstanceLight(ColladaNode aNode, int childrenCount, ref Matrix aLocalTransform)
        {
            ColladaLight light = aNode.GetFirst<ColladaInstanceLight>().Instance;
            ColladaLightData lightData = light.LightData;

            Vector3 lightColor = light.MayaIntensity * lightData.Color;

            if (lightData.Type == _ColladaElement.Enums.LightType.kAmbient)
            {
                if (mContext != null)
                {
                    mContext.Logger.LogImportantMessage("Warning: Siat XNA does not explicitly support " +
                        "light type <ambient>. The light referenced by <node> element \"" +
                        aNode.Id + "\" has been converted into a directional light.");
                }

                mScene.Nodes.Add(new DirectionalLightSceneNodeContent(mBaseName + aNode.Id, childrenCount, ref aLocalTransform, lightColor));
                return;
            }
            else if (lightData.Type == _ColladaElement.Enums.LightType.kDirectional)
            {
                mScene.Nodes.Add(new DirectionalLightSceneNodeContent(mBaseName + aNode.Id, childrenCount, ref aLocalTransform, lightColor));
                return;
            }
            else if (lightData.Type == _ColladaElement.Enums.LightType.kPoint)
            {
                Vector3 attenuation = new Vector3(lightData.ConstantAttenuation, lightData.LinearAttenuation, lightData.QuadraticAttenuation);
                mScene.Nodes.Add(new PointLightSceneNodeContent(mBaseName + aNode.Id, childrenCount, ref aLocalTransform, lightColor, attenuation));
                return;
            }
            else if (lightData.Type == _ColladaElement.Enums.LightType.kSpot)
            {
                float dropoff = light.MayaDropoff;
                Vector3 attenuation = new Vector3(lightData.ConstantAttenuation, lightData.LinearAttenuation, lightData.QuadraticAttenuation);
                float angleInRadians = MathHelper.ToRadians(lightData.FalloffAngleInDegrees);
                float falloffExponent = Utilities.AboutZero(dropoff, Utilities.kLooseToleranceFloat) ? lightData.FalloffExponent : dropoff;
                mScene.Nodes.Add(new SpotLightSceneNodeContent(mBaseName + aNode.Id, childrenCount, ref aLocalTransform, lightColor, attenuation, angleInRadians, falloffExponent));
                return;
            }

            throw new Exception(Utilities.kShouldNotBeHere);
        }

        private bool _ProcessPortal(ColladaExtra aExtra, ColladaNode aNode, int childrenCount, ref Matrix aLocalTransform)
        {
            ColladaTechnique technique = aExtra.GetFirst<ColladaTechnique>();

            if (technique.Profile == kMayaProfile)
            {
                foreach (_ColladaElement e in technique.GetEnumerable<_ColladaElement>())
                {
                    if (e is _ColladaGenericElement && ((_ColladaGenericElement)e).Name == kMayaDynamicAttributes)
                    {
                        bool bTo = false;
                        string to = string.Empty;

                        #region Use TreeNode.Apply to find the necessary children.
                        e.Apply<_ColladaGenericElement>(TreeNode<_ColladaElement>.ApplyType.RecurseDown, TreeNode<_ColladaElement>.ApplyStop.Delegate,
                            delegate(_ColladaGenericElement f)
                            {
                                if (f.Name == kMayaPortalToAttribute)
                                {
                                    if (f.GetContains(kMayaDynamicAttributeTypeAttribute))
                                    {
                                        if (f[kMayaDynamicAttributeTypeAttribute] == kMayaDynamicAttributeStringType)
                                        {
                                            if (f.Value != string.Empty)
                                            {
                                                to = f.Value;
                                                bTo = true;
                                                return true;
                                            }
                                        }
                                    }
                                }

                                return false;
                            });
                        #endregion

                        if (bTo)
                        {
                            EffectsBySymbol effects;
                            MaterialsBySymbol materials;
                            SiatMeshContent mesh;
                            _GetMeshAndMaterials(aNode, out mesh, out materials, out effects);

                            PortalContent portalContent;
                            if (!mContent.Portals.TryGetValue(mesh, out portalContent))
                            {
                                portalContent = new PortalContent(mesh);
                                mContent.Portals.Add(mesh, portalContent);
                            }

                            if (mContext != null)
                            {
                                string[] split = to.Split(_ColladaElement.Settings.kFragmentDelimiter);

                                if (split.Length == 2)
                                {
                                    if (!Path.HasExtension(split[0]))
                                    {
                                        split[0] = Path.ChangeExtension(split[0], kColladaExtension);
                                    }

                                    ExternalReference<ColladaCOLLADA> reference = new ExternalReference<ColladaCOLLADA>(split[0], mContent.Identity);
                                    ExternalReference<SceneContent> sceneReference = mContext.BuildAsset<ColladaCOLLADA, SceneContent>(reference, typeof(ColladaProcessor).Name);
                                    to = PipelineUtilities.ExtractXnaAssetName(sceneReference.Filename) + _ColladaElement.Settings.kFragmentDelimiter + split[1];
                                }
                            }

                            mScene.Nodes.Add(new PortalSceneNodeContent(mBaseName + aNode.Id, childrenCount, ref aLocalTransform, portalContent, to));
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool _ProcessSky(ColladaExtra aExtra, ColladaNode aNode, int childrenCount, ref Matrix aLocalTransform)
        {
            ColladaTechnique technique = aExtra.GetFirst<ColladaTechnique>();
            bool bSky = false;

            #region Check extra attribute to see if this node is a sky node.
            if (technique.Profile == kMayaProfile)
            {
                foreach (_ColladaGenericElement e in technique.GetEnumerable<_ColladaGenericElement>())
                {
                    if (e.Name == kMayaDynamicAttributes)
                    {
                        foreach (_ColladaGenericElement f in e.GetEnumerable<_ColladaGenericElement>())
                        {
                            if (f.Name == kMayaSkyAttribute)
                            {
                                if (f.GetContains(kMayaDynamicAttributeTypeAttribute) &&
                                    f[kMayaDynamicAttributeTypeAttribute] == kMayaDynamicAttributeBooleanType)
                                {
                                    bSky = XmlConvert.ToBoolean(f.Value);
                                    goto done;
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Extra sky
        done:
            if (bSky)
            {
                EffectsBySymbol effects;
                MaterialsBySymbol materials;
                SiatMeshContent mesh;
                _GetMeshAndMaterials(aNode, out mesh, out materials, out effects);

                if (mesh.Parts.Count != 1)
                {
                    throw new Exception("A sky node must have one mesh that has one and only one part.");
                }

                SiatMeshContent.Part part = mesh.Parts[0];

                mScene.Nodes.Add(new SkySceneNodeContent(mBaseName + aNode.Id, childrenCount,
                    ref aLocalTransform, effects[part.Effect], materials[part.Effect],
                    part));

                return true;
            }
            #endregion

            return false;
        }

        #region Scene node processing
        private void _CheckAndSetAlreadyFoundInstance(ref bool abFoundInstanceElement, string aNodeId)
        {
            if (abFoundInstanceElement)
            {
                throw new Exception("Siat XNA does not support multiple <instance_*> elements " +
                    "as children of <node> elements. Please remove multiple <instance_*> " +
                    "elements from <node> element \"" + aNodeId + "\" and try this build " +
                    "again.");
            }
            else
            {
                abFoundInstanceElement = true;
            }
        }

        private void _ProcessNode(ColladaNode aNode, ref Matrix aParentTransform)
        {
            bool bFoundInstanceElement = false;
            int childrenCount = aNode.GetChildCount<ColladaNode>();
            Matrix localTransform = mInverseUpAxisTransform * aNode.LocalXnaTransform * mUpAxisTransform;
            Matrix worldTransform = localTransform * aParentTransform;

            // Process extra - note this is done first to identify a portal node.
            foreach (ColladaExtra e in aNode.GetEnumerable<ColladaExtra>())
            {
                if (_ProcessPortal(e, aNode, childrenCount, ref localTransform))
                {
                    _CheckAndSetAlreadyFoundInstance(ref bFoundInstanceElement, aNode.Id);
                }

                if (_ProcessSky(e, aNode, childrenCount, ref localTransform))
                {
                    _CheckAndSetAlreadyFoundInstance(ref bFoundInstanceElement, aNode.Id);
                }
            }

            // This node is expected to be part of a bone hiearchy.
            if (aNode.Type == _ColladaElement.Enums.InputSemantic.kJoint)
            {
                _ProcessJoint(aNode, childrenCount, ref localTransform);
                _CheckAndSetAlreadyFoundInstance(ref bFoundInstanceElement, aNode.Id);
            }

            // Process any special nodes. Won't be done if we've already processed this as a Portal
            // node.
            if (!bFoundInstanceElement)
            {
                foreach (_ColladaElement e in aNode.GetEnumerable<_ColladaElement>())
                {
                    if (e is ColladaInstanceCamera)
                    {
                        _CheckAndSetAlreadyFoundInstance(ref bFoundInstanceElement, aNode.Id);
                        if (mContext != null)
                        {
                            mContext.Logger.LogImportantMessage("Siat XNA currently does not support " +
                                "<instance_camera> as a child of <node>. This will be added in a future " +
                                "release but the <node> \"" + aNode.Id + "\" has been converted to a " +
                                "standard scene node for now.");
                        }
                        mScene.Nodes.Add(new SceneNodeContent(mBaseName + aNode.Id, childrenCount, ref localTransform));
                    }
                    else if (e is ColladaInstanceController)
                    {
                        _CheckAndSetAlreadyFoundInstance(ref bFoundInstanceElement, aNode.Id);
                        _ProcessInstanceController(aNode, childrenCount, ref localTransform);
                    }
                    else if (e is ColladaInstanceGeometry)
                    {
                        _CheckAndSetAlreadyFoundInstance(ref bFoundInstanceElement, aNode.Id);
                        _ProcessInstanceGeometry(aNode, childrenCount, ref localTransform, ref worldTransform);
                    }
                    else if (e is ColladaInstanceLight)
                    {
                        _CheckAndSetAlreadyFoundInstance(ref bFoundInstanceElement, aNode.Id);
                        _ProcessInstanceLight(aNode, childrenCount, ref localTransform);
                    }
                    else if (e is ColladaInstanceNode)
                    {
                        _CheckAndSetAlreadyFoundInstance(ref bFoundInstanceElement, aNode.Id);
                        if (mContext != null)
                        {
                            mContext.Logger.LogImportantMessage("Siat XNA current does not support " +
                                "<instance_node> as a child of <node>. This will be added in a future " +
                                "release but the <node> \"" + aNode.Id + "\" has been converted to a " +
                                "standard scene node for now.");
                        }
                        mScene.Nodes.Add(new SceneNodeContent(mBaseName + aNode.Id, childrenCount, ref localTransform));
                    }
                }
            }

            // If this node wasn't a special node, add a regular scene node.
            if (!bFoundInstanceElement)
            {
                mScene.Nodes.Add(new SceneNodeContent(mBaseName + aNode.Id, childrenCount, ref localTransform));
            }

            // Process children.
            if (childrenCount > 0)
            {
                foreach (ColladaNode n in aNode.GetEnumerable<ColladaNode>())
                {
                    _ProcessNode(n, ref worldTransform);
                }
            }
        }

        private void _ProcessRoot(ColladaCOLLADA aRoot)
        {
            mUpAxisTransform = aRoot.GetFirst<ColladaAsset>()
                .GetXnaAxisTransform(_ColladaElement.Enums.UpAxis.kY);
            mInverseUpAxisTransform = Matrix.Invert(mUpAxisTransform);

            // Note: strangely, although  a COLLADA document can have multiple <visual_scene> elements
            //       defined in <library_visual_scenes> it can and must instance one and only one
            //       in the root <scene> element. Therefore, the only apparent purpose of defining
            //       multiple <visual_scene> elements in a single document is to centralize them
            //       in a single document and reference them from other documents.
            ColladaVisualScene visualScene = aRoot.GetFirst<ColladaScene>().GetFirst<ColladaInstanceVisualScene>().Instance;

            // COLLADA does not necessarily specify a root scene node but
            // Siat XNA requires one for each cell. This treats all child <node>
            // elements of <visual_scene> as children of a root scene node.
            int childrenCount = visualScene.GetChildCount<ColladaNode>();
            SceneNodeContent root = new SceneNodeContent(mBaseName + visualScene.Id, childrenCount, ref Utilities.kIdentity);
            mScene.Nodes.Add(root);

            foreach (ColladaNode n in visualScene.GetEnumerable<ColladaNode>())
            {
                _ProcessNode(n, ref Utilities.kIdentity);
            }
        }
        #endregion
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aPart"></param>
        /// 
        /// \todo This function needs to support more format types.
        /// \todo Yuck. I need to go back through the entire processor and avoid all this converting back
        ///       and forth stuff.
        private void _Optimize(SiatMeshContent.Part aPart)
        {
            MeshBuilder builder = MeshBuilder.StartMesh(aPart.Id);
            builder.MergeDuplicatePositions = true;
            builder.MergePositionTolerance = Utilities.kLooseToleranceFloat;

            #region Create positions in builder
            {
                List<Vector3> positions;
                PipelineUtilities.ExtractPositions(aPart, out positions);

                foreach (Vector3 e in positions)
                {
                    builder.CreatePosition(e);
                }
            }
            #endregion

            int[] channels = new int[aPart.VertexDeclaration.Length];

            #region Create channels in builder
            {
                int index = 0;
                foreach (VertexElement e in aPart.VertexDeclaration)
                {
                    if (e.VertexElementUsage != VertexElementUsage.Position)
                    {
                        switch (e.VertexElementFormat)
                        {
                            case VertexElementFormat.Vector2: channels[index] = builder.CreateVertexChannel<Vector2>(VertexChannelNames.EncodeName(e.VertexElementUsage, e.UsageIndex)); break;
                            case VertexElementFormat.Vector3: channels[index] = builder.CreateVertexChannel<Vector3>(VertexChannelNames.EncodeName(e.VertexElementUsage, e.UsageIndex)); break;
                            case VertexElementFormat.Vector4: channels[index] = builder.CreateVertexChannel<Vector4>(VertexChannelNames.EncodeName(e.VertexElementUsage, e.UsageIndex)); break;
                            default:
                                throw new ArgumentOutOfRangeException(e.VertexElementFormat.ToString());
                        }
                    }

                    index++;
                }
            }
            #endregion

            #region Populate channels and index buffer in builder
            {
                int stride = aPart.VertexStrideInSingles;

                foreach (int e in aPart.Indices)
                {
                    int index = 0;
                    int baseI = (e * stride);

                    foreach (VertexElement f in aPart.VertexDeclaration)
                    {
                        if (f.VertexElementUsage != VertexElementUsage.Position)
                        {
                            int offset = (f.Offset / sizeof(float));
                            int i = baseI + offset;

                            switch (f.VertexElementFormat)
                            {
                                case VertexElementFormat.Vector2: builder.SetVertexChannelData(channels[index], new Vector2(aPart.Vertices[i + 0], aPart.Vertices[i + 1])); break;
                                case VertexElementFormat.Vector3: builder.SetVertexChannelData(channels[index], new Vector3(aPart.Vertices[i + 0], aPart.Vertices[i + 1], aPart.Vertices[i + 2])); break;
                                case VertexElementFormat.Vector4: builder.SetVertexChannelData(channels[index], new Vector4(aPart.Vertices[i + 0], aPart.Vertices[i + 1], aPart.Vertices[i + 2], aPart.Vertices[i + 3])); break;
                                default:
                                    throw new ArgumentOutOfRangeException(f.VertexElementFormat.ToString());
                            }
                        }

                        index++;
                    }
                    builder.AddTriangleVertex(e);
                }
            }
            #endregion

            MeshContent meshContent = builder.FinishMesh();
            MeshHelper.OptimizeForCache(meshContent);

            if (meshContent.Geometry.Count != 1) { throw new ArgumentOutOfRangeException(); }

            VertexBufferContent vb;
            VertexElement[] ve;
            meshContent.Geometry[0].Vertices.CreateVertexBuffer(out vb, out ve, TargetPlatform.Windows);

            #region Checks
            if (meshContent.Geometry[0].Indices.Count != aPart.Indices.Length) { throw new ArgumentOutOfRangeException(); }

            if (aPart.VertexDeclaration.Length != ve.Length) { throw new ArgumentOutOfRangeException(); }
            for (int i = 0; i < ve.Length; i++) { if (aPart.VertexDeclaration[i] != ve[i]) { throw new ArgumentOutOfRangeException(); } }
            #endregion

            meshContent.Geometry[0].Indices.CopyTo(aPart.Indices, 0);
            aPart.VertexCount = vb.VertexData.Length / aPart.VertexStrideInBytes;

            Array.Resize(ref aPart.Vertices, vb.VertexData.Length / sizeof(float));
            for (int i = 0; i < vb.VertexData.Length; i += sizeof(float))
            {
                aPart.Vertices[i / sizeof(float)] = BitConverter.ToSingle(vb.VertexData, i);
            }
        }

        private void _OptimizeMeshes()
        {
            Dictionary<string, SiatMeshContent.Part> processed = new Dictionary<string, SiatMeshContent.Part>();

            foreach (SceneNodeContent e in mScene.Nodes)
            {
                if (e is MeshPartSceneNodeContent)
                {
                    MeshPartSceneNodeContent f = (MeshPartSceneNodeContent)e;

                    if (!processed.ContainsKey(f.MeshPart.Id))
                    {
                        _Optimize(f.MeshPart);
                        processed.Add(f.MeshPart.Id, f.MeshPart);
                    }
                }
            }
        }

        public override SceneContent Process(ColladaCOLLADA aRoot, ContentProcessorContext aContext)
        {
            mBaseName = PipelineUtilities.ExtractXnaAssetName(aRoot.SourceFile) + "_";
            mContent = new ColladaContent(aRoot.SourceFile);
            mContext = aContext;

            _ProcessRoot(aRoot);
            _OptimizeMeshes();

            if (mbProcessPhysics)
            {
                TriangleTree tree = _ProcessPhysics(aRoot);

                if (mScene.Nodes.Count < 1 || mScene.Nodes[0] == null) { throw new ArgumentNullException(); }

                mScene.Nodes[0].ChildrenCount++;
                mScene.Nodes.Insert(1, new PhysicsSceneNodeContent(tree));
            }

            return mScene;
        }

        public ColladaContent Content { get { return mContent; } }

        [DefaultValue(typeof(bool), "false")]
        public bool ProcessPhysics { get { return mbProcessPhysics; } set { mbProcessPhysics = value; } }

        /// <summary>
        /// The magnification filter to use if COLLADA specified filter is "None".
        /// </summary>
        [DefaultValue(typeof(_ColladaElement.Enums.SamplerFilter), "Linear")]
        public _ColladaElement.Enums.SamplerFilter MagFilterWhenNone { get { return mMagFilterWhenNone; } set { mMagFilterWhenNone = value; } }

        /// <summary>
        /// The minification filter to use if COLLADA specified filter is "None".
        /// </summary>
        [DefaultValue(typeof(_ColladaElement.Enums.SamplerFilter), "LinearMipmapLinear")]
        public _ColladaElement.Enums.SamplerFilter MinFilterWhenNone { get { return mMinFilterWhenNone; } set { mMinFilterWhenNone = value; } }

        /// <summary>
        /// The mipmapping filter to use if COLLADA specified filter is "None".
        /// </summary>
        [DefaultValue(typeof(_ColladaElement.Enums.SamplerFilter), "Linear")]
        public _ColladaElement.Enums.SamplerFilter MipFilterWhenNone { get { return mMipFilterWhenNone; } set { mMipFilterWhenNone = value; } }

    }
}
