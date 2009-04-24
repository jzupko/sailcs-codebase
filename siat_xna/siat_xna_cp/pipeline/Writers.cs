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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace siat.pipeline
{
    [ContentTypeWriter]
    public sealed class AnimationKeyFrameWriter : ContentTypeWriter<AnimationKeyFrame>
    {
        protected override void Write(ContentWriter aOut, AnimationKeyFrame aKeyFrame)
        {
            aOut.Write(aKeyFrame.Time);
            aOut.Write(aKeyFrame.Key);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "siat.AnimationKeyFrameReader, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return "siat.AnimationKeyFrame, siat, Version=1.0.0.0, Culture=neutral";
        }
    }

    [ContentTypeWriter]
    public sealed class AnimationWriter : ContentTypeWriter<AnimationContent>
    {
        protected override void Write(ContentWriter aOut, AnimationContent aAnimation)
        {
            aOut.Write(aAnimation.Id);
            aOut.WriteObject(aAnimation.KeyFrames);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "siat.AnimationReader, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return "siat.render.Animation, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }
    }

    [ContentTypeWriter]
    public sealed class EffectWriter : ContentTypeWriter<SiatEffectContent>
    {
        protected override void Write(ContentWriter aOut, SiatEffectContent aEffect)
        {
            aOut.Write(aEffect.Id);
            aOut.WriteObject<CompiledEffect>(aEffect.CompiledEffect);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "siat.EffectReader, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return "siat.render.SiatEffect, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }
    }

    [ContentTypeWriter]
    public sealed class MaterialWriter : ContentTypeWriter<SiatMaterialContent>
    {
        protected override void Write(ContentWriter aOut, SiatMaterialContent aMaterial)
        {
            aOut.Write(aMaterial.Parameters.Count);
            foreach (SiatMaterialContent.Parameter p in aMaterial.Parameters)
            {
                aOut.Write(p.Semantic);
                aOut.WriteObject<ParameterType>(p.Type);
                switch (p.Type)
                {
                    case ParameterType.kSingle:
                        aOut.Write((float)p.Value);
                        break;
                    case ParameterType.kMatrix:
                        aOut.Write((Matrix)p.Value);
                        break;
                    case ParameterType.kTexture:
                        aOut.WriteExternalReference<TextureContent>((ExternalReference<TextureContent>)p.Value);
                        break;
                    case ParameterType.kVector2:
                        aOut.Write((Vector2)p.Value);
                        break;
                    case ParameterType.kVector3:
                        aOut.Write((Vector3)p.Value);
                        break;
                    case ParameterType.kVector4:
                        aOut.Write((Vector4)p.Value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "siat.MaterialReader, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return "siat.render.SiatMaterial, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }
    }

    [ContentTypeWriter]
    public sealed class MeshPartWriter : ContentTypeWriter<SiatMeshContent.Part>
    {
        protected override void Write(ContentWriter aOut, SiatMeshContent.Part aIn)
        {
            IndexCollection indices = new IndexCollection();
            indices.AddRange(aIn.Indices);
            VertexBufferContent vertices = new VertexBufferContent();
            vertices.Write<float>(0, sizeof(float), aIn.Vertices);

            aOut.Write(aIn.Id);
            aOut.WriteObject<IndexCollection>(indices);
            aOut.WriteObject<BoundingBox>(aIn.AABB);
            aOut.WriteObject<BoundingSphere>(aIn.BoundingSphere);
            // Note: Material is not written - used as a content import helper only.
            aOut.Write(aIn.PrimitiveCount);
            aOut.WriteObject<PrimitiveType>(aIn.PrimitiveType);
            aOut.Write(aIn.VertexCount);
            aOut.WriteSharedResource<VertexElement[]>(aIn.VertexDeclaration);
            aOut.Write(aIn.VertexStrideInBytes);
            aOut.WriteObject<VertexBufferContent>(vertices);
        }

        public override string GetRuntimeReader(TargetPlatform aTargetPlatform)
        {
            return "siat.MeshPartReader, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }

        public override string GetRuntimeType(TargetPlatform aTargetPlatform)
        {
            return "siat.render.MeshPart, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }
    }

    [ContentTypeWriter]
    public sealed class PortalWriter : ContentTypeWriter<PortalContent>
    {
        protected override void Write(ContentWriter aOut, PortalContent aContent)
        {
            aOut.Write(aContent.Normal);
            aOut.WriteObject<Vector3[]>(aContent.Positions);
        }

        public override string GetRuntimeReader(TargetPlatform aTargetPlatform)
        {
            return "siat.PortalReader, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }

        public override string GetRuntimeType(TargetPlatform aTargetPlatform)
        {
            return "siat.scene.Portal, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }
    }

    [ContentTypeWriter]
    public sealed class SceneWriter : ContentTypeWriter<SceneContent>
    {
        #region Private members
        private void _WriteAnimatedMeshPart(ContentWriter aOut, AnimatedMeshPartSceneNodeContent aNode)
        {
            _WriteSceneNode(aOut, aNode);
            aOut.WriteSharedResource<SiatEffectContent>(aNode.Effect);
            aOut.WriteSharedResource<SiatMaterialContent>(aNode.Material);
            aOut.WriteSharedResource<SiatMeshContent.Part>(aNode.MeshPart);
            aOut.Write(aNode.BindMatrix);
            aOut.WriteObject<Matrix[]>(aNode.InverseBindTransforms);
            aOut.Write(aNode.RootJoint);
            aOut.WriteObject<string[]>(aNode.Joints);
        }

        private void _WriteDirectionalLight(ContentWriter aOut, DirectionalLightSceneNodeContent aNode)
        {
            _WriteSceneNode(aOut, aNode);
            aOut.Write(aNode.LightDiffuse);
            aOut.Write(aNode.LightSpecular);
        }

        private void _WriteJoint(ContentWriter aOut, JointSceneNodeContent aNode)
        {
            _WriteSceneNode(aOut, aNode);
            aOut.WriteObject<AnimationContent>(aNode.Animation);
        }

        private void _WriteMesh(ContentWriter aOut, MeshSceneNodeContent aNode)
        {
            _WriteSceneNode(aOut, aNode);
        }

        private void _WriteMeshPart(ContentWriter aOut, MeshPartSceneNodeContent aNode)
        {
            // Calculate here to defer it until all combining and mesh processing is finished.
            aNode.LocalTransform = aNode.MeshPart.UndoAxisAlignment * aNode.LocalTransform;

            _WriteSceneNode(aOut, aNode);
            aOut.WriteSharedResource<SiatEffectContent>(aNode.Effect);
            aOut.WriteSharedResource<SiatMaterialContent>(aNode.Material);
            aOut.WriteSharedResource<SiatMeshContent.Part>(aNode.MeshPart);
        }

        private void _WritePhysics(ContentWriter aOut, PhysicsSceneNodeContent aNode)
        {
            _WriteSceneNode(aOut, aNode);
            aNode.Tree.Write(aOut);
        }

        private void _WritePointLight(ContentWriter aOut, PointLightSceneNodeContent aNode)
        {
            _WriteSceneNode(aOut, aNode);
            aOut.Write(aNode.LightAttenuation);
            aOut.Write(aNode.LightDiffuse);
            aOut.Write(aNode.LightSpecular);
        }

        private void _WritePortal(ContentWriter aOut, PortalSceneNodeContent aNode)
        {
            _WriteSceneNode(aOut, aNode);
            aOut.WriteSharedResource<PortalContent>(aNode.Portal);
            aOut.Write(aNode.PortalTo);
        }

        private void _WriteSceneNode(ContentWriter aOut, SceneNodeContent aNode)
        {
            aOut.Write(aNode.ChildrenCount);
            aOut.Write(aNode.Id);
            aOut.Write(aNode.LocalTransform);
            aOut.WriteObject<SceneNodeType>(aNode.Type);
        }

        private void _WriteSky(ContentWriter aOut, SkySceneNodeContent aNode)
        {
            _WriteSceneNode(aOut, aNode);
            aOut.WriteSharedResource<SiatEffectContent>(aNode.Effect);
            aOut.WriteSharedResource<SiatMaterialContent>(aNode.Material);
            aOut.WriteSharedResource<SiatMeshContent.Part>(aNode.MeshPart);
        }

        private void _WriteSpotLight(ContentWriter aOut, SpotLightSceneNodeContent aNode)
        {
            _WriteSceneNode(aOut, aNode);
            aOut.Write(aNode.FalloffAngleInRadians);
            aOut.Write(aNode.FalloffExponent);
            aOut.Write(aNode.LightAttenuation);
            aOut.Write(aNode.LightDiffuse);
            aOut.Write(aNode.LightSpecular);
        }
        #endregion

        #region Protected members
        protected override void Write(ContentWriter aOut, SceneContent aContent)
        {
            foreach (SceneNodeContent e in aContent.Nodes)
            {
                switch (e.Type)
                {
                    case SceneNodeType.AnimatedMeshPart: _WriteAnimatedMeshPart(aOut, (AnimatedMeshPartSceneNodeContent)e); break;
                    case SceneNodeType.DirectionalLight: _WriteDirectionalLight(aOut, (DirectionalLightSceneNodeContent)e); break;
                    case SceneNodeType.Joint: _WriteJoint(aOut, (JointSceneNodeContent)e); break;
                    case SceneNodeType.Mesh: _WriteMesh(aOut, (MeshSceneNodeContent)e); break;
                    case SceneNodeType.MeshPart: _WriteMeshPart(aOut, (MeshPartSceneNodeContent)e); break;
                    case SceneNodeType.Node: _WriteSceneNode(aOut, e); break;
                    case SceneNodeType.Physics: _WritePhysics(aOut, (PhysicsSceneNodeContent)e); break;
                    case SceneNodeType.PointLight: _WritePointLight(aOut, (PointLightSceneNodeContent)e); break;
                    case SceneNodeType.Portal: _WritePortal(aOut, (PortalSceneNodeContent)e); break;
                    case SceneNodeType.SpotLight: _WriteSpotLight(aOut, (SpotLightSceneNodeContent)e); break;
                    case SceneNodeType.Sky: _WriteSky(aOut, (SkySceneNodeContent)e); break;
                    default:
                        throw new Exception(Utilities.kShouldNotBeHere);
                }
            }
        }
        #endregion

        public override string GetRuntimeReader(TargetPlatform aTargetPlatform)
        {
            return "siat.SceneReader, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }

        public override string GetRuntimeType(TargetPlatform aTargetPlatform)
        {
            return "siat.scene.SceneNode, siat_xna_engine, Version=1.0.0.0, Culture=neutral";
        }
    }
}
