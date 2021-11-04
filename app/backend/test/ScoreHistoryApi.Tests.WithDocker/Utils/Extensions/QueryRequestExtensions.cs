using System;
using Amazon.DynamoDBv2.Model;

namespace ScoreHistoryApi.Tests.WithDocker.Utils.Extensions
{
    public static class QueryRequestExtensions
    {
        public static QueryRequest SetNamesAndValue<T>(this QueryRequest request, T obj)
        {
            foreach (var propertyInfo in obj.GetType().GetProperties())
            {
                var name = propertyInfo.Name;

                var nameParam = $"#{name}";
                request.ExpressionAttributeNames[nameParam] = name;

                var valueParam = $":{name}";
                request.ExpressionAttributeValues[valueParam] = Convert(propertyInfo.GetValue(obj));
            }

            return request;

            static AttributeValue Convert(object value)
            {
                return value switch
                {
                    string str => new AttributeValue(str),
                    int _ => new AttributeValue {N = value.ToString()},
                    long _ => new AttributeValue { N = value.ToString() },
                    _ => throw new NotSupportedException($"'{value.GetType()}' is not support")
                };
            }
        }

        public static QueryRequest SetKeyConditionExpression(this QueryRequest request, string expression)
        {
            request.KeyConditionExpression = expression;
            return request;
        }
    }
}
