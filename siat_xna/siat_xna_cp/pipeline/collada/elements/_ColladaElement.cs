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
using System;
using System.Xml;
using System.Collections.Generic;
using siat.pipeline.collada.elements.fx;

namespace siat.pipeline.collada.elements
{
    /// <summary>
    /// Base element of classes that encapsulate COLLADA XML elements.
    /// </summary>
    /// <remarks>
    /// _ColladaElement is a bit of a fat class to centralize functions used
    /// by all element objects. 
    /// </remarks>
    public abstract class _ColladaElement : TreeNode<_ColladaElement>
    {
        public static readonly string[] kColladaSupportedVersions = { "1.4.0", "1.4.1" };

        #region Private mebmers
        private void _ThrowGetRequiredException<T>()
        {
            string identifier = string.Empty;

            if (this is _ColladaElementWithId) { identifier = ((_ColladaElementWithId)this).Id; }
            else if (this is _ColladaElementWithSid) { identifier = ((_ColladaElementWithSid)this).Sid; }
            else if (this is ColladaNode) { identifier = ((ColladaNode)this).Sid; }
            else if (this is _ColladaTechniqueFX) { identifier = ((_ColladaTechniqueFX)this).Sid; }

            string exceptionMessage = "COLLADA element \"" + identifier + "\" " +
                "of type \"" + GetType().Name + "\" does not have a required child of type \"" +
                typeof(T).Name + "\".";

            throw new Exception(exceptionMessage);
        }
        #endregion

        #region Protected members
        protected delegate _ColladaElement ColladaNodeSpawn();
        protected delegate _ColladaElement ColladaNodeSpawnByName(XmlReader aReader);

        protected void _NextElementOrText(XmlReader aReader)
        {
            while (aReader.Read() && !(aReader.NodeType == XmlNodeType.Element || aReader.NodeType == XmlNodeType.Text)) ;
        }

        protected void _NextElement(XmlReader aReader)
        {
            while (aReader.Read() && aReader.NodeType != XmlNodeType.Element);
        }

        protected void _NextText(XmlReader aReader)
        {
            while (aReader.Read() && aReader.NodeType != XmlNodeType.Text) ;
        }

        protected XmlReader _Sub(XmlReader aReader)
        {
            XmlReader ret = aReader.ReadSubtree();
            ret.Read();

            return ret;
        }

        protected void _CheckSubFinished(XmlReader aReader)
        {
            if (aReader.NodeType != XmlNodeType.None)
            {
                throw new Exception("XML subtree was not completely absorbed. This implies that" +
                    "required elements are out-of-order or missing, or an unsupported element" +
                    "is present.");
            }
        }

        protected void _AddRequiredChild(XmlReader aReader, Elements.Element aElement)
        {
            if (aReader.Name != aElement.Name)
            {
                throw new Exception("expected child element <" + aElement.Name + "> not found.");
            }
            else
            {
                XmlReader sub = _Sub(aReader);
                _ColladaElement node = aElement.New(sub);
                _CheckSubFinished(sub);
                _NextElement(aReader);
                node.Parent = this;
            }
        }

        protected int _AddOptionalChild(XmlReader aReader, Elements.Element aElement)
        {
            if (aReader.Name == aElement.Name)
            {
                XmlReader sub = _Sub(aReader);
                _ColladaElement node = aElement.New(sub);
                _CheckSubFinished(sub);
                _NextElement(aReader);
                node.Parent = this;

                return 1;
            }
            else
            {
                return 0;
            }
        }

        protected int _AddOneToManyChildren(XmlReader aReader, Elements.Element aElement)
        {
            int ret = 1;

            _AddRequiredChild(aReader, aElement);
            ret += _AddZeroToManyChildren(aReader, aElement);

            return ret;
        }

        protected int _AddZeroToManyChildren(XmlReader aReader, Elements.Element aElement)
        {
            int ret = 0;

            while (aReader.Name == aElement.Name)
            {
                XmlReader sub = _Sub(aReader);
                _ColladaElement node = aElement.New(sub);
                _CheckSubFinished(sub);
                _NextElement(aReader);
                node.Parent = this;
                ret++;
            }

            return ret;
        }

        protected delegate T AttributeConvert<T>(string aValue);
        protected void _SetRequiredAttribute<T>(XmlReader aReader, string aAttributeName, out T arOut, AttributeConvert<T> d)
        {
            if (!aReader.MoveToAttribute(aAttributeName))
            {
                throw new Exception("required attribute \"" + aAttributeName + "\" not set.");
            }
            else
            {
                arOut = d(aReader.Value);
                aReader.MoveToElement();
            }
        }

        protected bool _SetOptionalAttribute<T>(XmlReader aReader, string aAttributeName, ref T arOut, AttributeConvert<T> d)
        {
            if (aReader.MoveToAttribute(aAttributeName))
            {
                arOut = d(aReader.Value);
                aReader.MoveToElement();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected void _SetRequiredAttribute(XmlReader aReader, string aAttributeName, out string arOut)
        {
            _SetRequiredAttribute<string>(aReader, aAttributeName, out arOut, delegate(string aIn) { return aIn; });
        }

        protected void _SetRequiredAttribute(XmlReader aReader, string aAttributeName, out uint arOut)
        {
            _SetRequiredAttribute<uint>(aReader, aAttributeName, out arOut, XmlConvert.ToUInt32);
        }

        protected void _SetRequiredAttribute(XmlReader aReader, string aAttributeName, out short arOut)
        {
            _SetRequiredAttribute<short>(aReader, aAttributeName, out arOut, XmlConvert.ToInt16);
        }

        protected void _SetRequiredAttribute(XmlReader aReader, string aAttributeName, out int arOut)
        {
            _SetRequiredAttribute<int>(aReader, aAttributeName, out arOut, XmlConvert.ToInt32);
        }

        protected void _SetRequiredAttribute(XmlReader aReader, string aAttributeName, out float arOut)
        {
            _SetRequiredAttribute<float>(aReader, aAttributeName, out arOut, XmlConvert.ToSingle);
        }

        protected void _SetRequiredAttribute(XmlReader aReader, string aAttributeName, out double arOut)
        {
            _SetRequiredAttribute<double>(aReader, aAttributeName, out arOut, XmlConvert.ToDouble);
        }

        protected void _SetRequiredAttribute(XmlReader aReader, string aAttributeName, out bool arOut)
        {
            _SetRequiredAttribute<bool>(aReader, aAttributeName, out arOut, XmlConvert.ToBoolean);
        }

        protected bool _SetOptionalAttribute(XmlReader aReader, string aAttributeName, ref string arOut)
        {
            return _SetOptionalAttribute<string>(aReader, aAttributeName, ref arOut, delegate(string aIn) { return aIn; });
        }

        protected bool _SetOptionalAttribute(XmlReader aReader, string aAttributeName, ref uint arOut)
        {
            return _SetOptionalAttribute<uint>(aReader, aAttributeName, ref arOut, XmlConvert.ToUInt32);
        }

        protected bool _SetOptionalAttribute(XmlReader aReader, string aAttributeName, ref short arOut)
        {
            return _SetOptionalAttribute<short>(aReader, aAttributeName, ref arOut, XmlConvert.ToInt16);
        }

        protected bool _SetOptionalAttribute(XmlReader aReader, string aAttributeName, ref int arOut)
        {
            return _SetOptionalAttribute<int>(aReader, aAttributeName, ref arOut, XmlConvert.ToInt32);
        }

        protected bool _SetOptionalAttribute(XmlReader aReader, string aAttributeName, ref float arOut)
        {
            return _SetOptionalAttribute<float>(aReader, aAttributeName, ref arOut, XmlConvert.ToSingle);
        }

        protected bool _SetOptionalAttribute(XmlReader aReader, string aAttributeName, ref double arOut)
        {
            return _SetOptionalAttribute<double>(aReader, aAttributeName, ref arOut, XmlConvert.ToDouble);
        }

        protected bool _SetOptionalAttribute(XmlReader aReader, string aAttributeName, ref bool arOut)
        {
            return _SetOptionalAttribute<bool>(aReader, aAttributeName, ref arOut, XmlConvert.ToBoolean);
        }

        protected void _SetValue(XmlReader aReader, ref string arOut)
        {
            _NextText(aReader);
            arOut = aReader.Value;
            _NextElement(aReader);
        }

        protected void _SetValue(XmlReader aReader, ref DateTime arOut)
        {
            _NextText(aReader);
            arOut = aReader.ReadContentAsDateTime();
            _NextElement(aReader);
        }

        protected void _SetValue(XmlReader aReader, ref float arOut)
        {
            _NextText(aReader);
            arOut = aReader.ReadContentAsFloat();
            _NextElement(aReader);
        }

        protected bool _SetValueOptional<T>(XmlReader aReader, string aChildName, ref T arOut, AttributeConvert<T> d)
        {
            if (aReader.Name == aChildName)
            {
                _NextText(aReader);
                arOut = d(aReader.Value);
                _NextElement(aReader);
                return true;
            }
            else
            {
                return false;
            }
        }

        protected void _SetValueRequired<T>(XmlReader aReader, string aChildName, out T arOut, AttributeConvert<T> d)
        {
            if (aReader.Name != aChildName)
            {
                throw new Exception("expected child element <" + aChildName + "> not found.");
            }

            _NextText(aReader);
            arOut = d(aReader.Value);
            _NextElement(aReader);
        }

        protected bool _SetValueOptional(XmlReader aReader, string aChildName, ref string arOut)
        {
            return _SetValueOptional<string>(aReader, aChildName, ref arOut, delegate(string aIn) { return aIn; });
        }

        protected void _SetValueRequired(XmlReader aReader, string aChildName, out string arOut)
        {
            _SetValueRequired<string>(aReader, aChildName, out arOut, delegate(string aIn) { return aIn; });
        }

        protected bool _SetValueOptional(XmlReader aReader, string aChildName, ref float arOut)
        {
            return _SetValueOptional<float>(aReader, aChildName, ref arOut, XmlConvert.ToSingle);
        }

        protected void _SetValueRequired(XmlReader aReader, string aChildName, out float arOut)
        {
            _SetValueRequired<float>(aReader, aChildName, out arOut, XmlConvert.ToSingle);
        }

        protected bool _SetValueOptional(XmlReader aReader, string aChildName, ref uint arOut)
        {
            return _SetValueOptional<uint>(aReader, aChildName, ref arOut, XmlConvert.ToUInt32);
        }

        protected void _SetValueRequired(XmlReader aReader, string aChildName, out uint arOut)
        {
            _SetValueRequired<uint>(aReader, aChildName, out arOut, XmlConvert.ToUInt32);
        }

        protected bool _SetValueOptional(XmlReader aReader, string aChildName, ref bool arOut)
        {
            return _SetValueOptional<bool>(aReader, aChildName, ref arOut, XmlConvert.ToBoolean);
        }

        protected void _SetValueRequired(XmlReader aReader, string aChildName, out bool arOut)
        {
            _SetValueRequired<bool>(aReader, aChildName, out arOut, XmlConvert.ToBoolean);
        }

        protected bool _SetValueOptional(XmlReader aReader, string aChildName, ref DateTime arOut)
        {
            return _SetValueOptional<DateTime>(aReader, aChildName, ref arOut, delegate(string a) { return XmlConvert.ToDateTime(a, XmlDateTimeSerializationMode.Local); });
        }

        protected void _SetValueRequired(XmlReader aReader, string aChildName, out DateTime arOut)
        {
            _SetValueRequired<DateTime>(aReader, aChildName, out arOut, delegate(string a) { return XmlConvert.ToDateTime(a, XmlDateTimeSerializationMode.Local); });
        }

        protected void _PopulateBinHexBuffer(XmlReader aReader, ref byte[] arOut)
        {
            arOut = new byte[0];

            byte[] buf = new byte[Settings.kBinHexBufferSize];
            int read = aReader.ReadContentAsBinHex(buf, 0, Settings.kBinHexBufferSize);

            while (read != 0)
            {
                #region Combine into out buffer.
                byte[] merge = new byte[arOut.Length + read];
                arOut.CopyTo(merge, 0);
                Array.Copy(buf, 0, merge, arOut.Length, read);
                arOut = merge;
                #endregion

                read = aReader.ReadContentAsBinHex(buf, 0, Settings.kBinHexBufferSize);
            }
        }

        protected bool _SetValueOptional(XmlReader aReader, string aChildName, ref byte[] arOut)
        {
            if (aReader.Name == aChildName)
            {
                _NextText(aReader);
                _PopulateBinHexBuffer(aReader, ref arOut);
                _NextElement(aReader);
                return true;
            }
            else
            {
                return false;
            }
        }

        // Warning: this function will not work if, for some reason, ColladaDocument stops
        //    placing the '/' operator between namespace levels.
        protected void _ParseTargetToSidReference(string aTarget, out string aTargetElement, out string aTargetAccessors)
        {
            int index = -1;

            index = aTarget.IndexOf(Settings.kTargetAddressMemberSelect);

            if (index < 0)
            {
                index = aTarget.IndexOf(Settings.kTargetAddressArrayAccessLeft);

                if (index < 0)
                {
                    aTargetElement = aTarget;
                    aTargetAccessors = "";
                }
                else
                {
                    aTargetElement = aTarget.Substring(0, index);
                    aTargetAccessors = aTarget.Substring(index);
                }
            }
            else
            {
                aTargetElement = aTarget.Substring(0, index);
                aTargetAccessors = aTarget.Substring(index + 1); // exclude the '.'
            }
        }
        #endregion

        #region Attributes
        public static class Attributes
        {
            public const string kBaseUri = "base";
            public const string kClosed = "closed";
            public const string kCount = "count";
            public const string kDepth = "depth";
            public const string kDigits = "digits";
            public const string kEnd = "end";
            public const string kFormat = "format";
            public const string kHeight = "height";
            public const string kId = "id";
            public const string kInputSemantic = "input_semantic";
            public const string kInputSet = "input_set";
            public const string kLayer = "layer";
            public const string kMagnitude = "magnitude";
            public const string kMaterial = "material";
            public const string kMaxInclusive = "maxInclusive";
            public const string kMethod = "method";
            public const string kMinInclusive = "minInclusive";
            public const string kName = "name";
            public const string kOffset = "offset";
            public const string kOpaque = "opaque";
            public const string kPlatform = "platform";
            public const string kProfile = "profile";
            public const string kReference = "ref";
            public const string kSchema = "xmlns";
            public const string kSemantic = "semantic";
            public const string kSet = "set";
            public const string kSid = "sid";
            public const string kSource = "source";
            public const string kStart = "start";
            public const string kStride = "stride";
            public const string kSymbol = "symbol";
            public const string kTarget = "target";
            public const string kTexture = "texture";
            public const string kTexturecoord = "texcoord";
            public const string kType = "type";
            public const string kUnitMeter = "meter";
            public const string kUnitName = "name";
            public const string kUrl = "url";
            public const string kVersion = "version";
            public const string kWidth = "width";
        }
        #endregion

        #region Default values
        public static class Defaults
        {
            public const float kBorderColor = 0.0f;
            public const bool kbClosed = false;
            public const float kConstantAttenuation = 1.0f;
            public const float kFalloffAngle = 180.0f;
            public const float kFalloffExponent = 0.0f;
            public const string kFormat = "R8G8B8A8";
            public const uint kImageDepth = 1;
            public const int kIntMinAttribute = -2147483648;
            public const int kIntMaxAttribute = 2147483647;
            public const float kLinearAttenuation = 0.0f;
            public const Enums.SamplerFilter kMinfilter = Enums.SamplerFilter.None;
            public const Enums.SamplerFilter kMagfilter = Enums.SamplerFilter.None;
            public const Enums.SamplerFilter kMipfilter = Enums.SamplerFilter.None;
            public const uint kMipmapMaxlevel = 0;
            public const float kMipmapBias = 0.0f;
            public const Enums.MorphMethodType kMorphMethodAttribute = Enums.MorphMethodType.kNormalized;
            public const uint kOffsetDefault = 0;
            public const Enums.ParamName kParamName = Enums.ParamName.kNone;
            public static readonly string kParamSemantic = string.Empty;
            public const float kQuadraticAttenuation = 0.0f;
            public const double kStartAttribute = 0.0;
            public const uint kStrideDefault = 1;
            public const float kUnitMeter = 1.0f;
            public const string kUnitName = "meter";
            public static readonly Vector3 kForwardAxis = -Vector3.UnitY;
            public static readonly Vector3 kRightAxis = Vector3.UnitX;
            public static readonly Vector3 kUpAxis = Vector3.UnitY;
            public const Enums.UpAxis kUpAxisType = Enums.UpAxis.kY;
            public const Enums.SamplerWrap kWrapS = Enums.SamplerWrap.Wrap;
            public const Enums.SamplerWrap kWrapT = Enums.SamplerWrap.Wrap;
            public const Enums.SamplerWrap kWrapP = Enums.SamplerWrap.Wrap;
        }
        #endregion

        #region Elements
        public static class Elements
        {
            public delegate _ColladaElement _New(XmlReader aReader);
            public struct Element
            {
                public Element(string aName, _New aNew)
                {
                    Name = aName;
                    New = aNew;
                }

                public readonly string Name;
                public readonly _New New;
            };

            public static class FX
            {
                public static readonly Element kAlpha = new Element("alpha", delegate(XmlReader aReader) { return new fx.ColladaAlpha(aReader); });
                public static readonly Element kAnnotate = new Element("annotate", delegate(XmlReader aReader) { return new fx.ColladaAnnotate(aReader); });
                public static readonly Element kArgument = new Element("argument", delegate(XmlReader aReader) { return new fx.ColladaArgument(aReader); });
                public static readonly Element kAmbient = new Element("ambient", null);
                public static readonly Element kArray = new Element("array", delegate(XmlReader aReader) { return new fx.ColladaArray(aReader); });
                public static readonly Element kBindOfInstanceMaterial = new Element("bind", delegate(XmlReader aReader) { return new fx.ColladaBindOfInstanceMaterial(aReader); });
                public static readonly Element kBindMaterial = new Element("bind_material", delegate(XmlReader aReader) { return new fx.ColladaBindMaterial(aReader); });
                public static readonly Element kBindVertexInput = new Element("bind_vertex_input", delegate(XmlReader aReader) { return new fx.ColladaBindVertexInput(aReader); });
                public static readonly Element kBorderColor = new Element("border_color", null);
                public static readonly Element kBump = new Element("bump", null);
                public static readonly Element kCode = new Element("code", delegate(XmlReader aReader) { return new fx.ColladaCode(aReader); });
                public static readonly Element kColorClear = new Element("color_clear", delegate(XmlReader aReader) { return new fx.ColladaColorClear(aReader); });
                public static readonly Element kColorTarget = new Element("color_target", delegate(XmlReader aReader) { return new fx.ColladaColorTarget(aReader); });
                public static readonly Element kCommonColorOrTextureType = new Element("common_color_or_texture_type", null);
                public static readonly Element kCommonFloatOrParamType = new Element("common_float_or_param_type", null);
                public static readonly Element kCompilerOptions = new Element("compiler_options", delegate(XmlReader aReader) { return new fx.ColladaCompilerOptions(aReader); });
                public static readonly Element kCompilerTarget = new Element("compiler_target", delegate(XmlReader aReader) { return new fx.ColladaCompilerTarget(aReader); });
                public static readonly Element kConnectParam = new Element("connect_param", delegate(XmlReader aReader) { return new fx.ColladaConnectParam(aReader); });
                public static readonly Element kDepthClear = new Element("depth_clear", delegate(XmlReader aReader) { return new fx.ColladaDepthClear(aReader); });
                public static readonly Element kDepthTarget = new Element("depth_target", delegate(XmlReader aReader) { return new fx.ColladaDepthTarget(aReader); });
                public static readonly Element kDiffuse = new Element("diffuse", null);
                public static readonly Element kDraw = new Element("draw", delegate(XmlReader aReader) { return new fx.ColladaDraw(aReader); });
                public static readonly Element kEffect = new Element("effect", delegate(XmlReader aReader) { return new fx.ColladaEffect(aReader); });
                public static readonly Element kEmission = new Element("emission", null);
                public static readonly Element kFloat = new Element("float", delegate(XmlReader aReader) { return new fx.ColladaFloat(aReader); });
                public static readonly Element kFloat2 = new Element("float2", delegate(XmlReader aReader) { return new fx.ColladaFloat2(aReader); });
                public static readonly Element kFloat3 = new Element("float3", delegate(XmlReader aReader) { return new fx.ColladaFloat3(aReader); });
                public static readonly Element kFormat = new Element("format", null);
                public static readonly Element kFormatHint = new Element("format_hint", delegate(XmlReader aReader) { return new fx.ColladaFormatHint(aReader); });
                public static readonly Element kGenerator = new Element("generator", delegate(XmlReader aReader) { return new fx.ColladaGenerator(aReader); });
                public static readonly Element kInclude = new Element("include", delegate(XmlReader aReader) { return new fx.ColladaInclude(aReader); });
                public static readonly Element kIndexOfRefraction = new Element("index_of_refraction", null);
                public static readonly Element kInstanceEffect = new Element("instance_effect", delegate(XmlReader aReader) { return new fx.ColladaInstanceEffect(aReader); });
                public static readonly Element kInstanceMaterial = new Element("instance_material", delegate(XmlReader aReader) { return new fx.ColladaInstanceMaterial(aReader); });
                public static readonly Element kLibraryEffects = new Element("library_effects", delegate(XmlReader aReader) { return new fx.ColladaLibraryEffects(aReader); });
                public static readonly Element kLibraryMaterials = new Element("library_materials", delegate(XmlReader aReader) { return new fx.ColladaLibraryMaterials(aReader); });
                public static readonly Element kMagfilter = new Element("magfilter", null);
                public static readonly Element kMaterial = new Element("material", delegate(XmlReader aReader) { return new fx.ColladaMaterial(aReader); });
                public static readonly Element kMinfilter = new Element("minfilter", null);
                public static readonly Element kMipfilter = new Element("mipfilter", null);
                public static readonly Element kMipLevels = new Element("mip_levels", null);
                public static readonly Element kMipmapGenerate = new Element("mipmap_generate", null);
                public static readonly Element kMipmapBias = new Element("mipmap_bias", null);
                public static readonly Element kMipmapMaxlevel = new Element("mipmap_maxlevel", null);
                public static readonly Element kModifier = new Element("modifier", delegate(XmlReader aReader) { return new fx.ColladaModifier(aReader); });
                public static readonly Element kName = new Element("name", delegate(XmlReader aReader) { return new fx.ColladaName(aReader); });
                public static readonly Element kNewparamOfProfileCOMMON = new Element("newparam", delegate(XmlReader aReader) { return new fx.ColladaNewparamOfProfileCOMMON(aReader); });
                public static readonly Element kNewparamOfProfileGLESandEffect = new Element("newparam", delegate(XmlReader aReader) { return new fx.ColladaNewparamOfProfileGLESandEffect(aReader); });
                public static readonly Element kPass = new Element("pass", delegate(XmlReader aReader) { return new fx.ColladaPass(aReader); });
                public static readonly Element kProfileCG = new Element("profile_CG", delegate(XmlReader aReader) { return new fx.ColladaProfileCG(aReader); });
                public static readonly Element kProfileCOMMON = new Element("profile_COMMON", delegate(XmlReader aReader) { return new fx.ColladaProfileCOMMON(aReader); });
                public static readonly Element kProfileGLES = new Element("profile_GLES", delegate(XmlReader aReader) { return new fx.ColladaProfileGLES(aReader); });
                public static readonly Element kProfileGLSL = new Element("profile_GLSL", delegate(XmlReader aReader) { return new fx.ColladaProfileGLSL(aReader); });
                public static readonly Element kReflective = new Element("reflective", null);
                public static readonly Element kReflectivity = new Element("reflectivity", null);
                public static readonly Element kRGB = new Element("RGB", delegate(XmlReader aReader) { return new fx.ColladaRGB(aReader); });
                public static readonly Element kSampler1D = new Element("sampler1D", null);
                public static readonly Element kSampler2D = new Element("sampler2D", null);
                public static readonly Element kSampler3D = new Element("sampler3D", null);
                public static readonly Element kSamplerCUBE = new Element("samplerCUBE", null);
                public static readonly Element kSamplerDEPTH = new Element("samplerDEPTH", null);
                public static readonly Element kSamplerRECT = new Element("samplerRECT", null);
                public static readonly Element kSamplerState = new Element("sampler_state", delegate(XmlReader aReader) { return new fx.ColladaSamplerState(aReader); });
                public static readonly Element kSemantic = new Element("semantic", delegate(XmlReader aReader) { return new fx.ColladaSemantic(aReader); });
                public static readonly Element kSetparamOfInstanceEffect = new Element("setparam", delegate(XmlReader aReader) { return new fx.ColladaSetparamOfInstanceEffect(aReader); });
                public static readonly Element kShader = new Element("shader", delegate(XmlReader aReader) { return new fx.ColladaShader(aReader); });
                public static readonly Element kShininess = new Element("shininess", null);
                public static readonly Element kSize = new Element("size", null);
                public static readonly Element kSpecular = new Element("specular", null);
                public static readonly Element kStencilClear = new Element("stencil_clear", delegate(XmlReader aReader) { return new fx.ColladaStencilClear(aReader); });
                public static readonly Element kStencilTarget = new Element("stencil_target", delegate(XmlReader aReader) { return new fx.ColladaStencilTarget(aReader); });
                public static readonly Element kSurfaceOfProfileCOMMON = new Element("surface", delegate(XmlReader aReader) { return new fx.ColladaSurfaceOfProfileCOMMON(aReader); });
                public static readonly Element kTechniqueCommonOfBindMaterial = new Element("technique_common", delegate(XmlReader aReader) { return new fx.ColladaTechniqueCommonOfBindMaterial(aReader); });
                public static readonly Element kTechniqueOfProfileCOMMON = new Element("technique", delegate(XmlReader aReader) { return new fx.ColladaTechniqueFXOfProfileCOMMON(aReader); });
                public static readonly Element kTechniqueHint = new Element("technique_hint", delegate(XmlReader aReader) { return new fx.ColladaTechniqueHint(aReader); });
                public static readonly Element kTexcombiner = new Element("texcombiner", delegate(XmlReader aReader) { return new fx.ColladaTexcombiner(aReader); });
                public static readonly Element kTexenv = new Element("texenv", delegate(XmlReader aReader) { return new fx.ColladaTexenv(aReader); });
                public static readonly Element kTexturePipeline = new Element("texture_pipeline", delegate(XmlReader aReader) { return new fx.ColladaTexturePipeline(aReader); });
                public static readonly Element kTexture = new Element("texture", delegate(XmlReader aReader) { return new fx._ColladaTexture(); });
                public static readonly Element kTextureUnit = new Element("texture_unit", delegate(XmlReader aReader) { return new fx.ColladaTextureUnit(aReader); });
                public static readonly Element kTransparency = new Element("transparency", null);
                public static readonly Element kTransparent = new Element("transparent", null);
                public static readonly Element kUsertype = new Element("usertype", delegate(XmlReader aReader) { return new fx.ColladaUsertype(aReader); });
                public static readonly Element kViewportRatio = new Element("viewport_ratio", null);
                public static readonly Element kWrapS = new Element("wrap_s", null);
                public static readonly Element kWrapT = new Element("wrap_t", null);
                public static readonly Element kWrapP = new Element("wrap_p", null);
            }

            public static class Physics
            {
                public static readonly Element kConvexMesh = new Element("convex_mesh", delegate(XmlReader aReader) { return new physics.ColladaConvexMesh(aReader); });
                public static readonly Element kGravity = new Element("gravity", null);
                public static readonly Element kInstanceForceField = new Element("instance_force_field", delegate(XmlReader aReader) { return new physics.ColladaInstanceForceField(aReader); });
                public static readonly Element kInstancePhysicsModel = new Element("instance_physics_model", delegate(XmlReader aReader) { return new physics.ColladaInstancePhysicsModel(aReader); });
                public static readonly Element kInstancePhysicsScene = new Element("instance_physics_scene", delegate(XmlReader aReader) { return new physics.ColladaInstancePhysicsScene(aReader); });
                public static readonly Element kLibraryForceFields = new Element("library_force_fields", delegate(XmlReader aReader) { return new physics.ColladaLibraryForceFields(aReader); });
                public static readonly Element kLibraryPhysicsMaterials = new Element("library_physics_materials", delegate(XmlReader aReader) { return new physics.ColladaLibraryPhysicsMaterials(aReader); });
                public static readonly Element kLibraryPhysicsModels = new Element("library_physics_models", delegate(XmlReader aReader) { return new physics.ColladaLibraryPhysicsModels(aReader); });
                public static readonly Element kLibraryPhysicsScenes = new Element("library_physics_scenes", delegate(XmlReader aReader) { return new physics.ColladaLibraryPhysicsScenes(aReader); });
                public static readonly Element kPhysicsScene = new Element("physics_scene", delegate(XmlReader aReader) { return new physics.ColladaPhysicsScene(aReader); });
                public static readonly Element kTechniqueCommonOfPhysicsScene = new Element("technique_common", delegate(XmlReader aReader) { return new physics.ColladaTechniqueCommonOfPhysicsScene(aReader); });
                public static readonly Element kTimeStep = new Element("time_step", null);
            }

            public static readonly Element kAccessor = new Element("accessor", delegate(XmlReader aReader) { return new ColladaAccessor(aReader); });
            public static readonly Element kAnimation = new Element("animation", delegate(XmlReader aReader) { return new ColladaAnimation(aReader); });
            public static readonly Element kAnimationClip = new Element("animation_clip", delegate(XmlReader aReader) { return new ColladaAnimationClip(aReader); });
            public static readonly Element kAspectRatio = new Element("aspect_ratio", null);
            public static readonly Element kAuthor = new Element("author", null);
            public static readonly Element kAuthoringTool = new Element("authoring_tool", null);
            public static readonly Element kAsset = new Element("asset", delegate(XmlReader aReader) { return new ColladaAsset(aReader); });
            public static readonly Element kBindShapeMatrix = new Element("bind_shape_matrix", delegate(XmlReader aReader) { return new ColladaBindShapeMatrix(aReader); });
            public static readonly Element kBoolArray = new Element("bool_array", delegate(XmlReader aReader) { return new ColladaBoolArray(aReader); });
            public static readonly Element kCamera = new Element("camera", delegate(XmlReader aReader) { return new ColladaCamera(aReader); });
            public static readonly Element kChannel = new Element("channel", delegate(XmlReader aReader) { return new ColladaChannel(aReader); });
            public static readonly Element kCollada = new Element("COLLADA", null);
            public static readonly Element kColor = new Element("color", delegate(XmlReader aReader) { return new ColladaColor(aReader); });
            public static readonly Element kComments = new Element("comments", null);
            public static readonly Element kContributor = new Element("contributor", delegate(XmlReader aReader) { return new ColladaContributor(aReader); });
            public static readonly Element kController = new Element("controller", delegate(XmlReader aReader) { return new ColladaController(aReader); });
            public static readonly Element kControlVertices = new Element("control_vertices", delegate(XmlReader aReader) { return new ColladaControlVertices(aReader); });
            public static readonly Element kConstantAttenuation = new Element("constant_attenuation", null);
            public static readonly Element kCopyright = new Element("copyright", null);
            public static readonly Element kCreated = new Element("created", null);
            public static readonly Element kData = new Element("data", null);
            public static readonly Element kEvaluateScene = new Element("evaluate_scene", delegate(XmlReader aReader) { return new ColladaEvaluateScene(aReader); });
            public static readonly Element kExtra = new Element("extra", delegate(XmlReader aReader) { return new ColladaExtra(aReader); });
            public static readonly Element kFalloffAngle = new Element("falloff_angle", null);
            public static readonly Element kFalloffExponent = new Element("falloff_exponent", null);
            public static readonly Element kFloat = new Element("float", null);
            public static readonly Element kFloatArray = new Element("float_array", delegate(XmlReader aReader) { return new ColladaFloatArray(aReader); });
            public static readonly Element kGeometry = new Element("geometry", delegate(XmlReader aReader) { return new ColladaGeometry(aReader); });
            public static readonly Element kIdrefArray = new Element("IDREF_array", delegate(XmlReader aReader) { return new ColladaIdrefArray(aReader); });
            public static readonly Element kImage = new Element("image", delegate(XmlReader aReader) { return new ColladaImage(aReader); });
            public static readonly Element kImager = new Element("imager", delegate(XmlReader aReader) { return new ColladaImager(aReader); });
            public static readonly Element kInitFrom = new Element("init_from", null);
            public static readonly Element kInputGroupA = new Element("input", delegate(XmlReader aReader) { return new ColladaInputGroupA(aReader); });
            public static readonly Element kInputGroupB = new Element("input", delegate(XmlReader aReader) { return new ColladaInputGroupB(aReader); });
            public static readonly Element kInstanceAnimation = new Element("instance_animation", delegate(XmlReader aReader) { return new ColladaInstanceAnimation(aReader); });
            public static readonly Element kInstanceCamera = new Element("instance_camera", delegate(XmlReader aReader) { return new ColladaInstanceCamera(aReader); });
            public static readonly Element kInstanceController = new Element("instance_controller", delegate(XmlReader aReader) { return new ColladaInstanceController(aReader); });
            public static readonly Element kInstanceGeometry = new Element("instance_geometry", delegate(XmlReader aReader) { return new ColladaInstanceGeometry(aReader); });
            public static readonly Element kInstanceLight = new Element("instance_light", delegate(XmlReader aReader) { return new ColladaInstanceLight(aReader); });
            public static readonly Element kInstanceNode = new Element("instance_node", delegate(XmlReader aReader) { return new ColladaInstanceNode(aReader); });
            public static readonly Element kInstanceVisualScene = new Element("instance_visual_scene", delegate(XmlReader aReader) { return new ColladaInstanceVisualScene(aReader); });
            public static readonly Element kIntArray = new Element("int_array", delegate(XmlReader aReader) { return new ColladaIntArray(aReader); });
            public static readonly Element kJoints = new Element("joints", delegate(XmlReader aReader) { return new ColladaJoints(aReader); });
            public static readonly Element kKeywords = new Element("keywords", null);
            public static readonly Element kLibraryAnimations = new Element("library_animations", delegate(XmlReader aReader) { return new ColladaLibraryAnimations(aReader); });
            public static readonly Element kLibraryAnimationClips = new Element("library_animation_clips", delegate(XmlReader aReader) { return new ColladaLibraryAnimationClips(aReader); });
            public static readonly Element kLibraryCameras = new Element("library_cameras", delegate(XmlReader aReader) { return new ColladaLibraryCameras(aReader); });
            public static readonly Element kLibraryControllers = new Element("library_controllers", delegate(XmlReader aReader) { return new ColladaLibraryControllers(aReader); });
            public static readonly Element kLibraryGeometries = new Element("library_geometries", delegate(XmlReader aReader) { return new ColladaLibraryGeometries(aReader); });
            public static readonly Element kLibraryImages = new Element("library_images", delegate(XmlReader aReader) { return new ColladaLibraryImages(aReader); });
            public static readonly Element kLibraryLights = new Element("library_lights", delegate(XmlReader aReader) { return new ColladaLibraryLights(aReader); });
            public static readonly Element kLibraryNodes = new Element("library_nodes", delegate(XmlReader aReader) { return new ColladaLibraryNodes(aReader); });
            public static readonly Element kLibraryVisualScenes = new Element("library_visual_scenes", delegate(XmlReader aReader) { return new ColladaLibraryVisualScenes(aReader); });
            public static readonly Element kLight = new Element("light", delegate(XmlReader aReader) { return new ColladaLight(aReader); });
            public static readonly Element kLinearAttenuation = new Element("linear_attenuation", null);
            public static readonly Element kLines = new Element("lines", delegate(XmlReader aReader) { return new ColladaLines(aReader); });
            public static readonly Element kLineStrips = new Element("linestrips", delegate(XmlReader aReader) { return new ColladaLinestrips(aReader); });
            public static readonly Element kLookAt = new Element("lookat", delegate(XmlReader aReader) { return new ColladaLookAt(aReader); });
            public static readonly Element kMatrix = new Element("matrix", delegate(XmlReader aReader) { return new ColladaMatrix(aReader); });
            public static readonly Element kMesh = new Element("mesh", delegate(XmlReader aReader) { return new ColladaMesh(aReader); });
            public static readonly Element kModified = new Element("modified", null);
            public static readonly Element kMorph = new Element("morph", delegate(XmlReader aReader) { return new ColladaMorph(aReader); });
            public static readonly Element kNameArray = new Element("Name_array", delegate(XmlReader aReader) { return new ColladaNameArray(aReader); });
            public static readonly Element kNode = new Element("node", delegate(XmlReader aReader) { return new ColladaNode(aReader); });
            public static readonly Element kOptics = new Element("optics", delegate(XmlReader aReader) { return new ColladaOptics(aReader); });
            public static readonly Element kOrthographic = new Element("orthographic", delegate(XmlReader aReader) { return new ColladaOrthographic(aReader); });
            public static readonly Element kParam = new Element("param", delegate(XmlReader aReader) { return new ColladaParam(aReader); });
            public static readonly Element kPerspective = new Element("perspective", delegate(XmlReader aReader) { return new ColladaPerspective(aReader); });
            public static readonly Element kPolygons = new Element("polygons", delegate(XmlReader aReader) { return new ColladaPolygons(aReader); });
            public static readonly Element kPolylist = new Element("polylist", delegate(XmlReader aReader) { return new ColladaPolylist(aReader); });
            public static readonly Element kPrimitives = new Element("p", delegate(XmlReader aReader) { return new ColladaPrimitives(aReader); });
            public static readonly Element kQuadraticAttenuation = new Element("quadratic_attenuation", null);
            public static readonly Element kRevision = new Element("revision", null);
            public static readonly Element kRotate = new Element("rotate", delegate(XmlReader aReader) { return new ColladaRotate(aReader); });
            public static readonly Element kSampler = new Element("sampler", delegate(XmlReader aReader) { return new ColladaSampler(aReader); });
            public static readonly Element kScale = new Element("scale", delegate(XmlReader aReader) { return new ColladaScale(aReader); });
            public static readonly Element kScene = new Element("scene", delegate(XmlReader aReader) { return new ColladaScene(aReader); });
            public static readonly Element kSkeleton = new Element("skeleton", delegate(XmlReader aReader) { return new ColladaSkeleton(aReader); });
            public static readonly Element kSkew = new Element("skew", delegate(XmlReader aReader) { return new ColladaSkew(aReader); });
            public static readonly Element kSkin = new Element("skin", delegate(XmlReader aReader) { return new ColladaSkin(aReader); });
            public static readonly Element kSource = new Element("source", delegate(XmlReader aReader) { return new ColladaSource(aReader); });
            public static readonly Element kSourceData = new Element("source_data", null);
            public static readonly Element kSpline = new Element("spline", delegate(XmlReader aReader) { return new ColladaSpline(aReader); });
            public static readonly Element kSubject = new Element("subject", null);
            public static readonly Element kTargets = new Element("targets", delegate(XmlReader aReader) { return new ColladaTargets(aReader); });
            public static readonly Element kTechnique = new Element("technique", delegate(XmlReader aReader) { return new ColladaTechnique(aReader); });
            public static readonly Element kTechniqueCommonOfLight = new Element("technique_common", delegate(XmlReader aReader) { return new ColladaTechniqueCommonOfLight(aReader); });
            public static readonly Element kTechniqueCommonOfOptics = new Element("technique_common", delegate(XmlReader aReader) { return new ColladaTechniqueCommonOfOptics(aReader); });
            public static readonly Element kTechniqueCommonOfSource = new Element("technique_common", delegate(XmlReader aReader) { return new ColladaTechniqueCommonOfSource(aReader); });
            public static readonly Element kTitle = new Element("title", null);
            public static readonly Element kTranslate = new Element("translate", delegate(XmlReader aReader) { return new ColladaTranslate(aReader); });
            public static readonly Element kTriangles = new Element("triangles", delegate(XmlReader aReader) { return new ColladaTriangles(aReader); });
            public static readonly Element kTrifans = new Element("trifans", delegate(XmlReader aReader) { return new ColladaTrifans(aReader); });
            public static readonly Element kTristrips = new Element("tristrips", delegate(XmlReader aReader) { return new ColladaTristrips(aReader); });
            public static readonly Element kUnit = new Element("unit", null);
            public static readonly Element kUpAxis = new Element("up_axis", null);
            public static readonly Element kV = new Element("v", null);
            public static readonly Element kVcount = new Element("vcount", null);
            public static readonly Element kVertexWeights = new Element("vertex_weights", delegate(XmlReader aReader) { return new ColladaVertexWeights(aReader); });
            public static readonly Element kVertices = new Element("vertices", delegate(XmlReader aReader) { return new ColladaVertices(aReader); });
            public static readonly Element kVisualScene = new Element("visual_scene", delegate(XmlReader aReader) { return new ColladaVisualScene(aReader); });
            public static readonly Element kXfov = new Element("xfov", null);
            public static readonly Element kYfov = new Element("yfov", null);
            public static readonly Element kXmag = new Element("xmag", null);
            public static readonly Element kYmag = new Element("ymag", null);
            public static readonly Element kZnear = new Element("znear", null);
            public static readonly Element kZfar = new Element("zfar", null);
        }
        #endregion

        #region Enums
        public static class Enums
        {
            public enum MorphMethodType
            {
                kNormalized,
                kRelative
            }

            public static MorphMethodType GetMorphMethodType(string a)
            {
                switch (a)
                {
                    case "NORMALIZED": return MorphMethodType.kNormalized;
                    case "RELATIVE": return MorphMethodType.kRelative;
                    default:
                        throw new Exception("Unknown morph method type \"" + a + "\".");
                }
            }

            public static class SamplerFilterNames
            {
                public const string kNone = "NONE";
                public const string kNearest = "NEAREST";
                public const string kLinear = "LINEAR";
                public const string kNearestMipmapNearest = "NEAREST_MIPMAP_NEAREST";
                public const string kLinearMipmapNearest = "LINEAR_MIPMAP_NEAREST";
                public const string kNearestMipmapLinear = "NEAREST_MIPMAP_LINEAR";
                public const string kLinearMipmapLinear = "LINEAR_MIPMAP_LINEAR";
            }

            public enum SamplerFilter
            {
                None,
                Nearest,
                Linear,
                NearestMipmapNearest,
                LinearMipmapNearest,
                NearestMipmapLinear,
                LinearMipmapLinear
            }

            public static SamplerFilter SamplerFilterFromString(string aSamplerFilter)
            {
                switch (aSamplerFilter)
                {
                    case SamplerFilterNames.kNone: return SamplerFilter.None;
                    case SamplerFilterNames.kNearest: return SamplerFilter.Nearest;
                    case SamplerFilterNames.kLinear: return SamplerFilter.Linear;
                    case SamplerFilterNames.kNearestMipmapNearest: return SamplerFilter.NearestMipmapNearest;
                    case SamplerFilterNames.kLinearMipmapNearest: return SamplerFilter.LinearMipmapNearest;
                    case SamplerFilterNames.kNearestMipmapLinear: return SamplerFilter.NearestMipmapLinear;
                    case SamplerFilterNames.kLinearMipmapLinear: return SamplerFilter.LinearMipmapLinear;
                    default:
                        throw new Exception("Invalid sampler filter specification \"" + aSamplerFilter + "\".");
                }
            }

            public static class SamplerWrapNames
            {
                public const string kWrap = "WRAP";
                public const string kMirror = "MIRROR";
                public const string kClamp = "CLAMP";
                public const string kBorder = "BORDER";
                public const string kNone = "NONE";
            }

            public enum SamplerType
            {
                k1D,
                k2D,
                k3D,
                kCube,
                kDepth,
                kRect
            }

            public static SamplerType GetSamplerType(string s)
            {
                switch (s)
                {
                    case "sampler1D": return SamplerType.k1D;
                    case "sampler2D": return SamplerType.k2D;
                    case "sampler3D": return SamplerType.k3D;
                    case "samplerCUBE": return SamplerType.kCube;
                    case "samplerDEPTH": return SamplerType.kDepth;
                    case "samplerRECT": return SamplerType.kRect;
                    default:
                        throw new Exception("Unknown sampler type \"" + s + "\".");
                }
            }

            public enum SamplerWrap
            {
                Wrap,
                Mirror,
                Clamp,
                Border,
                None
            }

            public static SamplerWrap SamplerWrapFromString(string aSamplerWrap)
            {
                switch (aSamplerWrap)
                {
                    case SamplerWrapNames.kWrap: return SamplerWrap.Wrap;
                    case SamplerWrapNames.kMirror: return SamplerWrap.Mirror;
                    case SamplerWrapNames.kClamp: return SamplerWrap.Clamp;
                    case SamplerWrapNames.kBorder: return SamplerWrap.Border;
                    case SamplerWrapNames.kNone: return SamplerWrap.None;
                    default:
                        throw new Exception("Invalid sampler wrap specification \"" + aSamplerWrap + "\".");
                }
            }

            public enum UpAxis
            {
                kX,
                kY,
                kZ
            }

            public static UpAxis GetUpAxis(string aName)
            {
                switch (aName)
                {
                    case "X_UP": return UpAxis.kX;
                    case "Y_UP": return UpAxis.kY;
                    case "Z_UP": return UpAxis.kZ;
                    default:
                        throw new Exception("Invalid <up_axis> \"" + aName + "\".");
                }
            }

            public static class TechniqueHintProfile
            {
                public const string kCg = "CG";
                public const string kCommon = "COMMON";
                public const string kGles = "GLES";
                public const string kGlsl = "GLSL";
            }

            public enum Profile
            {
                Cg,
                Common,
                Gles,
                Glsl
            }

            public static class SurfaceTypeAttribute
            {
                public const string kUntyped = "UNTYPED";
                public const string k1d = "1D";
                public const string k2d = "2D";
                public const string k3d = "3D";
                public const string kCube = "CUBE";
                public const string kDepth = "DEPTH";
                public const string kRectangle = "RECT";
            }

            public enum SurfaceType
            {
                Untyped,
                OneD,
                TwoD,
                ThreeD,
                Cube,
                Depth,
                Rectangle
            }

            public static class FormatHintChannels
            {
                public const string kRgb = "RGB";
                public const string kRgba = "RGBA";
                public const string kLuminance = "L";
                public const string kLuminanceAlpha = "LA";
                public const string kDepth = "D";
                public const string kXyz = "XYZ";
                public const string kXyzw = "XYZW";
            }

            public enum Channels
            {
                Rgb,
                Rgba,
                Luminance,
                LuminanceAlpha,
                Depth,
                Xyz,
                Xyzw
            }

            public enum CoreValueType
            {
                kBool, kBool2, kBool3, kBool4,
                kInt, kInt2, kInt3, kInt4,
                kFloat, kFloat2, kFloat3, kFloat4,
                kFloat1x1, kFloat1x2, kFloat1x3, kFloat1x4,
                kFloat2x1, kFloat2x2, kFloat2x3, kFloat2x4,
                kFloat3x1, kFloat3x2, kFloat3x3, kFloat3x4,
                kFloat4x1, kFloat4x2, kFloat4x3, kFloat4x4,
                kSurface,
                kSampler1D, kSampler2D, kSampler3D, kSamplerCUBE, kSamplerRECT, kSamplerDEPTH,
                kEnum
            }

            public static CoreValueType GetCoreValueType(string a)
            {
                switch (a)
                {
                    case "bool": return CoreValueType.kBool; case "bool2": return CoreValueType.kBool2;
                    case "bool3": return CoreValueType.kBool3; case "bool4": return CoreValueType.kBool4;
                    case "int": return CoreValueType.kInt; case "int2": return CoreValueType.kInt2;
                    case "int3": return CoreValueType.kInt3; case "int4": return CoreValueType.kInt4;
                    case "float": return CoreValueType.kFloat; case "float2": return CoreValueType.kFloat2;
                    case "float3": return CoreValueType.kFloat3; case "float4": return CoreValueType.kFloat4;
                    case "float1x1": return CoreValueType.kFloat1x1; case "float1x2": return CoreValueType.kFloat1x2;
                    case "float1x3": return CoreValueType.kFloat1x3; case "float1x4": return CoreValueType.kFloat1x4;
                    case "float2x1": return CoreValueType.kFloat2x1; case "float2x2": return CoreValueType.kFloat2x2;
                    case "float2x3": return CoreValueType.kFloat2x3; case "float2x4": return CoreValueType.kFloat2x4;
                    case "float3x1": return CoreValueType.kFloat3x1; case "float3x2": return CoreValueType.kFloat3x2;
                    case "float3x3": return CoreValueType.kFloat3x3; case "float3x4": return CoreValueType.kFloat3x4;
                    case "float4x1": return CoreValueType.kFloat4x1; case "float4x2": return CoreValueType.kFloat4x2;
                    case "float4x3": return CoreValueType.kFloat4x3; case "float4x4": return CoreValueType.kFloat4x4;
                    case "surface": return CoreValueType.kSurface;
                    case "sampler1D": return CoreValueType.kSampler1D;
                    case "sampler2D": return CoreValueType.kSampler2D;
                    case "sampler3D": return CoreValueType.kSampler3D;
                    case "samplerCUBE": return CoreValueType.kSamplerCUBE;
                    case "samplerRECT": return CoreValueType.kSamplerRECT;
                    case "samplerDEPTH": return CoreValueType.kSamplerDEPTH;
                    case "enum": return CoreValueType.kEnum;
                    default:
                        throw new Exception("Core value type \"" + a + "\" is unknown.");
                }
            }

            public static class FormatHintRange
            {
                public const string kSnorm = "SNORM";
                public const string kUnorm = "UNORM";
                public const string kSint = "SINT";
                public const string kUint = "UINT";
                public const string kFloat = "FLOAT";
            }

            public enum LightType
            {
                kAmbient,
                kDirectional,
                kPoint,
                kSpot
            }

            public static LightType GetLightType(string a)
            {
                switch (a)
                {
                    case "ambient": return LightType.kAmbient;
                    case "directional": return LightType.kDirectional;
                    case "point": return LightType.kPoint;
                    case "spot": return LightType.kSpot;
                    default:
                        throw new Exception("Unknown light type \"" + a + "\".");
                }
            }

            public enum Range
            {
                Snorm,
                Unorm,
                Sint,
                Uint,
                Float
            }

            public static class FormatHintPrecision
            {
                public const string kLow = "LOW";
                public const string kMid = "MID";
                public const string kHigh = "HIGH";
            }

            public enum Precision
            {
                Low,
                Mid,
                High
            }

            public static class FormatHintOption
            {
                public const string kSrgbGamma = "SRGB_GAMMA";
                public const string kNormalized3 = "NORMALIZED3";
                public const string kNormalized4 = "NORMALIZED4";
                public const string kCompressable = "COMPRESSABLE";
            }

            public enum Option
            {
                SrgbGamma,
                Normalized3,
                Normalized4,
                Compressable
            }

            public static class InputSemantic
            {
                public const string kBinormal = "BINORMAL";
                public const string kColor = "COLOR";
                public const string kContinuity = "CONTINUITY";
                public const string kImage = "IMAGE";
                public const string kInput = "INPUT";
                public const string kInTangent = "IN_TANGENT";
                public const string kInterpolation = "INTERPOLATION";
                public const string kInverseBindMatrix = "INV_BIND_MATRIX";
                public const string kJoint = "JOINT";
                public const string kLinearSteps = "LINEAR_STEPS";
                public const string kMorphTarget = "MORPH_TARGET";
                public const string kMorphWeight = "MORPH_WEIGHT";
                public const string kNormal = "NORMAL";
                public const string kOutput = "OUTPUT";
                public const string kOutTangent = "OUT_TANGENT";
                public const string kPosition = "POSITION";
                public const string kTangent = "TANGENT";
                public const string kTexbinormal = "TEXBINORMAL";
                public const string kTexcoord = "TEXCOORD";
                public const string kTextangent = "TEXTANGENT";
                public const string kUV = "UV";
                public const string kVertex = "VERTEX";
                public const string kWeight = "WEIGHT";
            }

            public enum EffectType
            {
                kConstant,
                kLambert,
                kPhong,
                kBlinn
            }

            public static class EffectTypeNames
            {
                public const string kConstant = "constant";
                public const string kLambert = "lambert";
                public const string kPhong = "phong";
                public const string kBlinn = "blinn";
            }

            public static EffectType GetEffectType(string a)
            {
                switch (a)
                {
                    case EffectTypeNames.kConstant: return EffectType.kConstant;
                    case EffectTypeNames.kLambert: return EffectType.kLambert;
                    case EffectTypeNames.kPhong: return EffectType.kPhong;
                    case EffectTypeNames.kBlinn: return EffectType.kBlinn;
                    default:
                        throw new Exception("\"" + a + "\" is an unknown <profile_COMMON> effect type.");
                }
            }

            public static bool IsValidEffectType(string a)
            {
                if ((a == EffectTypeNames.kConstant) ||
                    (a == EffectTypeNames.kLambert) ||
                    (a == EffectTypeNames.kPhong) ||
                    (a == EffectTypeNames.kBlinn))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public enum ParamName
            {
                kNone,
                kA,
                kAngle,
                kB,
                kG,
                kInterpolation,
                kJoint,
                kP,
                kQ,
                kR,
                kS,
                kT,
                kTime,
                kTransform,
                kU,
                kV,
                kW,
                kWeight,
                kX,
                kY,
                kZ,
            }

            public static ParamName GetParamName(string a)
            {
                switch (a)
                {
                    case "": return ParamName.kNone;
                    case "A": return ParamName.kA;
                    case "ANGLE": return ParamName.kAngle;
                    case "B": return ParamName.kB;
                    case "G": return ParamName.kG;
                    case "INTERPOLATION": return ParamName.kInterpolation;
                    case "JOINT": return ParamName.kJoint;
                    case "P": return ParamName.kP;
                    case "Q": return ParamName.kQ;
                    case "R": return ParamName.kR;
                    case "S": return ParamName.kS;
                    case "T": return ParamName.kT;
                    case "TIME": return ParamName.kTime;
                    case "TRANSFORM": return ParamName.kTransform;
                    case "U": return ParamName.kU;
                    case "V": return ParamName.kV;
                    case "W": return ParamName.kW;
                    case "WEIGHT": return ParamName.kWeight;
                    case "X": return ParamName.kX;
                    case "Y": return ParamName.kY;
                    case "Z": return ParamName.kZ;
                    default:
                        throw new Exception("\"" + a + "\" is an unknown <param> element name.");
                }
            }

            public enum Type
            {
                kFloat,
                kInt,
                kName,
                kBool,
                kIdref,
                kFloat4x4
            }

            public static Type GetType(string a)
            {
                switch (a)
                {
                    case "float": return Type.kFloat;
                    case "int": return Type.kInt;
                    case "Name": return Type.kName;
                    case "bool": return Type.kBool;
                    case "IDREF": return Type.kIdref;
                    case "float4x4": return Type.kFloat4x4;
                    default:
                        throw new Exception("\"" + a + "\" is an unknown type.");
                }
            }

            public static class TransparencyTypes
            {
                public const string kAone = "A_ONE";
                public const string kRgbZero = "RGB_ZERO";
            }
        }
        #endregion

        #region Settings
        public static class Settings
        {
            public const int kBinHexBufferSize = 1024;

            public const char kFragmentDelimiter = '#';

            public const string kTargetContainingElement = "./";
            public const char kTargetAddressContainingElement = '.';
            public const char kTargetAddressMemberSelect = '.';
            public const char kTargetAddressArrayAccessLeft = '(';
            public const char kTargetAddressArrayAccessRight = ')';
            public const char kTargetAddressSeperator = '/';

            /// <summary>
            /// Configuration setting - see detailed explanation.
            /// </summary>
            /// <remarks>
            /// See 4-92 of the COLLADA 1.4.1 specification - "sampler" must contain an
            /// "input" child with semantic attribute "INTERPOLATION" to be well-defined but
            /// it is often excluded. COLLADA does not define a default interpolation type so 
            /// the behavior when it is excluded is application defined.
            /// </remarks>
            public static bool bAllowMissingInputWithInterpolationSemanticOfSamplerElement = true;
        }
        #endregion

        public T GetFirst<T>() where T : _ColladaElement
        {
            for (_ColladaElement child = mFirstChild; child != null; child = child.NextSibling)
            {
                T e = child as T;

                if (e != null)
                {
                    return e;
                }
            }

            _ThrowGetRequiredException<T>();
            return null;
        }

        public T GetFirst<T>(Predicate<T> aPredicate) where T : _ColladaElement
        {
            for (_ColladaElement child = mFirstChild; child != null; child = child.NextSibling)
            {
                T e = child as T;

                if (e != null && aPredicate(e))
                {
                    return e;
                }
            }

            _ThrowGetRequiredException<T>();
            return null;
        }

        public T GetFirstOptional<T>() where T : _ColladaElement
        {
            for (_ColladaElement child = mFirstChild; child != null; child = child.NextSibling)
            {
                T e = child as T;

                if (e != null)
                {
                    return e;
                }
            }

            return null;
        }

        public T GetFirstOptional<T>(Predicate<T> aPredicate) where T : _ColladaElement
        {
            for (_ColladaElement child = mFirstChild; child != null; child = child.NextSibling)
            {
                T e = child as T;

                if (e != null && aPredicate(e))
                {
                    return e;
                }
            }

            return null;
        }

        /// <summary>
        /// Converts an (expected) 16-element float array containing a COLLADA matrix
        /// to an XNA matrix.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        /// <remarks>
        /// Although COLLADA uses the OpenGL standard of vectors as columns it stores the
        /// matrices in row-major, requiring the elements to be transposed in-place to make-up
        /// an XNA vectors as rows, row-major storage matrix.
        /// </remarks>
        public static Matrix ToXnaMatrix(float[] e)
        {
            Matrix ret = ToXnaMatrix(e, 0);

            return ret;
        }

        public static Matrix ToXnaMatrix(float[] e, uint index)
        {
            Matrix ret = new Matrix(e[index + 0], e[index + 4], e[index + 8], e[index + 12],
                                    e[index + 1], e[index + 5], e[index + 9], e[index + 13],
                                    e[index + 2], e[index + 6], e[index + 10], e[index + 14],
                                    e[index + 3], e[index + 7], e[index + 11], e[index + 15]);

            return ret;
        }
    }
}
