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
            // TODO:
            else throw new Exception();
        }

        public static void SetElement(this Vector3 vector, int i, float value)
        {
            if (i == 0) vector.X = value;
            else if (i == 1) vector.Y = value;
            else if (i == 2) vector.Z = value;
            // TODO:
            else throw new Exception();
        }

        public static Vector3 GetPosition(this Matrix4x4 matrix)
        {
            return new Vector3(matrix.M41, matrix.M42, matrix.M43);
        }

        public static void SetPosition(this Matrix4x4 matrix, Vector3 value)
        {
            matrix.M41 = value.X;
            matrix.M42 = value.Y;
            matrix.M43 = value.Z;
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

        public static void PushAll<T>(this Stack<T> stack, IEnumerable<T> elements)
        {
            foreach (var element in elements)
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
        IEnumerable<ISymbol> Rewrite(ExecutionContext context);
    }

    public class Symbol : ISymbol
    {
        public string Name { get; }

        public Symbol(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString()
        {
            return Name;
        }

        public IEnumerable<ISymbol> Rewrite(ExecutionContext context)
        {
            context.MatchRule(Name)?.Run(context);
            return Enumerable.Empty<ISymbol>();
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

        public override string ToString()
        {
            return $"{Operand}({string.Join(", ", Params.Select(x => x.ToString()))})";
        }

        public T GetParam<T>(int i)
        {
            return (T)Params[i];
        }

        public abstract IEnumerable<ISymbol> Rewrite(ExecutionContext context);
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

        public override IEnumerable<ISymbol> Rewrite(ExecutionContext context)
        {
            var T = Matrix4x4.CreateTranslation(new Vector3(X, Y, Z));
            var shape = context.PopShape();
            shape.Transform = T * shape.Transform;
            context.PushShape(shape);
            return Enumerable.Empty<ISymbol>();
        }
    }

    public class Rotate : SRT
    {
        public Rotate(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override IEnumerable<ISymbol> Rewrite(ExecutionContext context)
        {
            var R = Matrix4x4.CreateFromYawPitchRoll(X.ToRadians(), Y.ToRadians(), Z.ToRadians());
            var shape = context.PopShape();
            shape.Transform = R * shape.Transform;
            context.PushShape(shape);
            return Enumerable.Empty<ISymbol>();
        }
    }

    public class Scale : SRT
    {
        public Scale(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override IEnumerable<ISymbol> Rewrite(ExecutionContext context)
        {
            var newShape = context.PopShape();
            newShape.Size = new Vector3(X, Y, Z);
            context.PushShape(newShape);
            return Enumerable.Empty<ISymbol>();
        }
    }

    public class Extrude : Operation
    {
        public Axis Axis => (Params.Count == 1) ? Axis.Z : GetParam<Axis>(0);
        public float Height => (Params.Count == 1) ? GetParam<float>(0) : GetParam<float>(1);

        public Extrude(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override IEnumerable<ISymbol> Rewrite(ExecutionContext context)
        {
            throw new NotImplementedException();
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

        public override string ToString()
        {
            return $"{Selector.ToString().ToLower()}: {Successor.ToString()}";
        }
    }

    public interface IComponentSplitStrategy
    {
        IEnumerable<Shape> Split(Shape parentShape, ComponentSelector selector);
    }

    public class BoxComponentSplitStrategy : IComponentSplitStrategy
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
                        Transform = Matrix4x4.CreateTranslation(0, parentShape.Size.Y * 0.5f, 0) * Matrix4x4.CreateRotationX((float)Math.PI * 0.5f) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.X, 0, parentShape.Size.Z)
                    },
                    // bottom
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateTranslation(0, parentShape.Size.Y * -0.5f, 0) * Matrix4x4.CreateRotationX((float)Math.PI * -0.5f) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.X, 0, parentShape.Size.Z)
                    },
                    // front
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateRotationY((float)Math.PI) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.X, 0, parentShape.Size.Z)
                    },
                    // back
                    new Quad()
                    {
                        Transform = parentShape.Transform,
                        Size = new Vector3(parentShape.Size.X, 0, parentShape.Size.Z)
                    },
                    // left
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateTranslation(parentShape.Size.X * -0.5f, 0, 0) * Matrix4x4.CreateRotationY((float)Math.PI * -0.5f) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.Z, 0, parentShape.Size.X)
                    },
                    // right
                    new Quad()
                    {
                        Transform = Matrix4x4.CreateTranslation(parentShape.Size.X * 0.5f, 0, 0) * Matrix4x4.CreateRotationY((float)Math.PI * 0.5f) * parentShape.Transform,
                        Size = new Vector3(parentShape.Size.Z, 0, parentShape.Size.X)
                    },
                };
                return quads;
            }
            throw new NotImplementedException($"{nameof(BoxComponentSplitStrategy)} doesn't know how to split '{selector.ToString()}' (yet?)");
        }
    }

    public static class ComponentSplitStrategyFactory
    {
        public static IComponentSplitStrategy Create(Type shapeType)
        {
            if (shapeType == typeof(Box))
            {
                return new BoxComponentSplitStrategy();
            }
            throw new NotImplementedException($"No component split strategy found for shape type '{nameof(shapeType.Name)}'");
        }
    }

    public interface ISemanticSelectionStrategy
    {
        bool Select(Shape parentShape, Shape componentShape, SemanticSelector selector);
    }

    public class BoxSemanticSelectionStrategy : ISemanticSelectionStrategy
    {
        private static float _Epsilon = 0.0001f;
        private static float _DotThreshold = 1 - _Epsilon;

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
                        var pZ = parentShape.Transform.GetXAxis();
                        var dot = Vector3.Dot(pZ, cN);
                        return dot <= -_DotThreshold; /* Z and N are almost parallel and point to same direction */
                    }
                case SemanticSelector.BACK:
                    {
                        var pZ = parentShape.Transform.GetXAxis();
                        var dot = Vector3.Dot(pZ, cN);
                        return dot >= _DotThreshold; /* Z and N are almost parallel and point to opposite directions */
                    }
                case SemanticSelector.LEFT:
                    {
                        var pX = parentShape.Transform.GetXAxis();
                        var dot = Vector3.Dot(pX, cN);
                        return dot >= _DotThreshold; /* X and N are almost parallel and point to same direction */
                    }
                case SemanticSelector.RIGHT:
                    {
                        var pX = parentShape.Transform.GetYAxis();
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
                        var pY = parentShape.Transform.GetXAxis();
                        var absDot = Math.Abs(Vector3.Dot(pY, cN));
                        return absDot <= _Epsilon; /* Y and N are almost perpendicular */
                    }
                case SemanticSelector.ALL:
                    return true; /* permissive */
                default:
                    throw new Exception($"Unknown {nameof(SemanticSelector)} '{selector.ToString()}'");
            }
        }
    }

    public static class SemanticSelectionStrategyFactory
    {
        public static ISemanticSelectionStrategy Create(Type shapeType)
        {
            if (shapeType == typeof(Box))
            {
                return new BoxSemanticSelectionStrategy();
            }
            throw new NotImplementedException($"No component split strategy found for parent shape type '{nameof(shapeType.Name)}'");
        }
    }

    public class CompSplit : Operation
    {
        public ComponentSelector Selector => GetParam<ComponentSelector>(0);
        public IEnumerable<CompSplitArg> Args => Params.Skip(1).Cast<CompSplitArg>();

        public CompSplit(string operand, IList<object> @params) : base(operand, @params)
        {
        }

        public override IEnumerable<ISymbol> Rewrite(ExecutionContext context)
        {
            var shape = context.PopShape();
            var shapeType = shape.GetType();
            var shapeComponents = ComponentSplitStrategyFactory.Create(shapeType).Split(shape, Selector);
            var selectionStrategy = SemanticSelectionStrategyFactory.Create(shapeType);
            var newSymbols = new List<ISymbol>();
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
                    newSymbols.Add(arg.Successor);
                });
            }
            return newSymbols;
        }

        public override string ToString()
        {
            return $"split({Selector.ToString().ToLower()}) {{ {string.Join(" | ", Args.Select(x => x.ToString()))} }}";
        }
    }

    public interface ISplitter
    {
        IEnumerable<ISymbol> Split(ExecutionContext context, Axis axis, Shape shape, float offset);
        float GetRelativeStep();
        float GetAbsoluteSize();
        void ComputeAbsoluteSize(float absoluteSizeLeft, float relativeStepsSum);
    }

    public interface ISplitStrategy
    {
        Shape Split(Shape parentShape, Axis axis, float offset, float length);
    }

    public class BoxSplitStrategy : ISplitStrategy
    {
        public Shape Split(Shape parentShape, Axis axis, float offset, float length)
        {
            var newShape = parentShape.Copy();
            var oldOrigin = newShape.Transform.GetPosition();
            var halfOldLength = newShape.Size.GetElement((int)axis) * 0.5f;
            var originOffset = Vector3.Zero;
            originOffset.SetElement((int)axis, offset - halfOldLength);
            newShape.Transform.SetPosition(oldOrigin + originOffset);
            newShape.Size.SetElement((int)axis, length);
            return newShape;
        }
    }

    public static class SplitStrategyFactory
    {
        public static ISplitStrategy Create(Type shapeType)
        {
            if (shapeType == typeof(Box))
            {
                return new BoxSplitStrategy();
            }
            throw new NotImplementedException($"No split strategy found for shape type '{nameof(shapeType.Name)}'");
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

        public override string ToString()
        {
            return $"{(Prefix.HasValue ? (char)Prefix.Value : '\0')}{Value.ToString()}: {Successor.ToString()}";
        }

        public IEnumerable<ISymbol> Split(ExecutionContext context, Axis axis, Shape parentShape, float offset)
        {
            var absoluteSize = GetAbsoluteSize();
            var strategy = SplitStrategyFactory.Create(parentShape.GetType());
            context.PushShape(strategy.Split(parentShape, axis, offset, absoluteSize));
            return new[] { Successor };
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

        public IEnumerable<ISymbol> Split(ExecutionContext context, Axis axis, Shape parentShape, float offset = 0)
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
            var newSymbols = new List<ISymbol>();
            float localOffset = 0;
            foreach (var step in Steps)
            {
                newSymbols.AddRange(step.Split(context, axis, localShape, localOffset));
                localOffset += step.GetAbsoluteSize();
            }
            return newSymbols;
        }

        public override string ToString()
        {
            return $"{{ {string.Join(" | ", Steps.Select(x => x.ToString()))} }}{(HasSwitch ? "*" : "")}";
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

        public override IEnumerable<ISymbol> Rewrite(ExecutionContext context)
        {
            var shape = context.PopShape();
            Pattern.ComputeAbsoluteSize(shape.Size.GetElement((int)Axis), 1);
            return Pattern.Split(context, Axis, shape);
        }

        public override string ToString()
        {
            return $"split({Axis.ToString().ToLower()}) {Pattern.ToString()}";
        }
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

        public void Run(ExecutionContext context)
        {
            var symbolsToProcess = new Stack<ISymbol>(Successors.Reverse());
            while (symbolsToProcess.Count > 0)
            {
                var symbol = symbolsToProcess.Pop();
                symbolsToProcess.PushAll(symbol.Rewrite(context));
            }
        }

        public override string ToString()
        {
            return $"{Predecessor} --> {string.Join(" ", Successors.Select(x => x.ToString()))}";
        }
    }

    public class ParseTree
    {
        public IEnumerable<ProductionRule> ProductionRules { get; }

        public ParseTree(IEnumerable<ProductionRule> productionRules)
        {
            ProductionRules = productionRules;
        }

        public override string ToString()
        {
            return $"{{\n{string.Join(",\n", ProductionRules.Select(x => '\t' + x.ToString()))}\n}}";
        }
    }

    public class Axiom
    {
        public string SymbolName { get; }
        public Shape Shape { get; }

        public Axiom(string symbolName, Shape shape)
        {
            SymbolName = symbolName ?? throw new ArgumentNullException(nameof(symbolName));
            Shape = shape ?? throw new ArgumentNullException(nameof(shape));
        }
    }

    public class ExecutionContext
    {
        private IEnumerable<ProductionRule> ProductionRules { get; }
        private IList<Shape> IntermediaryShapes { get; } = new List<Shape>();
        private Stack<Shape> ShapesStack { get; } = new Stack<Shape>();
        public Axiom Axiom { get; }
        public IReadOnlyCollection<Shape> TerminalShapes => ShapesStack;

        public ExecutionContext(IEnumerable<ProductionRule> productionRules, Axiom axiom)
        {
            ProductionRules = productionRules ?? throw new ArgumentNullException(nameof(productionRules));
            Axiom = axiom ?? throw new ArgumentNullException(nameof(axiom));
            ShapesStack.Push(axiom.Shape.Copy());
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

        public ProductionRule MatchRule(string symbolName)
        {
            return ProductionRules.Where(x => x.Predecessor.Name == symbolName).FirstOrDefault();
        }
    }

    public class Interpreter
    {
        public IReadOnlyCollection<Shape> Run(ParseTree parseTree, Axiom axiom)
        {
            var context = new ExecutionContext(parseTree.ProductionRules, axiom);
            var startingRule = context.MatchRule(axiom.SymbolName);
            startingRule.Run(context);
            return context.TerminalShapes;
        }
    }
}
