using Pegasus.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CGA
{
    public static class NumericsExtensions
    {
        private const float _Epsilon = 1e-6f;

        public static float GetElement(this Vector3 vector, int i)
        {
            if (i == 0) return vector.X;
            else if (i == 1) return vector.Y;
            else if (i == 2) return vector.Z;
            else throw new Exception($"Invalid element: {i}");
        }

        public static Vector3 SetElement(this Vector3 vector, int i, float value)
        {
            if (i == 0) vector.X = value;
            else if (i == 1) vector.Y = value;
            else if (i == 2) vector.Z = value;
            else throw new Exception($"Invalid element: {i}");
            return vector;
        }

        public static Vector3 GetPosition(this Matrix4x4 matrix)
        {
            return new Vector3(matrix.M41, matrix.M42, matrix.M43);
        }


        // Adapted from: https://www.learnopencv.com/rotation-matrix-to-euler-angles/
        public static Vector3 GetEulerAngles(this Matrix4x4 matrix)
        {
            var sy = Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M21 * matrix.M21);
            bool singular = sy < _Epsilon;
            float x, y, z;
            if (!singular)
            {
                x = (float)Math.Atan2(matrix.M32, matrix.M33);
                y = (float)Math.Atan2(-matrix.M31, sy);
                z = (float)Math.Atan2(matrix.M21, matrix.M11);
            }
            else
            {
                x = (float)Math.Atan2(-matrix.M23, matrix.M22);
                y = (float)Math.Atan2(-matrix.M31, sy);
                z = 0;
            }
            return new Vector3(x, y, z);
        }

        public static Matrix4x4 SetPosition(this Matrix4x4 matrix, Vector3 value)
        {
            matrix.M41 = value.X;
            matrix.M42 = value.Y;
            matrix.M43 = value.Z;
            return matrix;
        }

        public static Vector3 GetXAxis(this Matrix4x4 matrix)
        {
            return new Vector3(matrix.M11, matrix.M12, matrix.M13);
        }

        public static Vector3 GetYAxis(this Matrix4x4 matrix)
        {
            return new Vector3(matrix.M21, matrix.M22, matrix.M23);
        }

        public static Vector3 GetZAxis(this Matrix4x4 matrix)
        {
            return new Vector3(matrix.M31, matrix.M32, matrix.M33);
        }

        public static float CrossScalar(this Vector3 a, Vector3 b)
        {
            return (a.Y * b.Z - a.Z * b.Y) - (a.X * b.Z - a.Z * b.X) + (a.X * b.Y - a.Y * b.X);
        }
    }

    public static class SystemCollectionsExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> predicate)
        {
            foreach (var element in enumerable)
            {
                predicate(element);
            }
        }

        public static void SafePushAll<T>(this Stack<T> stack, Stack<T> elements)
        {
            if (elements == null)
            {
                return;
            }
            foreach (var element in elements.Reverse())
            {
                stack.Push(element);
            }
        }
    }

    public static class FloatExtensions
    {
        public static float ToRadians(this float degrees)
        {
            return ((float)Math.PI / 180.0f) * degrees;
        }
    }

    public static class EnumExtensions
    {
        public static string ToParseString(this ShapeAttribute shapeAttribute)
        {
            return string.Join(".", shapeAttribute.ToString().Split('_'));
        }
    }

    public enum Axis
    {
        X = 0,
        Y,
        Z
    }

    public enum SizePrefix
    {
        RELATIVE = '~',
        FEET = '\''
    }

    public enum AdjustSelector
    {
        ADJUST,
        NOADJUST
    }

    public enum SemanticSelector
    {
        FRONT,
        BACK,
        LEFT,
        RIGHT,
        TOP,
        BOTTOM,
        VERTICAL,
        HORIZONTAL,
        SIDE,
        ALL
    }

    public enum ComponentSelector
    {
        FACES = 'F',
        EDGES = 'E',
        VERTICES = 'V'
    }

    public abstract class Shape
    {
        public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;
        public Vector3 Size { get; set; } = Vector3.One;

        public abstract Shape Copy();

        public virtual float GetAttribute(ShapeAttribute shapeAttribute)
        {
            switch (shapeAttribute)
            {
                case ShapeAttribute.SCOPE_TX:
                    return Transform.GetPosition().X;
                case ShapeAttribute.SCOPE_TY:
                    return Transform.GetPosition().Y;
                case ShapeAttribute.SCOPE_TZ:
                    return Transform.GetPosition().Z;
                case ShapeAttribute.SCOPE_RX:
                    return Transform.GetEulerAngles().X;
                case ShapeAttribute.SCOPE_RY:
                    return Transform.GetEulerAngles().Y;
                case ShapeAttribute.SCOPE_RZ:
                    return Transform.GetEulerAngles().Z;
                case ShapeAttribute.SCOPE_SX:
                    return Size.X;
                case ShapeAttribute.SCOPE_SY:
                    return Size.Y;
                case ShapeAttribute.SCOPE_SZ:
                    return Size.Z;
                default:
                    throw new Exception($"Unknown {nameof(ShapeAttribute)}: {shapeAttribute}");
            }
        }
    }

    public interface IRuntimeValue
    {
        bool Negated { get; set; }
        bool Parenthesized { get; set; }
        float Evaluate(ExecutionContext context);
    }

    public class RuntimeConstant : IRuntimeValue
    {
        private readonly float _value;
        public bool Negated { get; set; }
        public bool Parenthesized { get; set; }

        public RuntimeConstant(float value)
        {
            _value = value;
        }

        public float Evaluate(ExecutionContext context)
        {
            return Negated ? -_value : _value;
        }

        public override string ToString() => (Negated ? "-" : "") + (Parenthesized ? "(" + _value.ToString() + ")" : _value.ToString());
    }

    public class AttributeValueLookup : IRuntimeValue
    {
        private readonly string _attributeName;
        public bool Negated { get; set; }
        public bool Parenthesized { get; set; }

        public AttributeValueLookup(string attributeName)
        {
            _attributeName = attributeName ?? throw new ArgumentNullException(nameof(attributeName));
        }

        public float Evaluate(ExecutionContext context)
        {
            return context.GetAttributeValue(_attributeName);
        }

        public override string ToString() => (Negated ? "-" : "") + (Parenthesized ? "(" + _attributeName + ")" : _attributeName);
    }

    public enum ShapeAttribute
    {
        SCOPE_TX,
        SCOPE_TY,
        SCOPE_TZ,
        SCOPE_RX,
        SCOPE_RY,
        SCOPE_RZ,
        SCOPE_SX,
        SCOPE_SY,
        SCOPE_SZ
    }

    public class ShapeAttributeValueLookup : IRuntimeValue
    {
        private readonly ShapeAttribute _shapeAttribute;
        public bool Negated { get; set; }
        public bool Parenthesized { get; set; }

        public ShapeAttributeValueLookup(ShapeAttribute shapeAttribute)
        {
            _shapeAttribute = shapeAttribute;
        }

        public float Evaluate(ExecutionContext context)
        {
            return context.CurrentShape.GetAttribute(_shapeAttribute);
        }

        public override string ToString() => (Negated ? "-" : "") + (Parenthesized ? "(" + _shapeAttribute.ToParseString() + ")" : _shapeAttribute.ToParseString());
    }

    public enum AlgebraicOperationType
    {
        ADDITION = '+',
        SUBTRACTION = '-',
        MULTIPLICATION = '*',
        DIVISION = '/',
        EXPONENTIATION = '^',
        MODULO = '%'
    }

    public class AlgebraicOperation : IRuntimeValue
    {
        private readonly AlgebraicOperationType _type;
        private readonly IRuntimeValue _leftOperand;
        private readonly IRuntimeValue _rightOperand;
        public bool Negated { get; set; }
        public bool Parenthesized { get; set; }

        public AlgebraicOperation(AlgebraicOperationType type, IRuntimeValue leftOperand, IRuntimeValue rightOperand, bool negated = false, bool parenthesized = false)
        {
            _type = type;
            _leftOperand = leftOperand ?? throw new ArgumentNullException(nameof(leftOperand));
            _rightOperand = rightOperand ?? throw new ArgumentNullException(nameof(leftOperand));
            Parenthesized = parenthesized;
        }

        public float Evaluate(ExecutionContext context)
        {
            float value;
            switch (_type)
            {
                case AlgebraicOperationType.ADDITION:
                    value = _leftOperand.Evaluate(context) + _rightOperand.Evaluate(context);
                    break;
                case AlgebraicOperationType.SUBTRACTION:
                    value = _leftOperand.Evaluate(context) - _rightOperand.Evaluate(context);
                    break;
                case AlgebraicOperationType.MULTIPLICATION:
                    value = _leftOperand.Evaluate(context) * _rightOperand.Evaluate(context);
                    break;
                case AlgebraicOperationType.DIVISION:
                    value = _leftOperand.Evaluate(context) / _rightOperand.Evaluate(context);
                    break;
                case AlgebraicOperationType.EXPONENTIATION:
                    value = (float)Math.Pow(_leftOperand.Evaluate(context), _rightOperand.Evaluate(context));
                    break;
                case AlgebraicOperationType.MODULO:
                    value = _leftOperand.Evaluate(context) % _rightOperand.Evaluate(context);
                    break;
                default:
                    throw new Exception($"Unknown {nameof(AlgebraicOperationType)}: {_type}");
            }
            return Negated ? -value : value;
        }

        public override string ToString() => $"{(Parenthesized ? "( " : "")}{_leftOperand.ToString()} {(char)_type} {_rightOperand.ToString()}{(Parenthesized ? " )" : "")}";
    }

    public class Quad : Shape
    {
        public override Shape Copy()
        {
            return new Quad
            {
                Transform = Transform,
                Size = Size
            };
        }

        public override string ToString() => $"Quad[{Transform}, {Size}]";
    }

    public class Box : Shape
    {
        public override Shape Copy()
        {
            return new Box
            {
                Transform = Transform,
                Size = Size
            };
        }

        public override string ToString() => $"Box[{Transform}, {Size}]";
    }

    public interface ISymbol
    {
        Stack<ISymbol> Rewrite(ExecutionContext context);
    }

    public class Symbol : ISymbol
    {
        public string Name { get; }

        public Symbol(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public Stack<ISymbol> Rewrite(ExecutionContext context)
        {
            return context.RunRule(this);
        }

        public override string ToString() => Name;

        public override bool Equals(object obj)
        {
            if (!(obj is Symbol other))
            {
                return false;
            }
            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }

    public abstract class ShapeOperation : ISymbol
    {
        public string Operand { get; }
        public IList<object> Params { get; }

        public ShapeOperation(string operand, IList<object> @params)
        {
            Operand = operand ?? throw new ArgumentNullException(nameof(operand));
            Params = @params ?? throw new ArgumentNullException(nameof(@params)); ;
        }

        public T GetParam<T>(int i)
        {
            return (T)Params[i];
        }

        public abstract Stack<ISymbol> Rewrite(ExecutionContext context);

        public override string ToString()
        {
            return $"{Operand}({string.Join(", ", Params.Select(x => x.ToString()))})";
        }
    }

    public abstract class SRTOperation : ShapeOperation
    {
        public float X(ExecutionContext context) => GetParam<IRuntimeValue>(0).Evaluate(context);
        public float Y(ExecutionContext context) => GetParam<IRuntimeValue>(1).Evaluate(context);
        public float Z(ExecutionContext context) => GetParam<IRuntimeValue>(2).Evaluate(context);

        public SRTOperation(string operand, IList<object> @params) : base(operand, @params)
        {
        }
    }

    public class TOperation : SRTOperation
    {
        public TOperation(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override Stack<ISymbol> Rewrite(ExecutionContext context)
        {
            var shape = context.PopShape();
            var T = Matrix4x4.CreateTranslation(new Vector3(X(context), Y(context), Z(context)));
            shape.Transform = T * shape.Transform;
            context.PushShape(shape);
            return null;
        }
    }

    public class ROperation : SRTOperation
    {
        public ROperation(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override Stack<ISymbol> Rewrite(ExecutionContext context)
        {
            var shape = context.PopShape();
            var R = Matrix4x4.CreateFromYawPitchRoll(Y(context).ToRadians(), X(context).ToRadians(), Z(context).ToRadians());
            shape.Transform = R * shape.Transform;
            context.PushShape(shape);
            return null;
        }
    }

    public class SOperation : SRTOperation
    {
        public SOperation(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override Stack<ISymbol> Rewrite(ExecutionContext context)
        {
            var shape = context.PopShape();
            shape.Size = new Vector3(X(context), Y(context), Z(context));
            context.PushShape(shape);
            return null;
        }
    }

    public interface IExtrusionStrategy
    {
        Shape Extrude(Shape parentShape, Axis axis, float distance);
    }

    public class QuadExtruder : IExtrusionStrategy
    {
        public Shape Extrude(Shape parentShape, Axis axis, float distance)
        {
            if (axis != Axis.Z)
            {
                throw new Exception($"Cannot extrude {nameof(Quad)} along {axis.ToString()}");
            }
            return new Box
            {
                Transform = Matrix4x4.CreateTranslation(0, distance * 0.5f, 0) * Matrix4x4.CreateRotationX((float)Math.PI * 0.5f) * parentShape.Transform,
                Size = parentShape.Size.SetElement((int)Axis.Y, distance)
            };
        }
    }

    public static class ExtrusionStrategyFactory
    {
        public static IExtrusionStrategy Create(Type shapeType)
        {
            if (shapeType == typeof(Quad))
            {
                return new QuadExtruder();
            }
            throw new NotImplementedException($"No extrusion strategy found for shape type: {nameof(shapeType.Name)}");
        }
    }

    public class ExtrudeOperation : ShapeOperation
    {
        public Axis Axis(ExecutionContext context) => (Params.Count == 1) ? CGA.Axis.Z : (Axis)GetParam<IRuntimeValue>(0).Evaluate(context);
        public float Distance(ExecutionContext context) => (Params.Count == 1) ? GetParam<IRuntimeValue>(0).Evaluate(context) : GetParam<IRuntimeValue>(1).Evaluate(context);

        public ExtrudeOperation(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override Stack<ISymbol> Rewrite(ExecutionContext context)
        {
            var shape = context.PopShape();
            var extrusionStrategy = ExtrusionStrategyFactory.Create(shape.GetType());
            context.PushShape(extrusionStrategy.Extrude(shape, Axis(context), Distance(context)));
            return null;
        }
    }

    public class ComponentSplitArg
    {
        public SemanticSelector Selector { get; }
        public ISymbol Successor { get; }

        public ComponentSplitArg(SemanticSelector selector, ISymbol successor)
        {
            Selector = selector;
            Successor = successor ?? throw new ArgumentNullException(nameof(successor));
        }

        public override string ToString() => $"{Selector.ToString().ToLower()}: {Successor.ToString()}";
    }

    public interface IComponentSplittingStrategy
    {
        IEnumerable<Shape> Split(Shape parentShape, ComponentSelector selector);
    }

    public class BoxComponentSplitter : IComponentSplittingStrategy
    {
        public IEnumerable<Shape> Split(Shape parentShape, ComponentSelector selector)
        {
            if (selector == ComponentSelector.FACES)
            {
                var halfWidth = parentShape.Size.X * 0.5f;
                var halfHeight = parentShape.Size.Y * 0.5f;
                var halfDepth = parentShape.Size.Z * 0.5f;
                var quads = new List<Quad>
                {
                    // bottom
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateTranslation(0, 0, halfHeight) * Matrix4x4.CreateRotationX((float)Math.PI * 0.5f) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.X, 1, parentShape.Size.Z)
                    },
                    // top
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateTranslation(0, 0, halfHeight) * Matrix4x4.CreateRotationX((float)Math.PI * -0.5f) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.X, 1, parentShape.Size.Z)
                    },
                    // back
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateTranslation(0, 0, halfDepth) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.X, 1, parentShape.Size.Z)
                    },
                    // front
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateTranslation(0, 0, halfDepth) * Matrix4x4.CreateRotationY(-(float)Math.PI) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.X, 1, parentShape.Size.Z)
                    },
                    // right
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateTranslation(0, 0, halfWidth) * Matrix4x4.CreateRotationY((float)Math.PI * 0.5f) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.Z, 1, parentShape.Size.X)
                    },
                    // left
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateTranslation(0, 0, halfWidth) * Matrix4x4.CreateRotationY((float)Math.PI * -0.5f) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.Z, 1, parentShape.Size.X)
                    },
                };
                return quads;
            }
            throw new NotImplementedException($"{nameof(BoxComponentSplitter)} doesn't know how to split '{selector.ToString()}' (yet?)");
        }
    }

    public static class ComponentSplittingStrategyFactory
    {
        public static IComponentSplittingStrategy Create(Type shapeType)
        {
            if (shapeType == typeof(Box))
            {
                return new BoxComponentSplitter();
            }
            throw new NotImplementedException($"No component splitting strategy found for shape type: {nameof(shapeType.Name)}");
        }
    }

    public interface ISemanticSelectionStrategy
    {
        bool Select(Shape parentShape, Shape componentShape, SemanticSelector selector);
    }

    public class BoxSemanticSelector : ISemanticSelectionStrategy
    {
        private static readonly float _Epsilon = 0.0001f;
        private static readonly float _DotThreshold = 1 - _Epsilon;

        public bool Select(Shape parentShape, Shape componentShape, SemanticSelector selector)
        {
            var componentShapeType = componentShape.GetType();
            if (componentShapeType != typeof(Quad))
            {
                throw new ArgumentException($"'{nameof(Box)}' cannot have a '{componentShapeType.Name}' as a component");
            }
            var cN = componentShape.Transform.GetZAxis();
            switch (selector)
            {
                case SemanticSelector.BOTTOM:
                    {
                        var pY = parentShape.Transform.GetYAxis();
                        var dot = Vector3.Dot(pY, cN);
                        return dot <= -_DotThreshold; /* Y and N are almost parallel and point to same direction */
                    }
                case SemanticSelector.TOP:
                    {
                        var pY = parentShape.Transform.GetYAxis();
                        var dot = Vector3.Dot(pY, cN);
                        return dot >= _DotThreshold; /* Y and N are almost parallel and point to opposite directions */
                    }
                case SemanticSelector.BACK:
                    {
                        var pZ = parentShape.Transform.GetZAxis();
                        var dot = Vector3.Dot(pZ, cN);
                        return dot <= -_DotThreshold; /* Z and N are almost parallel and point to same direction */
                    }
                case SemanticSelector.FRONT:
                    {
                        var pZ = parentShape.Transform.GetZAxis();
                        var dot = Vector3.Dot(pZ, cN);
                        return dot >= _DotThreshold; /* Z and N are almost parallel and point to opposite directions */
                    }
                case SemanticSelector.RIGHT:
                    {
                        var pX = parentShape.Transform.GetXAxis();
                        var dot = Vector3.Dot(pX, cN);
                        return dot >= _DotThreshold; /* X and N are almost parallel and point to same direction */
                    }
                case SemanticSelector.LEFT:
                    {
                        var pX = parentShape.Transform.GetXAxis();
                        var dot = Vector3.Dot(pX, cN);
                        return dot <= -_DotThreshold; /* X and N are almost parallel and point to opposite directions */
                    }
                case SemanticSelector.HORIZONTAL:
                    {
                        var pY = parentShape.Transform.GetYAxis();
                        var absDot = Math.Abs(Vector3.Dot(pY, cN));
                        return absDot >= _DotThreshold; /* Y and N are almost parallel */
                    }
                case SemanticSelector.VERTICAL:
                case SemanticSelector.SIDE:
                    {
                        var pY = parentShape.Transform.GetYAxis();
                        var absDot = Math.Abs(Vector3.Dot(pY, cN));
                        return absDot <= _Epsilon; /* Y and N are almost perpendicular */
                    }
                case SemanticSelector.ALL:
                    return true; /* permissive */
                default:
                    throw new Exception($"Unknown {nameof(SemanticSelector)}: '{selector.ToString()}'");
            }
        }
    }

    public static class SemanticSelectionStrategyFactory
    {
        public static ISemanticSelectionStrategy Create(Type shapeType)
        {
            if (shapeType == typeof(Box))
            {
                return new BoxSemanticSelector();
            }
            throw new NotImplementedException($"No semantic selection strategy found for shape type: {nameof(shapeType.Name)}");
        }
    }

    public class ComponentSplitOperation : ShapeOperation
    {
        public ComponentSelector Selector => GetParam<ComponentSelector>(0);
        public IEnumerable<ComponentSplitArg> Args => Params.Skip(1).Cast<ComponentSplitArg>();

        public ComponentSplitOperation(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override Stack<ISymbol> Rewrite(ExecutionContext context)
        {
            var shape = context.PopShape();
            var shapeType = shape.GetType();
            var shapeComponents = ComponentSplittingStrategyFactory.Create(shapeType).Split(shape, Selector);
            var selectionStrategy = SemanticSelectionStrategyFactory.Create(shapeType);
            var newSymbols = new Stack<ISymbol>();
            foreach (var arg in Args)
            {
                var selectedShapeComponents = shapeComponents.Where(x => selectionStrategy.Select(shape, x, arg.Selector));
                if (selectedShapeComponents.Count() == 0)
                {
                    continue;
                }
                shapeComponents = shapeComponents.Except(selectedShapeComponents);
                selectedShapeComponents.ForEach(x =>
                {
                    context.PushShape(x);
                    newSymbols.Push(arg.Successor);
                });
            }
            return newSymbols;
        }

        public override string ToString() => $"split({Selector.ToString().ToLower()}) {{ {string.Join(" | ", Args.Select(x => x.ToString()))} }}";
    }

    public interface ISplitter
    {
        Stack<ISymbol> Split(ExecutionContext context, Axis axis, Shape parentShape, float offset);
        float GetRelativeStep(ExecutionContext context);
        float GetAbsoluteSize(ExecutionContext context);
        void ComputeAbsoluteSize(ExecutionContext context, float absoluteSizeLeft, float relativeStepsSum);
    }

    public interface ISplitStrategy
    {
        Shape Split(Shape parentShape, Axis axis, float offset, float length);
    }

    public class ReplicatingSplitStrategy : ISplitStrategy
    {
        public Shape Split(Shape parentShape, Axis axis, float offset, float length)
        {
            var newShape = parentShape.Copy();
            var oldOrigin = newShape.Transform.GetPosition();
            var halfOldLength = newShape.Size.GetElement((int)axis) * 0.5f;
            var halfLength = length * 0.5f;
            var originOffset = Vector3.Zero.SetElement((int)axis, offset - halfOldLength + halfLength);
            newShape.Transform = newShape.Transform.SetPosition(oldOrigin + originOffset);
            newShape.Size = newShape.Size.SetElement((int)axis, length);
            return newShape;
        }
    }

    public static class SplitStrategyFactory
    {
        public static ISplitStrategy Create(Type shapeType)
        {
            if (shapeType == typeof(Box) || shapeType == typeof(Quad))
            {
                return new ReplicatingSplitStrategy();
            }
            throw new NotImplementedException($"No split strategy found for shape type: {nameof(shapeType.Name)}");
        }
    }

    public class SplitStep : ISplitter
    {
        private float _computedAbsoluteSize;
        public SizePrefix? Prefix { get; }
        public IRuntimeValue Value { get; }
        public ISymbol Successor { get; }

        public SplitStep(SizePrefix? prefix, IRuntimeValue value, ISymbol successor)
        {
            Prefix = prefix;
            Value = value;
            Successor = successor ?? throw new ArgumentNullException(nameof(successor));
            _computedAbsoluteSize = 0;
        }

        public Stack<ISymbol> Split(ExecutionContext context, Axis axis, Shape parentShape, float offset)
        {
            var absoluteSize = GetAbsoluteSize(context);
            var strategy = SplitStrategyFactory.Create(parentShape.GetType());
            context.PushShape(strategy.Split(parentShape, axis, offset, absoluteSize));
            return new Stack<ISymbol>(new[] { Successor });
        }

        public float GetRelativeStep(ExecutionContext context)
        {
            return Prefix.HasValue && Prefix.Value == SizePrefix.RELATIVE ? Value.Evaluate(context) : 0;
        }

        public float GetAbsoluteSize(ExecutionContext context)
        {
            if (!Prefix.HasValue)
            {
                return Value.Evaluate(context);
            }
            if (Prefix.Value == SizePrefix.RELATIVE)
            {
                return _computedAbsoluteSize;
            }
            else
            {
                // TODO:
                throw new NotImplementedException();
            }
        }

        public void ComputeAbsoluteSize(ExecutionContext context, float absoluteSizeLeft, float relativeStepsSum)
        {
            _computedAbsoluteSize = absoluteSizeLeft / relativeStepsSum * Value.Evaluate(context);
        }

        public override string ToString()
        {
            return $"{(Prefix.HasValue ? (char)Prefix.Value : '\0')}{Value.ToString()}: {Successor.ToString()}";
        }
    }

    public class SplitPattern : ISplitter
    {
        private float _computedAbsoluteSize;
        public IEnumerable<ISplitter> Steps { get; }
        public bool HasSwitch { get; }

        public SplitPattern(IEnumerable<ISplitter> steps, bool hasSwitch)
        {
            Steps = steps ?? throw new ArgumentNullException(nameof(steps));
            HasSwitch = hasSwitch;
            _computedAbsoluteSize = 0;
        }

        public Stack<ISymbol> Split(ExecutionContext context, Axis axis, Shape parentShape, float offset = 0)
        {
            var totalAbsoluteSize = GetAbsoluteSize(context);
            var absoluteSizesSum = Steps.Sum(x => x.GetAbsoluteSize(context));
            var absoluteSizeLeft = totalAbsoluteSize - absoluteSizesSum;
            var relativeStepsSum = Steps.Sum(x => x.GetRelativeStep(context));
            foreach (var step in Steps)
            {
                step.ComputeAbsoluteSize(context, absoluteSizeLeft, relativeStepsSum);
            }
            var splitStrategy = SplitStrategyFactory.Create(parentShape.GetType());
            var localShape = splitStrategy.Split(parentShape, axis, offset, totalAbsoluteSize);
            var newSymbols = new Stack<ISymbol>();
            float localOffset = 0;
            foreach (var step in Steps)
            {
                newSymbols.SafePushAll(step.Split(context, axis, localShape, localOffset));
                localOffset += step.GetAbsoluteSize(context);
            }
            return newSymbols;
        }

        public float GetRelativeStep(ExecutionContext context)
        {
            return 1;
        }

        public float GetAbsoluteSize(ExecutionContext context)
        {
            return _computedAbsoluteSize;
        }

        public void ComputeAbsoluteSize(ExecutionContext context, float absoluteSizeLeft, float relativeStepsSum)
        {
            _computedAbsoluteSize = absoluteSizeLeft / relativeStepsSum;
        }

        public override string ToString() => $"{{ {string.Join(" | ", Steps.Select(x => x.ToString()))} }}{(HasSwitch ? "*" : "")}";
    }

    public class SplitOperation : ShapeOperation
    {
        public Axis Axis => GetParam<Axis>(0);
        public AdjustSelector Adjust => GetParam<AdjustSelector>(1);
        public bool HasAdjust => Params[1] is AdjustSelector;
        public SplitPattern Pattern => GetParam<SplitPattern>(HasAdjust ? 2 : 1);
        public bool HasSwitch => GetParam<bool>(Params.Count - 1);

        public SplitOperation(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override Stack<ISymbol> Rewrite(ExecutionContext context)
        {
            var shape = context.PopShape();
            Pattern.ComputeAbsoluteSize(context, shape.Size.GetElement((int)Axis), 1);
            return Pattern.Split(context, Axis, shape);
        }

        public override string ToString() => $"split({Axis.ToString().ToLower()}) {Pattern.ToString()}";
    }

    // FIXME: Marker interface is a bad design!
    public interface IStatement
    {
    }

    public class ProductionRule : IStatement
    {
        public Symbol Predecessor { get; }
        public IEnumerable<ISymbol> Successors { get; }

        public ProductionRule(Symbol predecessor, IList<ISymbol> successors)
        {
            Predecessor = predecessor ?? throw new ArgumentNullException(nameof(predecessor));
            Successors = successors ?? throw new ArgumentNullException(nameof(successors));
        }

        public Stack<ISymbol> Run(ExecutionContext context)
        {
            var newSymbols = new Stack<ISymbol>();
            foreach (var successor in Successors)
            {
                newSymbols.SafePushAll(successor.Rewrite(context));
            }
            return newSymbols;
        }

        public override string ToString() => $"{Predecessor} --> {string.Join(" ", Successors.Select(x => x.ToString()))}";
    }

    public class Attribute : IStatement
    {
        public string Name { get; }
        public float Value { get; }

        public Attribute(string name, float value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

        public override string ToString() => $"attr {Name} = {Value.ToString()}";
    }

    public class ParseTree
    {
        private readonly IEnumerable<IStatement> _statements;
        public IEnumerable<Attribute> Attributes => GetStatements<Attribute>();
        public IEnumerable<ProductionRule> ProductionRules => GetStatements<ProductionRule>();

        public ParseTree(IEnumerable<IStatement> statements)
        {
            _statements = statements ?? throw new ArgumentNullException(nameof(statements));
        }

        private IEnumerable<T> GetStatements<T>() where T: IStatement
        {
            return _statements.Where(x => x.GetType() == typeof(T)).Cast<T>();
        }

        public override string ToString() => $"{{\n{string.Join(",\n", _statements.Select(x => '\t' + x.ToString()))}\n}}";
    }

    public class Axiom
    {
        public Symbol Symbol { get; }
        public Shape Shape { get; }

        public Axiom(Symbol symbol, Shape shape)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Shape = shape ?? throw new ArgumentNullException(nameof(shape));
        }
    }

    public class ExecutionContext
    {
        private IDictionary<Symbol, ProductionRule> _productionRules;
        private IDictionary<string, Attribute> _attributes;
        private IList<Shape> IntermediaryShapes { get; } = new List<Shape>();
        private Stack<Shape> ShapesStack { get; } = new Stack<Shape>();
        public Axiom Axiom { get; }
        public Shape CurrentShape { get; private set; }
        public List<Shape> TerminalShapes { get; } = new List<Shape>();

        public ExecutionContext(ParseTree parseTree, Axiom axiom)
        {
            if (parseTree == null)
            {
                throw new ArgumentNullException(nameof(parseTree));
            }
            _productionRules = parseTree.ProductionRules.ToDictionary(x => x.Predecessor, y => y);
            _attributes = parseTree.Attributes.ToDictionary(x => x.Name, y => y);
            Axiom = axiom ?? throw new ArgumentNullException(nameof(axiom));
        }

        public Shape PopShape()
        {
            CurrentShape = ShapesStack.Pop();
            return CurrentShape;
        }

        public void PushShape(Shape shape)
        {
            ShapesStack.Push(shape);
            IntermediaryShapes.Add(shape.Copy());
        }

        private Stack<ISymbol> RunTerminalRule()
        {
            TerminalShapes.Add(ShapesStack.Pop());
            return null;
        }

        public Stack<ISymbol> RunRule(Symbol symbol)
        {
            if (_productionRules.TryGetValue(symbol, out ProductionRule productionRule))
            {
                return productionRule.Run(this);
            }
            return RunTerminalRule();
        }

        public float GetAttributeValue(string attributeName)
        {
            if (_attributes.TryGetValue(attributeName, out Attribute attribute))
            {
                return attribute.Value;
            }
            // TODO:
            throw new Exception();
        }
    }

    public class Parser
    {
        public ParseTree Parse(string content)
        {
            var parser = new PegasusParser();
            try
            {
                return parser.Parse(content);
            }
            catch (FormatException e)
            {
                var cursor = (Cursor)e.Data["cursor"];
                var newException = new FormatException(e.Message);
                newException.Data["column"] = cursor.Column;
                newException.Data["line"] = cursor.Line;
                newException.Data["location"] = cursor.Location;
                throw newException;
            }
        }
    }

    public class Interpreter
    {
        public IReadOnlyCollection<Shape> Run(ParseTree parseTree, Axiom axiom)
        {
            var context = new ExecutionContext(parseTree, axiom);
            var symbols = new Stack<ISymbol>();
            context.PushShape(axiom.Shape);
            symbols.Push(axiom.Symbol);
            while (symbols.Count > 0)
            {
                var symbol = symbols.Pop();
                symbols.SafePushAll(symbol.Rewrite(context));
            }
            return context.TerminalShapes.AsReadOnly();
        }
    }
}
