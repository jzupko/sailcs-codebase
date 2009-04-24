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

using ExternalCompiledEffectReference = Microsoft.Xna.Framework.Content.Pipeline.ExternalReference<Microsoft.Xna.Framework.Graphics.CompiledEffect>;
using ExternalTextureReference = Microsoft.Xna.Framework.Content.Pipeline.ExternalReference<Microsoft.Xna.Framework.Content.Pipeline.Graphics.TextureContent>;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

// Note: this file contains intermediary content representations.
namespace siat.pipeline
{
    /// <summary>
    /// Encapsulates animation at content processing time.
    /// </summary>
    public sealed class AnimationContent
    {
        public AnimationContent(string aId, AnimationKeyFrame[] aKeyFrames)
        {
            Id = aId;
            KeyFrames = aKeyFrames;
        }

        public override bool Equals(object obj)
        {
            if (obj is AnimationContent)
            {
                AnimationContent b = (AnimationContent)obj;
                return (b.Id == Id);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Id;
        }

        public readonly string Id;
        public readonly AnimationKeyFrame[] KeyFrames;
    }

    public struct BoundingFrame
    {
        public BoundingBox BoundingBox;
        public BoundingSphere BoundingSphere;
        public float Time;
    }

    /// <summary>
    /// Encapsulates an effect at content processing time.
    /// </summary>
    public sealed class SiatEffectContent
    {
        private uint mHash;
        public string Id;
        public CompiledEffect CompiledEffect;

        public SiatEffectContent(uint aHash, string aId)
        {
            mHash = aHash;
            Id = aId;
        }

        public override bool Equals(object obj)
        {
            if (obj is SiatEffectContent)
            {
                SiatEffectContent effect = (SiatEffectContent)obj;
                return effect.mHash == mHash;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (int)mHash;
        }

        public override string ToString()
        {
            return CompiledEffect.ToString();
        }
    }

    /// <summary>
    /// Encapsulates a material at content processing time.
    /// </summary>
    /// <remarks>
    /// A material is used to set the parameters of an effect.
    /// </remarks>
    public sealed class SiatMaterialContent
    {
        public sealed class Parameter
        {
            #region Private members
            private uint mHash;
            private string mSemantic;
            private ParameterType mType;
            private object mValue;

            private void _CalculateHash()
            {
                byte[] hashData = Encoding.UTF8.GetBytes(ToString());
                mHash = Hash.Calculate32(hashData, 0u);
            }
            #endregion

            public Parameter(string aSemantic, ParameterType aType, object aValue)
            {
                mSemantic = aSemantic;
                mType = aType;
                mValue = aValue;

                _CalculateHash();
            }

            public override bool Equals(object obj)
            {
                if (obj is Parameter)
                {
                    Parameter param = (Parameter)obj;
                    return (mSemantic.Equals(param.mSemantic)) &&
                        (mType.Equals(param.mType)) &&
                        (mValue.Equals(param.mValue));
                }

                return false;
            }

            public override int GetHashCode()
            {
                return (int)mHash;
            }

            public override string ToString()
            {
                if (Value != null)
                {
                    return Semantic + "_" + Type.ToString() + "_" + Value.ToString();
                }
                else
                {
                    return Semantic + "_" + Type.ToString();
                }
            }

            public string Semantic { get { return mSemantic; } }
            public ParameterType Type { get { return mType; } }
            public object Value { get { return mValue; } }
        }

        public List<Parameter> Parameters = new List<Parameter>();

        public override bool Equals(object obj)
        {
            if (obj is SiatMaterialContent)
            {
                SiatMaterialContent mat = (SiatMaterialContent)obj;

                if (mat.Parameters.Count != Parameters.Count)
                {
                    return false;
                }
                else
                {
                    int count = Parameters.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (!mat.Parameters[i].Equals(Parameters[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }

            return false;
        }

        public override int GetHashCode()
        {
            byte[] hashData = Encoding.UTF8.GetBytes(ToString());
            int hash = (int)Hash.Calculate32(hashData, 0u);
            return hash;
        }

        public override string ToString()
        {
            string ret = string.Empty;
            foreach (Parameter p in Parameters)
            {
                ret += "_" + p.ToString();
            }

            return ret;
        }
    }

    /// <summary>
    /// Encapsulates a mesh (bucket of geometry) at content processing time.
    /// </summary>
    public sealed class SiatMeshContent
    {
        public sealed class Part
        {
            #region Private members
            private void _DoAxisAlignment()
            {
                if (!mbAxisAlignCalculated)
                {
                    List<Vector3> positions;
                    PipelineUtilities.ExtractCompactPositions(this, out positions);

                    Vector3 halfExtents;
                    Vector3 center;
                    Vector3 r;
                    Vector3 s;
                    Vector3 t;

                    Utilities.CalculatePrincipalComponentAxes(positions, out r, out s, out t);
                    Utilities.CalculateCenterAndHalfExtents(positions, ref r, ref s, ref t, out center, out halfExtents);

                    Matrix m;
                    Utilities.CalculateMatrix(ref center, ref r, ref s, ref t, out m);
                    mAxisAlignment = Matrix.Invert(m);

                    PipelineUtilities.ApplyTransform(this, ref mAxisAlignment);

                    if (!Utilities.IsOrthogonal(ref mAxisAlignment)) { throw new ArgumentException("Axis align matrix is not orthogonal."); }
                    if (Utilities.IsReflection(ref mAxisAlignment)) { throw new ArgumentException("Axis align matrix is a reflection matrix."); }

                    mbAxisAlignCalculated = true;
                }
            }

            private Matrix _AxisAlignment
            {
                get
                {
                    _DoAxisAlignment();

                    return mAxisAlignment;
                }
            }

            private bool mbAxisAlignCalculated = false;
            private Matrix mAxisAlignment = Matrix.Identity;
            private int mVertexStrideInBytes;
            #endregion

            public string Id;
            public int[] Indices;
            public string Effect;
            public int PrimitiveCount;
            public PrimitiveType PrimitiveType;
            public int VertexCount;
            public VertexElement[] VertexDeclaration;

            public BoundingBox AABB
            {
                get
                {
                    _DoAxisAlignment();

                    List<Vector3> positions;
                    PipelineUtilities.ExtractCompactPositions(this, out positions);

                    return BoundingBox.CreateFromPoints(positions);
                }
            }

            public BoundingSphere BoundingSphere
            {
                get
                {
                    List<Vector3> positions;
                    PipelineUtilities.ExtractCompactPositions(this, out positions);

                    return BoundingSphere.CreateFromPoints(positions);
                }
            }

            public Matrix UndoAxisAlignment
            {
                get
                {
                    return Matrix.Invert(_AxisAlignment);
                }
            }

            public int VertexStrideInBytes
            {
                get
                {
                    return mVertexStrideInBytes;
                }

                set
                {
                    mVertexStrideInBytes = value;
                }
            }

            public int VertexStrideInSingles
            {
                get
                {
                    return mVertexStrideInBytes / sizeof(float);
                }

                set
                {
                    mVertexStrideInBytes = (value * sizeof(float));
                }
            }

            public float[] Vertices;
        }

        public List<Part> Parts = new List<Part>();
    }

    /// <summary>
    /// Encapsultes a portal at content processing time.
    /// </summary>
    public sealed class PortalContent
    {
        #region Private members
        private void _Validate(SiatMeshContent aMesh)
        {
            foreach (SiatMeshContent.Part e in aMesh.Parts)
            {
                bool bFoundNormals = false;
                bool bFoundPositions = false;

                foreach (VertexElement f in e.VertexDeclaration)
                {
                    if (f.VertexElementUsage == VertexElementUsage.Position)
                    {
                        bFoundPositions = true;
                    }
                    else if (f.VertexElementUsage == VertexElementUsage.Normal)
                    {
                        bFoundNormals = true;
                    }

                    if (bFoundNormals && bFoundPositions)
                    {
                        break;
                    }
                }

                if (!(bFoundPositions && bFoundNormals))
                {
                    throw new Exception("The input mesh for a portal muts contain vertices with normal and position semantics.");
                }
            }
        }

        private void _GatherNormalsAndPositions(SiatMeshContent aMesh, out Vector3[] arNormals, out Vector3[] arPositions)
        {
            #region Initialize return arrays
            int totalVertices = 0;
            foreach (SiatMeshContent.Part e in aMesh.Parts)
            {
                totalVertices += e.VertexCount;
            }
            arNormals = new Vector3[totalVertices];
            arPositions = new Vector3[totalVertices];
            #endregion

            int index = 0;
            foreach (SiatMeshContent.Part e in aMesh.Parts)
            {
                int normalIndex = -1;
                int positionIndex = -1;

                foreach (VertexElement f in e.VertexDeclaration)
                {
                    if (f.VertexElementUsage == VertexElementUsage.Normal && normalIndex == -1)
                    {
                        normalIndex = f.Offset / sizeof(float);
                    }
                    else if (f.VertexElementUsage == VertexElementUsage.Position && positionIndex == -1)
                    {
                        positionIndex = f.Offset / sizeof(float);
                    }

                    if (normalIndex != -1 && positionIndex != -1)
                    {
                        break;
                    }
                }

                int stride = e.VertexStrideInSingles;
                int count = e.VertexCount * stride;

                for (int i = 0; i < count; i += stride)
                {
                    int n0 = i + normalIndex + 0;
                    int n1 = i + normalIndex + 1;
                    int n2 = i + normalIndex + 2;

                    int p0 = i + positionIndex + 0;
                    int p1 = i + positionIndex + 1;
                    int p2 = i + positionIndex + 2;

                    arNormals[index] = new Vector3(e.Vertices[n0],
                                                   e.Vertices[n1],
                                                   e.Vertices[n2]);
                    arPositions[index] = new Vector3(e.Vertices[p0],
                                                     e.Vertices[p1],
                                                     e.Vertices[p2]);
                    index++;
                }
            }
        }

        private static Vector3 _DeriveNormal(Vector3[] aNormals)
        {
            Vector3 ret = Utilities.Mean(aNormals);

            if (Utilities.AboutZero(ref ret, kMeanNormalTolerance))
            {
                ret = Vector3.Forward;
            }

            return ret;
        }

        private static Vector3[] _DerivePositions(Winding aWinding, ref Vector3 aNormal, Vector3[] aPositions)
        {
            BoundingBox box = BoundingBox.CreateFromPoints(aPositions);
            Vector3 center = Utilities.GetCenter(ref box);
            Plane plane = new Plane(aNormal, 0.0f);
            plane.D = -plane.DotCoordinate(center);

            Matrix m = Matrix.CreateFromAxisAngle(Vector3.Cross(Vector3.Backward, aNormal), Utilities.SmallestAngle(Vector3.Backward, aNormal));

            Vector3 left = Vector3.TransformNormal(Vector3.Left, m);
            Vector3 right = Vector3.TransformNormal(Vector3.Right, m);
            Vector3 down = Vector3.TransformNormal(Vector3.Down, m);
            Vector3 up = Vector3.TransformNormal(Vector3.Up, m);

            float leftMin = Vector3.Dot(left, box.Min);
            float leftMax = Vector3.Dot(left, box.Max);
            float rightMin = Vector3.Dot(right, box.Min);
            float rightMax = Vector3.Dot(right, box.Max);
            float downMin = Vector3.Dot(down, box.Min);
            float downMax = Vector3.Dot(down, box.Max);
            float upMin = Vector3.Dot(up, box.Min);
            float upMax = Vector3.Dot(up, box.Max);

            left *= Utilities.GreaterThan(leftMin, 0.0f) ? leftMin : leftMax;
            right *= Utilities.GreaterThan(rightMin, 0.0f) ? rightMin : rightMax;
            down *= Utilities.GreaterThan(downMin, 0.0f) ? downMin : downMax;
            up *= Utilities.GreaterThan(upMin, 0.0f) ? upMin : upMax;

            Vector3[] ret = new Vector3[4];
            if (aWinding == Winding.Clockwise)
            {
                ret[0] = left + down;
                ret[1] = left + up;
                ret[2] = right + up;
                ret[3] = right + down;
            }
            else
            {
                ret[0] = left + down;
                ret[1] = right + down;
                ret[2] = right + up;
                ret[3] = left + up;
            }

            return ret;
        }

        private void _Process(SiatMeshContent aMesh)
        {
            _Validate(aMesh);

            Vector3[] normals;
            Vector3[] positions;
            _GatherNormalsAndPositions(aMesh, out normals, out positions);

            Normal = _DeriveNormal(normals);
            Positions = _DerivePositions(Utilities.kWinding, ref Normal, positions);
        }
        #endregion

        public const float kMeanNormalTolerance = 1e-3f;
        public Vector3 Normal;
        public Vector3[] Positions;

        public PortalContent(SiatMeshContent aMesh)
        {
            _Process(aMesh);
        }
    }

    #region Scene
    public sealed class AnimatedMeshPartSceneNodeContent : SceneNodeContent
    {
        public AnimatedMeshPartSceneNodeContent(string aId, int aChildrenCount, ref Matrix aLocalTransform,
            SiatEffectContent aEffect, SiatMaterialContent aMaterial, SiatMeshContent.Part aMeshPart,
            ref Matrix aBindMatrix, Matrix[] aInverseBindTransforms, string aRootJoint, string[] aJoints)
            : base(aId, aChildrenCount, ref aLocalTransform, SceneNodeType.AnimatedMeshPart)
        {
            Effect = aEffect;
            Material = aMaterial;
            MeshPart = aMeshPart;
            BindMatrix = aBindMatrix;
            InverseBindTransforms = aInverseBindTransforms;
            RootJoint = aRootJoint;
            Joints = aJoints;
        }

        public readonly SiatEffectContent Effect;
        public readonly SiatMaterialContent Material;
        public readonly SiatMeshContent.Part MeshPart;
        public readonly Matrix BindMatrix;
        public readonly Matrix[] InverseBindTransforms;
        public readonly string RootJoint;
        public readonly string[] Joints;
    }

    public sealed class DirectionalLightSceneNodeContent : SceneNodeContent
    {
        public DirectionalLightSceneNodeContent(string aId, int aChildrenCount, ref Matrix aLocalTransform, Vector3 aLightColor)
            : base(aId, aChildrenCount, ref aLocalTransform, SceneNodeType.DirectionalLight)
        {
            LightDiffuse = aLightColor;
            LightSpecular = aLightColor;
        }

        public readonly Vector3 LightDiffuse;
        public readonly Vector3 LightSpecular;
    }

    public sealed class JointSceneNodeContent : SceneNodeContent
    {
        public JointSceneNodeContent(string aId, int aChildrenCount, ref Matrix aLocalTransform, AnimationContent aAnimation)
            : base(aId, aChildrenCount, ref aLocalTransform, SceneNodeType.Joint)
        {
            Animation = aAnimation;
        }

        public readonly AnimationContent Animation;
    }

    public sealed class MeshSceneNodeContent : SceneNodeContent
    {
        public MeshSceneNodeContent(string aId, int aChildrenCount, ref Matrix aLocalTransform, SiatMeshContent aMesh)
            : base(aId, aChildrenCount, ref aLocalTransform, SceneNodeType.Mesh)
        {
            Mesh = aMesh;
        }

        public readonly SiatMeshContent Mesh;
    }

    public sealed class MeshPartSceneNodeContent : SceneNodeContent
    {
        public MeshPartSceneNodeContent(string aId, int aChildrenCount, ref Matrix aLocalTransform, ref Matrix aWorldTransform, SiatEffectContent aEffect, SiatMaterialContent aMaterial, SiatMeshContent.Part aMeshPart)
            : base(aId, aChildrenCount, ref aLocalTransform,  SceneNodeType.MeshPart)
        {
            Effect = aEffect;
            Material = aMaterial;
            MeshPart = aMeshPart;
            WorldTransform = aWorldTransform;
        }

        public readonly SiatEffectContent Effect;
        public readonly SiatMaterialContent Material;
        public readonly SiatMeshContent.Part MeshPart;
        public readonly Matrix WorldTransform;
    }

    public sealed class PhysicsSceneNodeContent : SceneNodeContent
    {
        public PhysicsSceneNodeContent(TriangleTree aTree)
            : base("physics_world", 0, ref Utilities.kIdentity, SceneNodeType.Physics)
        {
            Tree = aTree;
        }

        public readonly TriangleTree Tree;
    }

    public sealed class PointLightSceneNodeContent : SceneNodeContent
    {
        public PointLightSceneNodeContent(string aId, int aChildrenCount, ref Matrix aLocalTransform, Vector3 aLightColor, Vector3 aAttenuation)
            : base(aId, aChildrenCount, ref aLocalTransform, SceneNodeType.PointLight)
        {
            LightAttenuation = aAttenuation;
            LightDiffuse = aLightColor;
            LightSpecular = aLightColor;
        }

        public readonly Vector3 LightAttenuation;
        public readonly Vector3 LightDiffuse;
        public readonly Vector3 LightSpecular;
    }

    public sealed class PortalSceneNodeContent : SceneNodeContent
    {
        public PortalSceneNodeContent(string aId, int aChildrenCount, ref Matrix aLocalTransform, PortalContent aPortal, string aPortalTo)
            : base(aId, aChildrenCount, ref aLocalTransform, SceneNodeType.Portal)
        {
            Portal = aPortal;
            PortalTo = aPortalTo;
        }

        public readonly PortalContent Portal;
        public readonly string PortalTo;
    }

    public class SceneNodeContent
    {
        #region Protected members
        protected SceneNodeContent(string aId, int aChildrenCount, ref Matrix aLocalTransform, SceneNodeType aType)
        {
            Id = aId;
            ChildrenCount = aChildrenCount;
            LocalTransform = aLocalTransform;
            Type = aType;
        }
        #endregion

        public SceneNodeContent(string aId, int aChildrenCount, ref Matrix aLocalTransform)
            : this(aId, aChildrenCount, ref aLocalTransform, SceneNodeType.Node)
        { }

        public int ChildrenCount;
        public string Id;
        public Matrix LocalTransform;
        public SceneNodeType Type;
    }

    public class SkySceneNodeContent : SceneNodeContent
    {
        public SkySceneNodeContent(string aId, int aChildrenCount, ref Matrix aLocalTransform, SiatEffectContent aEffect, SiatMaterialContent aMaterial, SiatMeshContent.Part aMeshPart)
            : base(aId, aChildrenCount, ref aLocalTransform,  SceneNodeType.Sky)
        {
            Effect = aEffect;
            Material = aMaterial;
            MeshPart = aMeshPart;
        }

        public readonly SiatEffectContent Effect;
        public readonly SiatMaterialContent Material;
        public readonly SiatMeshContent.Part MeshPart;
    }

    public sealed class SpotLightSceneNodeContent : SceneNodeContent
    {
        public SpotLightSceneNodeContent(string aId, int aChildrenCount, ref Matrix aLocalTransform, Vector3 aLightColor, Vector3 aAttenuation, float aFalloffAngleInRadians, float aFalloffExponent)
            : base(aId, aChildrenCount, ref aLocalTransform, SceneNodeType.SpotLight)
        {
            FalloffAngleInRadians = aFalloffAngleInRadians;
            FalloffExponent = aFalloffExponent;
            LightAttenuation = aAttenuation;
            LightDiffuse = aLightColor;
            LightSpecular = aLightColor;
        }

        public readonly float FalloffAngleInRadians;
        public readonly float FalloffExponent;
        public readonly Vector3 LightAttenuation;
        public readonly Vector3 LightDiffuse;
        public readonly Vector3 LightSpecular;
    }

    public sealed class SceneContent
    {
        public List<SceneNodeContent> Nodes = new List<SceneNodeContent>();
    }
    #endregion
}
