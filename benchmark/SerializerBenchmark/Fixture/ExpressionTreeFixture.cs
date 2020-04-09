// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private readonly ConcurrentDictionary<Type, Func<int, int, object>> functorCache =
            new ConcurrentDictionary<Type, Func<int, int, object>>();

        private readonly Dictionary<Type, IValueFixture> valueFixtures = new Dictionary<Type, IValueFixture>();

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
                new CharValueFixture(),
            };

            foreach (IValueFixture item in fixtures)
            {
                this.valueFixtures.Add(item.Type, item);
            }
        }

        public object Create(Type type, int repeatCount = 1, int recursiveCount = 1)
        {
            Func<int, int, object> functor = this.functorCache.GetOrAdd(type, this.AddFunctor);
            return functor(repeatCount, recursiveCount);
        }

        private Func<int, int, object> AddFunctor(Type type)
        {
            ParameterExpression repeatCount = Expression.Parameter(typeof(int), "repeatCount");
            ParameterExpression recursiveCount = Expression.Parameter(typeof(int), "recursiveCount");
            var subExpressions = new List<Expression>();
            ParameterExpression typedOutput = Expression.Variable(type, "typedOutput");
            if (this.valueFixtures.ContainsKey(type) || type.IsArray || type.IsTypedList())
            {
                // they can be generated directly
                Expression expression = this.GenerateValue(typedOutput, repeatCount, recursiveCount, type);
                subExpressions.Add(expression);
            }
            else
            {
                subExpressions.Add(Expression.Assign(typedOutput, Expression.New(type)));
                PropertyInfo[] typeProps = type.GetProperties();
                foreach (PropertyInfo propertyInfo in typeProps)
                {
                    if (!propertyInfo.CanWrite)
                    {
                        continue;
                    }

                    Type propertyType = propertyInfo.PropertyType;
                    var isRecursion = this.IsRecursion(type, propertyType) || this.IsRecursion(propertyType, type);
                    MemberExpression memberAccess = Expression.MakeMemberAccess(typedOutput, propertyInfo);
                    Expression expression = this.GenerateValue(
                        memberAccess,
                        repeatCount,
                        isRecursion ? Expression.Decrement(recursiveCount) : (Expression)recursiveCount,
                        propertyType);
                    subExpressions.Add(expression);
                }
            }

            LabelTarget returnTarget = Expression.Label(typeof(object));
            LabelExpression returnLabel = Expression.Label(returnTarget, Expression.Convert(typedOutput, typeof(object)));
            subExpressions.Add(returnLabel);
            BlockExpression block = Expression.Block(new[] { typedOutput }, subExpressions);
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
                Type childType = parentType.GetGenericArguments()[0];
                return this.IsRecursion(type, childType);
            }

            if (parentType.IsArray)
            {
                Type elementType = parentType.GetElementType();
                return this.IsRecursion(type, elementType);
            }

            if (Nullable.GetUnderlyingType(parentType) != null)
            {
                Type nullableType = Nullable.GetUnderlyingType(parentType);
                return this.IsRecursion(type, nullableType);
            }

            return false;
        }

        private Expression GenerateValue(Expression generatedValue, Expression repeatCount, Expression recursiveCount, Type type)
        {
            var result = new List<Expression>();
            if (this.valueFixtures.TryGetValue(type, out IValueFixture valueFixture))
            {
                MethodInfo generateMethodInfo = valueFixture.GetType().GetMethod(nameof(IValueFixture.Generate));
                result.Add(Expression.Assign(
                    generatedValue,
                    Expression.Convert(Expression.Call(Expression.Constant(valueFixture), generateMethodInfo), generatedValue.Type)));
            }
            else if (type.IsTypedList())
            {
                var expressionList = new List<Expression>();
                Type elementType = type.GetGenericArguments()[0];
                expressionList.Add(Expression.Assign(generatedValue, Expression.New(type)));
                ParameterExpression index = Expression.Parameter(typeof(int), "i");
                MethodInfo addMi = type.GetMethod("Add", new[] { elementType });
                ParameterExpression childValue = Expression.Parameter(elementType);
                var loopBlock = new List<Expression>
                {
                    this.GenerateValue(childValue, repeatCount, recursiveCount, elementType),
                    Expression.Call(generatedValue, addMi, childValue),
                };
                if (loopBlock.Count > 0)
                {
                    BlockExpression loopContent = Expression.Block(new[] { childValue, index }, loopBlock);
                    expressionList.Add(ForLoop(index, repeatCount, loopContent));
                }

                result.Add(this.MakeIfExpression(recursiveCount, expressionList));
            }
            else if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                ParameterExpression index = Expression.Parameter(typeof(int), "i");
                var arrayList = new List<Expression>
                {
                    Expression.Assign(generatedValue, Expression.NewArrayBounds(elementType, repeatCount)),
                    ForLoop(
                        index,
                        repeatCount,
                        this.GenerateValue(Expression.ArrayAccess(generatedValue, index), repeatCount, recursiveCount, elementType)),
                };
                result.Add(this.MakeIfExpression(recursiveCount, arrayList));
            }
            else if (Nullable.GetUnderlyingType(type) != null)
            {
                Type elementType = Nullable.GetUnderlyingType(type);
                result.Add(this.GenerateValue(generatedValue, repeatCount, recursiveCount, elementType));
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                if (!this.valueFixtures.TryGetValue(type, out valueFixture))
                {
                    valueFixture = new EnumValueFixture(type);
                    this.valueFixtures.Add(valueFixture.Type, valueFixture);
                }

                result.Add(this.GenerateValue(generatedValue, repeatCount, recursiveCount, type)); // call again for main method
            }
            else
            {
                result.Add(this.MakeIfExpression(recursiveCount, this.InvokeCreate(type, generatedValue, repeatCount, recursiveCount)));
            }

            return result.Count > 1 ? Expression.Block(result) : result.Single();
        }

        private Expression InvokeCreate(Type type, Expression generatedValue, Expression repeatCount, Expression recursiveCount)
        {
            MethodInfo mi = typeof(ExpressionTreeFixture).GetMethod(nameof(this.Create), new[] { typeof(Type), typeof(int), typeof(int) });
            return Expression.Assign(
                generatedValue,
                Expression.Convert(
                    Expression.Call(Expression.Constant(this), mi, new[] { Expression.Constant(type), repeatCount, recursiveCount }),
                    generatedValue.Type));
        }

        private Expression MakeIfExpression(Expression recursiveCount, params Expression[] input)
        {
            return Expression.IfThen(
                Expression.GreaterThanOrEqual(recursiveCount, Expression.Constant(0)),
                input.Length > 1 ? Expression.Block(input) : input.Single());
        }

        private Expression MakeIfExpression(Expression recursiveCount, IList<Expression> input)
        {
            return Expression.IfThen(
                Expression.GreaterThanOrEqual(recursiveCount, Expression.Constant(0)),
                input.Count > 1 ? Expression.Block(input) : input.Single());
        }

        public T Create<T>(int repeatCount = 1, int recursiveCount = 1)
        {
            return (T)this.Create(typeof(T), repeatCount, recursiveCount);
        }

        public IEnumerable<T> CreateMany<T>(int count, int repeatCount = 1, int recursiveCount = 1)
        {
            return this.CreateMany(typeof(T), count, repeatCount, recursiveCount).Cast<T>();
        }

        public IEnumerable<object> CreateMany(Type type, int count, int repeatCount = 1, int recursiveCount = 1)
        {
            Func<int, int, object> functor = this.functorCache.GetOrAdd(type, this.AddFunctor);
            for (var i = 0; i < count; i++)
            {
                yield return functor(repeatCount, recursiveCount);
            }
        }

        public static Expression ForLoop(ParameterExpression index, Expression lengthExpression, Expression loopContent)
        {
            LabelTarget breakLabel = Expression.Label("LoopBreak");
            ParameterExpression length = Expression.Variable(typeof(int), "length");
            BlockExpression block = Expression.Block(
                new[] { index, length },
                Expression.Assign(index, Expression.Constant(0)),
                Expression.Assign(length, lengthExpression),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.LessThan(index, length),
                        Expression.Block(loopContent, Expression.PostIncrementAssign(index)),
                        Expression.Break(breakLabel)),
                    breakLabel));
            return block;
        }
    }
}
