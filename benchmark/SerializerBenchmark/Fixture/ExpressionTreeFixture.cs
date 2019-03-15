using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Benchmark.Fixture
{
    public class ExpressionTreeFixture
    {
        private readonly ConcurrentDictionary<Type, Func<int, int, object>> _functorCache =
            new ConcurrentDictionary<Type, Func<int, int, object>>();

        private readonly Dictionary<Type, IValueFixture> _valueFixtures = new Dictionary<Type, IValueFixture>();

        public ExpressionTreeFixture()
        {
            IValueFixture[] fixtures = new IValueFixture[]
            {
                new StringValueFixture(),
                new IntValueFixture(),
                new GuidValueFixture(),
                new DateTimeOffsetValueFixture(),
                new DateTimeValueFixture(),
                new BooleanValueFixture(),
                new DecimalValueFixture(),
                new LongValueFixture(),
                new FloatValueFixture(),
                new DoubleValueFixture(),
                new ByteValueFixture(),
                new ShortValueFixture(),
                new ByteArrayFixture(),
                new SByteValueFixture(),
                new UShortValueFixture(),
                new UInt32ValueFixture(),
                new UInt64ValueFixture(),
                new CharValueFixture()
            };

            foreach (var item in fixtures)
            {
                _valueFixtures.Add(item.Type, item);
            }
        }

        public object Create(Type type, int repeatCount = 1, int recursiveCount = 1)
        {
            var functor = _functorCache.GetOrAdd(type, AddFunctor);
            return functor(repeatCount, recursiveCount);
        }

        private Func<int, int, object> AddFunctor(Type type)
        {
            var repeatCount = Expression.Parameter(typeof(int), "repeatCount");
            var recursiveCount = Expression.Parameter(typeof(int), "recursiveCount");
            var subExpressions = new List<Expression>();
            var typedOutput = Expression.Variable(type, "typedOutput");
            if (_valueFixtures.ContainsKey(type) || type.IsArray || type.IsTypedList()
            ) // they can be generated directly
            {
                var expression = GenerateValue(typedOutput, repeatCount, recursiveCount, type);
                subExpressions.Add(expression);
            }
            else
            {
                subExpressions.Add(Expression.Assign(typedOutput, Expression.New(type)));
                var typeProps = type.GetProperties();
                foreach (var propertyInfo in typeProps)
                {
                    if (!propertyInfo.CanWrite)
                    {
                        continue;
                    }
                    var propertyType = propertyInfo.PropertyType;
                    var isRecursion = IsRecursion(type, propertyType) || IsRecursion(propertyType, type);
                    var memberAccess = Expression.MakeMemberAccess(typedOutput, propertyInfo);
                    var expression = GenerateValue(memberAccess, repeatCount,
                        isRecursion ? Expression.Decrement(recursiveCount) : (Expression)recursiveCount, propertyType);
                    subExpressions.Add(expression);
                }
            }
            var returnTarget = Expression.Label(typeof(object));
            var returnLabel = Expression.Label(returnTarget, Expression.Convert(typedOutput, typeof(object)));
            subExpressions.Add(returnLabel);
            var block = Expression.Block(new[] { typedOutput }, subExpressions);
            var lambda = Expression.Lambda<Func<int, int, object>>(block, repeatCount, recursiveCount);
            return lambda.Compile();
        }

        private bool IsRecursion(Type parentType, Type type)
        {
            if (type == parentType)
            {
                return true;
            }
            if (parentType.IsTypedList())
            {
                var childType = parentType.GetGenericArguments()[0];
                return IsRecursion(type, childType);
            }
            if (parentType.IsArray)
            {
                var elementType = parentType.GetElementType();
                return IsRecursion(type, elementType);
            }
            if (Nullable.GetUnderlyingType(parentType) != null)
            {
                var nullableType = Nullable.GetUnderlyingType(parentType);
                return IsRecursion(type, nullableType);
            }
            return false;
        }

        private Expression GenerateValue(Expression generatedValue, Expression repeatCount, Expression recursiveCount,
            Type type)
        {
            var result = new List<Expression>();
            if (_valueFixtures.TryGetValue(type, out var valueFixture))
            {
                var generateMethodInfo = valueFixture.GetType().GetMethod(nameof(IValueFixture.Generate));
                result.Add(Expression.Assign(generatedValue,
                    Expression.Convert(Expression.Call(Expression.Constant(valueFixture), generateMethodInfo),
                        generatedValue.Type)));
            }
            else if (type.IsTypedList())
            {
                var expressionList = new List<Expression>();
                var elementType = type.GetGenericArguments()[0];
                expressionList.Add(Expression.Assign(generatedValue, Expression.New(type)));
                var index = Expression.Parameter(typeof(int), "i");
                var addMi = type.GetMethod("Add", new[] { elementType });
                var childValue = Expression.Parameter(elementType);
                var loopBlock = new List<Expression>
                {
                    GenerateValue(childValue, repeatCount, recursiveCount, elementType),
                    Expression.Call(generatedValue, addMi, childValue)
                };
                if (loopBlock.Count > 0)
                {
                    var loopContent = Expression.Block(new[] { childValue, index }, loopBlock);
                    expressionList.Add(ForLoop(index, repeatCount, loopContent));
                }
                result.Add(MakeIfExpression(recursiveCount, expressionList));
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var index = Expression.Parameter(typeof(int), "i");
                var arrayList = new List<Expression>
                {
                    Expression.Assign(generatedValue, Expression.NewArrayBounds(elementType, repeatCount)),
                    ForLoop(index, repeatCount,
                        GenerateValue(Expression.ArrayAccess(generatedValue, index), repeatCount, recursiveCount,
                            elementType))
                };
                result.Add(MakeIfExpression(recursiveCount, arrayList));
            }
            else if (Nullable.GetUnderlyingType(type) != null)
            {
                var elementType = Nullable.GetUnderlyingType(type);
                result.Add(GenerateValue(generatedValue, repeatCount, recursiveCount, elementType));
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                if (!_valueFixtures.TryGetValue(type, out valueFixture))
                {
                    valueFixture = new EnumValueFixture(type);
                    _valueFixtures.Add(valueFixture.Type, valueFixture);
                }
                result.Add(GenerateValue(generatedValue, repeatCount, recursiveCount,
                    type)); // call again for main method
            }
            else
            {
                result.Add(MakeIfExpression(recursiveCount,
                    InvokeCreate(type, generatedValue, repeatCount, recursiveCount)));
            }
            return result.Count > 1 ? Expression.Block(result) : result.Single();
        }

        private Expression InvokeCreate(Type type, Expression generatedValue, Expression repeatCount,
            Expression recursiveCount)
        {
            var mi = typeof(ExpressionTreeFixture).GetMethod(nameof(Create),
                new[] { typeof(Type), typeof(int), typeof(int) });
            return Expression.Assign(generatedValue,
                Expression.Convert(
                    Expression.Call(Expression.Constant(this), mi,
                        new[] { Expression.Constant(type), repeatCount, recursiveCount }),
                    generatedValue.Type));
        }

        private Expression MakeIfExpression(Expression recursiveCount, params Expression[] input)
        {
            return Expression.IfThen(Expression.GreaterThanOrEqual(recursiveCount, Expression.Constant(0)),
                input.Length > 1 ? Expression.Block(input) : input.Single());
        }

        private Expression MakeIfExpression(Expression recursiveCount, IList<Expression> input)
        {
            return Expression.IfThen(Expression.GreaterThanOrEqual(recursiveCount, Expression.Constant(0)),
                input.Count > 1 ? Expression.Block(input) : input.Single());
        }

        public T Create<T>(int repeatCount = 1, int recursiveCount = 1)
        {
            return (T)Create(typeof(T), repeatCount, recursiveCount);
        }

        public IEnumerable<T> CreateMany<T>(int count, int repeatCount = 1, int recursiveCount = 1)
        {
            return CreateMany(typeof(T), count, repeatCount, recursiveCount).Cast<T>();
        }

        public IEnumerable<object> CreateMany(Type type, int count, int repeatCount = 1, int recursiveCount = 1)
        {
            var functor = _functorCache.GetOrAdd(type, AddFunctor);
            for (var i = 0; i < count; i++)
            {
                yield return functor(repeatCount, recursiveCount);
            }
        }

        public static Expression ForLoop(ParameterExpression index, Expression lengthExpression, Expression loopContent)
        {
            var breakLabel = Expression.Label("LoopBreak");
            var length = Expression.Variable(typeof(int), "length");
            var block = Expression.Block(new[] { index, length },
                Expression.Assign(index, Expression.Constant(0)),
                Expression.Assign(length, lengthExpression),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.LessThan(index, length),
                        Expression.Block(loopContent, Expression.PostIncrementAssign(index)),
                        Expression.Break(breakLabel)
                    ),
                    breakLabel)
            );
            return block;
        }
    }
}