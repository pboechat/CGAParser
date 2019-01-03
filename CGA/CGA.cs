using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CGA
{
    public static class NumericsExtensions
    {
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
            var other = obj as Symbol;
            if (other == null)
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

    public abstract class Operation : ISymbol
    {
        public string Operand { get; }
        public IList<object> Params { get; }

        public Operation(string operand, IList<object> @params)
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

    public abstract class SRT : Operation
    {
        public float X => GetParam<float>(0);
        public float Y => GetParam<float>(1);
        public float Z => GetParam<float>(2);

        public SRT(string operand, IList<object> @params) : base(operand, @params)
        {
        }
    }

    public class Translate : SRT
    {
        public Translate(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override Stack<ISymbol> Rewrite(ExecutionContext context)
        {
            var T = Matrix4x4.CreateTranslation(new Vector3(X, Y, Z));
            var shape = context.PopShape();
            shape.Transform = T * shape.Transform;
            context.PushShape(shape);
            return null;
        }
    }

    public class Rotate : SRT
    {
        public Rotate(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override Stack<ISymbol> Rewrite(ExecutionContext context)
        {
            var R = Matrix4x4.CreateFromYawPitchRoll(Y.ToRadians(), X.ToRadians(), Z.ToRadians());
            var shape = context.PopShape();
            shape.Transform = R * shape.Transform;
            context.PushShape(shape);
            return null;
        }
    }

    public class Scale : SRT
    {
        public Scale(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override Stack<ISymbol> Rewrite(ExecutionContext context)
        {
            var newShape = context.PopShape();
            newShape.Size = new Vector3(X, Y, Z);
            context.PushShape(newShape);
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
                Transform = Matrix4x4.CreateRotationX((float)Math.PI * -0.5f) * Matrix4x4.CreateTranslation(0, distance * 0.5f, 0) * parentShape.Transform,
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

    public class Extrude : Operation
    {
        public Axis Axis => (Params.Count == 1) ? Axis.Z : (Axis)GetParam<float>(0);
        public float Distance => (Params.Count == 1) ? GetParam<float>(0) : GetParam<float>(1);

        public Extrude(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override Stack<ISymbol> Rewrite(ExecutionContext context)
        {
            var shape = context.PopShape();
            var extrusionStrategy = ExtrusionStrategyFactory.Create(shape.GetType());
            context.PushShape(extrusionStrategy.Extrude(shape, Axis, Distance));
            return null;
        }
    }

    public class CompSplitArg
    {
        public SemanticSelector Selector { get; }
        public ISymbol Successor { get; }

        public CompSplitArg(SemanticSelector selector, ISymbol successor)
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
                var quads = new List<Quad>
                {
                    // top
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateRotationX((float)Math.PI * -0.5f) * Matrix4x4.CreateTranslation(0, parentShape.Size.Y * 0.5f, 0) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.X, 1, parentShape.Size.Z)
                    },
                    // bottom
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateRotationX((float)Math.PI * 0.5f) * Matrix4x4.CreateTranslation(0, -parentShape.Size.Y * -0.5f, 0) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.X, 1, parentShape.Size.Z)
                    },
                    // back
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateTranslation(0, 0, parentShape.Size.Z * 0.5f) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.X, 1, parentShape.Size.Z)
                    },
                    // front
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateRotationY(-(float)Math.PI) * Matrix4x4.CreateTranslation(0, 0, parentShape.Size.Z * -0.5f) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.X, 1, parentShape.Size.Z)
                    },
                    // right
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateRotationY((float)Math.PI * 0.5f) * Matrix4x4.CreateTranslation(parentShape.Size.X * 0.5f, 0, 0) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.Z, 1, parentShape.Size.X)
                    },
                    // left
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateRotationY((float)Math.PI * -0.5f) * Matrix4x4.CreateTranslation(parentShape.Size.X * -0.5f, 0, 0) * parentShape.Transform,
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

        public static int GetSide(Vector3 a, Vector3 b, Vector3 c)
        {
            return Math.Sign(a.X * b.Y * c.Z + a.Y * b.Z * c.X + a.Z * b.X * c.Y - a.Z * b.Y * c.X - a.Y * b.X * c.Z - a.X * b.Z * c.Y);
        }

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
                case SemanticSelector.FRONT:
                    {
                        var pZ = parentShape.Transform.GetZAxis();
                        var dot = Vector3.Dot(pZ, cN);
                        return dot >= _DotThreshold; /* Z and N are almost parallel and point to opposite directions */
                    }
                case SemanticSelector.BACK:
                    {
                        var pZ = parentShape.Transform.GetZAxis();
                        var dot = Vector3.Dot(pZ, cN);
                        return dot <= -_DotThreshold; /* Z and N are almost parallel and point to same direction */
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
                case SemanticSelector.TOP:
                    {
                        var pY = parentShape.Transform.GetYAxis();
                        var dot = Vector3.Dot(pY, cN);
                        return dot >= _DotThreshold; /* Y and N are almost parallel and point to same direction */
                    }
                case SemanticSelector.BOTTOM:
                    {
                        var pY = parentShape.Transform.GetYAxis();
                        var dot = Vector3.Dot(pY, cN);
                        return dot <= -_DotThreshold; /* Y and N are almost parallel and point to opposite directions */
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

    public class CompSplit : Operation
    {
        public ComponentSelector Selector => GetParam<ComponentSelector>(0);
        public IEnumerable<CompSplitArg> Args => Params.Skip(1).Cast<CompSplitArg>();

        public CompSplit(string operand, IList<object> @params) : base(operand, @params)
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
        Stack<ISymbol> Split(ExecutionContext context, Axis axis, Shape shape, float offset);
        float GetRelativeStep();
        float GetAbsoluteSize();
        void ComputeAbsoluteSize(float absoluteSizeLeft, float relativeStepsSum);
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
        public float Value { get; }
        public ISymbol Successor { get; }

        public SplitStep(SizePrefix? prefix, float value, ISymbol successor)
        {
            Prefix = prefix;
            Value = value;
            Successor = successor ?? throw new ArgumentNullException(nameof(successor));
            _computedAbsoluteSize = 0;
        }

        public Stack<ISymbol> Split(ExecutionContext context, Axis axis, Shape parentShape, float offset)
        {
            var absoluteSize = GetAbsoluteSize();
            var strategy = SplitStrategyFactory.Create(parentShape.GetType());
            context.PushShape(strategy.Split(parentShape, axis, offset, absoluteSize));
            return new Stack<ISymbol>(new[] { Successor });
        }

        public float GetRelativeStep()
        {
            return Prefix.HasValue && Prefix.Value == SizePrefix.RELATIVE ? Value : 0;
        }

        public float GetAbsoluteSize()
        {
            if (!Prefix.HasValue)
            {
                return Value;
            }
            if (Prefix.Value == SizePrefix.RELATIVE)
            {
                return _computedAbsoluteSize;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void ComputeAbsoluteSize(float absoluteSizeLeft, float relativeStepsSum)
        {
            _computedAbsoluteSize = absoluteSizeLeft / relativeStepsSum * Value;
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
            var totalAbsoluteSize = GetAbsoluteSize();
            var absoluteSizesSum = Steps.Sum(x => x.GetAbsoluteSize());
            var absoluteSizeLeft = totalAbsoluteSize - absoluteSizesSum;
            var relativeStepsSum = Steps.Sum(x => x.GetRelativeStep());
            foreach (var step in Steps)
            {
                step.ComputeAbsoluteSize(absoluteSizeLeft, relativeStepsSum);
            }
            var splitStrategy = SplitStrategyFactory.Create(parentShape.GetType());
            var localShape = splitStrategy.Split(parentShape, axis, offset, totalAbsoluteSize);
            var newSymbols = new Stack<ISymbol>();
            float localOffset = 0;
            foreach (var step in Steps)
            {
                newSymbols.SafePushAll(step.Split(context, axis, localShape, localOffset));
                localOffset += step.GetAbsoluteSize();
            }
            return newSymbols;
        }

        public float GetRelativeStep()
        {
            return 1;
        }

        public float GetAbsoluteSize()
        {
            return _computedAbsoluteSize;
        }

        public void ComputeAbsoluteSize(float absoluteSizeLeft, float relativeStepsSum)
        {
            _computedAbsoluteSize = absoluteSizeLeft / relativeStepsSum;
        }

        public override string ToString() => $"{{ {string.Join(" | ", Steps.Select(x => x.ToString()))} }}{(HasSwitch ? "*" : "")}";
    }

    public class Split : Operation
    {
        public Axis Axis => GetParam<Axis>(0);
        public AdjustSelector Adjust => GetParam<AdjustSelector>(1);
        public bool HasAdjust => Params[1] is AdjustSelector;
        public SplitPattern Pattern => GetParam<SplitPattern>(HasAdjust ? 2 : 1);
        public bool HasSwitch => GetParam<bool>(Params.Count - 1);

        public Split(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override Stack<ISymbol> Rewrite(ExecutionContext context)
        {
            var shape = context.PopShape();
            Pattern.ComputeAbsoluteSize(shape.Size.GetElement((int)Axis), 1);
            return Pattern.Split(context, Axis, shape);
        }

        public override string ToString() => $"split({Axis.ToString().ToLower()}) {Pattern.ToString()}";
    }

    public class ProductionRule
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

    public class ParseTree
    {
        public IEnumerable<ProductionRule> ProductionRules { get; }

        public ParseTree(IEnumerable<ProductionRule> productionRules)
        {
            ProductionRules = productionRules ?? throw new ArgumentNullException(nameof(productionRules));
        }

        public override string ToString() => $"{{\n{string.Join(",\n", ProductionRules.Select(x => '\t' + x.ToString()))}\n}}";
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
        private IDictionary<Symbol, ProductionRule> ProductionRules { get; }
        private IList<Shape> IntermediaryShapes { get; } = new List<Shape>();
        private Stack<Shape> ShapesStack { get; } = new Stack<Shape>();
        public Axiom Axiom { get; }
        public List<Shape> TerminalShapes { get; } = new List<Shape>();

        public ExecutionContext(IEnumerable<ProductionRule> productionRules, Axiom axiom)
        {
            if (productionRules == null)
            {
                throw new ArgumentNullException(nameof(productionRules));
            }
            ProductionRules = productionRules.ToDictionary(x => x.Predecessor, y => y);
            Axiom = axiom ?? throw new ArgumentNullException(nameof(axiom));
        }

        public Shape PopShape()
        {
            return ShapesStack.Pop();
        }

        public void PushShape(Shape shape)
        {
            ShapesStack.Push(shape);
            IntermediaryShapes.Add(shape.Copy());
        }

        public Stack<ISymbol> RunRule(Symbol symbol)
        {
            if (ProductionRules.TryGetValue(symbol, out ProductionRule productionRule))
            {
                return productionRule.Run(this);
            }
            return RunTerminalRule();
        }

        private Stack<ISymbol> RunTerminalRule()
        {
            TerminalShapes.Add(ShapesStack.Pop());
            return null;
        }
    }

    public class Interpreter
    {
        public IReadOnlyCollection<Shape> Run(ParseTree parseTree, Axiom axiom)
        {
            var context = new ExecutionContext(parseTree.ProductionRules, axiom);
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
