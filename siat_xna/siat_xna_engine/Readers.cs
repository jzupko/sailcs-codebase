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
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using siat.render;
using siat.scene;

using jz;
using jz.physics;
using jz.physics.narrowphase;

namespace siat
{
    public sealed class AnimationKeyFrameReader : ContentTypeReader<AnimationKeyFrame>
    {
        protected override AnimationKeyFrame Read(ContentReader aIn, AnimationKeyFrame aExistingInstance)
        {
            AnimationKeyFrame ret = new AnimationKeyFrame(aIn.ReadSingle(), aIn.ReadMatrix());
            return ret;
        }
    }

    public sealed class AnimationReader : ContentTypeReader<Animation>
    {
        protected override Animation Read(ContentReader aIn, Animation aExistingInstance)
        {
            Animation ret = new Animation(aIn.ReadString());
            ret.KeyFrames = aIn.ReadObject<AnimationKeyFrame[]>();

            return ret;
        }
    }

    public sealed class EffectReader : ContentTypeReader<SiatEffect>
    {
        protected override SiatEffect Read(ContentReader aIn, SiatEffect aExistingInstance)
        {
            SiatEffect ret = new SiatEffect(aIn.ReadString(), aIn.ReadObject<Effect>());
            return ret;
        }
    }

    public sealed class MaterialReader : ContentTypeReader<SiatMaterial>
    {
        protected override SiatMaterial Read(ContentReader aIn, SiatMaterial aExistingInstance)
        {
            SiatMaterial ret = new SiatMaterial();
            int count = aIn.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string semantic = aIn.ReadString();
                ParameterType type = aIn.ReadObject<ParameterType>();
                switch (type)
                {
                    case ParameterType.kSingle:
                        ret.AddParameter(semantic, aIn.ReadSingle());
                        break;
                    case ParameterType.kMatrix:
                        ret.AddParameter(semantic, aIn.ReadMatrix());
                        break;
                    case ParameterType.kTexture:
                        ret.AddParameter(semantic, aIn.ReadExternalReference<Texture>());
                        break;
                    case ParameterType.kVector2:
                        ret.AddParameter(semantic, aIn.ReadVector2());
                        break;
                    case ParameterType.kVector3:
                        ret.AddParameter(semantic, aIn.ReadVector3());
                        break;
                    case ParameterType.kVector4:
                        ret.AddParameter(semantic, aIn.ReadVector4());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return ret;
        }
    }

    public sealed class MeshPartReader : ContentTypeReader<MeshPart>
    {
        protected override MeshPart Read(ContentReader aIn, MeshPart aExistingInstance)
        {
            string id = aIn.ReadString();
            MeshPart ret = new MeshPart(id);
            ret.Indices = aIn.ReadObject<IndexBuffer>();
            ret.AABB = aIn.ReadObject<BoundingBox>();
            ret.BoundingSphere = aIn.ReadObject<BoundingSphere>();
            ret.PrimitiveCount = aIn.ReadInt32();
            ret.PrimitiveType = aIn.ReadObject<PrimitiveType>();
            ret.VertexCount = aIn.ReadInt32();
            aIn.ReadSharedResource<VertexDeclaration>(delegate(VertexDeclaration e) { ret.VertexDeclaration = e; });
            ret.VertexStride = aIn.ReadInt32();
            ret.Vertices = aIn.ReadObject<VertexBuffer>();

            return ret;
        }
    }

    public sealed class PortalReader : ContentTypeReader<Portal>
    {
        protected override Portal Read(ContentReader aIn, Portal aExistingInstance)
        {
            Portal portal = new Portal();
            portal.LocalNormal = aIn.ReadVector3();
            portal.LocalPositions = aIn.ReadObject<Vector3[]>();

            return portal;
        }
    }

    public sealed class SceneReader : ContentTypeReader<SceneNode>
    {
        #region Private members
        private SceneNode _ReadAnimatedMeshPart(ContentReader aIn, SceneNode aNode, bool abAlreadyExists)
        {
            AnimatedMeshPartNode ret = (abAlreadyExists) ? (AnimatedMeshPartNode)aNode : new AnimatedMeshPartNode();

            aIn.ReadSharedResource<SiatEffect>(delegate(SiatEffect a) { ret.Effect = a; });
            aIn.ReadSharedResource<SiatMaterial>(delegate(SiatMaterial a) { ret.Material = a; });
            aIn.ReadSharedResource<MeshPart>(delegate(MeshPart a) { ret.MeshPart = a; });

            if (!abAlreadyExists)
            {
                ret.BindTransform = aIn.ReadMatrix();
                ret.InvJointBindTransforms = aIn.ReadObject<Matrix[]>();
                ret.RootJointId = aIn.ReadString();
                ret.JointIds = aIn.ReadObject<string[]>();
            }
            else
            {
                aIn.ReadMatrix();
                aIn.ReadObject<Matrix[]>();
                aIn.ReadString();
                aIn.ReadObject<string[]>();
            }

            return ret;
        }

        private SceneNode _ReadCamera(ContentReader aIn, SceneNode aNode, bool abAlreadyExists)
        {
            throw new Exception(Utilities.kNotImplemented);
        }

        private SceneNode _ReadDirectionalLight(ContentReader aIn, SceneNode aNode, bool abAlreadyExists)
        {
            Light light = new Light();
            light.LightDiffuse = aIn.ReadVector3();
            light.LightSpecular = aIn.ReadVector3();
            light.Type = LightType.Directional;

            if (!abAlreadyExists)
            {
                LightNode ret = new LightNode();
                ret.Light = light;
                return ret;
            }
            else
            {
                return aNode;
            }
        }

        private SceneNode _ReadJoint(ContentReader aIn, SceneNode aNode, bool abAlreadyExists)
        {
            if (!abAlreadyExists)
            {
                JointNode joint = new JointNode();
                joint.Animation = aIn.ReadObject<Animation>();

                return joint;
            }
            else
            {
                aIn.ReadObject<Animation>();

                return aNode;
            }
        }

        private SceneNode _ReadMesh(ContentReader aIn, SceneNode aNode, bool abAlreadyExists)
        {
            if (!abAlreadyExists)
            {
                MeshNode ret = new MeshNode();

                return ret;
            }
            else
            {
                return aNode;
            }
        }

        private SceneNode _ReadMeshPart(ContentReader aIn, SceneNode aNode, bool abAlreadyExists)
        {
            MeshPartNode ret = (abAlreadyExists) ? (MeshPartNode)aNode : new MeshPartNode();

            aIn.ReadSharedResource<SiatEffect>(delegate(SiatEffect a) { ret.Effect = a; });
            aIn.ReadSharedResource<SiatMaterial>(delegate(SiatMaterial a) { ret.Material = a; });
            aIn.ReadSharedResource<MeshPart>(delegate(MeshPart a) { ret.MeshPart = a; });

            return ret;
        }

        private SceneNode _ReadPhysics(ContentReader aIn, SceneNode aNode, bool abAlreadyExists)
        {
            WorldTree tree = new WorldTree();
            tree.Read(aIn);

            if (!abAlreadyExists)
            {
                PhysicsSceneNode ret = new PhysicsSceneNode(tree);

                return ret;
            }
            else
            {
                return aNode;
            }
        }

        private SceneNode _ReadPointLight(ContentReader aIn, SceneNode aNode, bool abAlreadyExists)
        {
            Light light = new Light();

            Vector3 attenuation = aIn.ReadVector3();
            Vector3 diffuse = aIn.ReadVector3();
            Vector3 specular = aIn.ReadVector3();

            light.LightAttenuation = attenuation;
            light.LightDiffuse = diffuse;
            light.LightSpecular = specular;
            light.Type = LightType.Point;

            if (!abAlreadyExists)
            {
                LightNode ret = new LightNode();
                ret.Light = light;
                return ret;
            }
            else
            {
                return aNode;
            }
        }

        private SceneNode _ReadPortal(ContentReader aIn, SceneNode aNode, bool abAlreadyExists)
        {
            PortalNode ret = (abAlreadyExists) ? (PortalNode)aNode : new PortalNode();

            aIn.ReadSharedResource<Portal>(delegate(Portal a) { ret.Portal = a; });
            ret.PortalTo = aIn.ReadString();

            return ret;
        }

        private void _ReadSceneNode(ContentReader aIn, SceneNode aParent)
        {
            SceneNode node = _ReadSceneNode(aIn);
            node.Parent = aParent;
        }

        private SceneNode _ReadSceneNode(ContentReader aIn)
        {
            int childrenCount = aIn.ReadInt32();
            string id = aIn.ReadString();
            Matrix localTransform = aIn.ReadMatrix();
            SceneNodeType type = aIn.ReadObject<SceneNodeType>();

            SceneNode node = SceneNode.Find<SceneNode>(id);
            bool bAlreadyExists = (node != null);

            switch (type)
            {
                case SceneNodeType.AnimatedMeshPart: node = _ReadAnimatedMeshPart(aIn, node, bAlreadyExists); break;
                case SceneNodeType.Camera: node = _ReadCamera(aIn, node, bAlreadyExists); break;
                case SceneNodeType.DirectionalLight: node = _ReadDirectionalLight(aIn, node, bAlreadyExists); break;
                case SceneNodeType.Joint: node = _ReadJoint(aIn, node, bAlreadyExists); break;
                case SceneNodeType.Mesh: node = _ReadMesh(aIn, node, bAlreadyExists); break;
                case SceneNodeType.MeshPart: node = _ReadMeshPart(aIn, node, bAlreadyExists); break;
                case SceneNodeType.Node: if (!bAlreadyExists) { node = new SceneNode(); } break;
                case SceneNodeType.Physics: node = _ReadPhysics(aIn, node, bAlreadyExists); break;
                case SceneNodeType.PointLight: node = _ReadPointLight(aIn, node, bAlreadyExists); break;
                case SceneNodeType.Portal: node = _ReadPortal(aIn, node, bAlreadyExists); break;
                case SceneNodeType.SpotLight: node = _ReadSpotLight(aIn, node, bAlreadyExists); break;
                case SceneNodeType.Sky: node = _ReadSky(aIn, node, bAlreadyExists); break;
                default:
                    throw new Exception(Utilities.kShouldNotBeHere);
            }

            if (!bAlreadyExists)
            {
                node.Id = id;
                node.LocalTransform = localTransform;
            }

            for (int i = 0; i < childrenCount; i++)
            {
                _ReadSceneNode(aIn, node);
            }

            return node;
        }

        private SceneNode _ReadSky(ContentReader aIn, SceneNode aNode, bool abAlreadyExists)
        {
            SkyNode ret = (abAlreadyExists) ? (SkyNode)aNode : new SkyNode();

            aIn.ReadSharedResource<SiatEffect>(delegate(SiatEffect a) { ret.Effect = a; });
            aIn.ReadSharedResource<SiatMaterial>(delegate(SiatMaterial a) { ret.Material = a; });
            aIn.ReadSharedResource<MeshPart>(delegate(MeshPart a) { ret.MeshPart = a; });

            return ret;
        }

        private SceneNode _ReadSpotLight(ContentReader aIn, SceneNode aNode, bool abAlreadyExists)
        {
            Light light = new Light();

            light.FalloffAngleInRadians = aIn.ReadSingle();
            light.FalloffExponent = aIn.ReadSingle();

            Vector3 attenuation = aIn.ReadVector3();
            Vector3 diffuse = aIn.ReadVector3();
            Vector3 specular = aIn.ReadVector3();

            light.LightAttenuation = attenuation;
            light.LightDiffuse = diffuse;
            light.LightSpecular = specular;
            light.Type = LightType.Spot;

            if (!abAlreadyExists)
            {
                LightNode ret = new LightNode();
                ret.Light = light;
                ret.bCastShadow = true;
                return ret;
            }
            else
            {
                return aNode;
            }
        }
        #endregion

        protected override SceneNode Read(ContentReader aIn, SceneNode aExistingInstance)
        {
            SceneNode ret = new SceneNode();
            _ReadSceneNode(aIn, ret);

            return ret;
        }
    }
}
