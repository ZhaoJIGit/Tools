using System;
using System.Collections.Concurrent;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.Sqlite;

namespace TaskMGPro.Helper
{
    public static class Mapper
    {
        private static readonly ConcurrentDictionary<Type, Func<SqliteDataReader, object>> _mapperCache = new ConcurrentDictionary<Type, Func<SqliteDataReader, object>>();
        private static readonly ConcurrentDictionary<(Type, Type), Delegate> Cache = new ConcurrentDictionary<(Type, Type), Delegate>();
        /// <summary>
        /// Reader mapper
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T Map<T>(SqliteDataReader reader)
        {
            var entityType = typeof(T);

            // 尝试从缓存中获取映射函数
            if (_mapperCache.TryGetValue(entityType, out var mapper))
            {
                return (T)mapper(reader);
            }

            // 如果缓存中没有映射函数，则动态生成映射函数并添加到缓存中
            var mapperFunc = CreateMapperFunc<T>();
            _mapperCache.TryAdd(entityType, mapperFunc);

            return (T)mapperFunc(reader);
        }

        private static Func<SqliteDataReader, object> CreateMapperFunc<T>()
        {
            var readerType = typeof(SqliteDataReader);
            var entityType = typeof(T);
            var readerParam = System.Linq.Expressions.Expression.Parameter(readerType, "reader");

            var entity = System.Linq.Expressions.Expression.Variable(entityType, "entity");
            var entityAssign = System.Linq.Expressions.Expression.Assign(entity, System.Linq.Expressions.Expression.New(entityType));
            //用于创建一个表示一系列语句的表达式块。这个方法可以用来组合多个表达式
            var body = System.Linq.Expressions.Expression.Block(
                new[] { entity },
                entityAssign,
                //用于创建一个表示方法调用的表达式。这个方法允许您动态地构建方法调用表达式，从而可以在运行时动态地调用方法
                System.Linq.Expressions.Expression.Call(
                    typeof(Mapper),
                    nameof(MapColumnsToProperties),
                    null,
                    entity,
                    readerParam
                ),
                entity
            );

            var lambda = System.Linq.Expressions.Expression.Lambda<Func<SqliteDataReader, object>>(body, readerParam);
            return lambda.Compile();
        }

        private static void MapColumnsToProperties(object entity, SqliteDataReader reader)
        {
            var entityType = entity.GetType();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var propertyName = reader.GetName(i);
                var propertyInfo = entityType.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {

                    // 跳过所有复杂类型
                    if (propertyInfo.PropertyType.IsClass && propertyInfo.PropertyType != typeof(string) ||
                        propertyInfo.PropertyType.IsArray ||
                        typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType) && propertyInfo.PropertyType != typeof(string))
                    {
                        continue;
                    }

                    var value = reader[propertyInfo.Name];
                    if (value != DBNull.Value)
                    {
                        if (propertyInfo.PropertyType.IsEnum) // 如果是枚举类型
                        {
                            propertyInfo.SetValue(entity, Enum.Parse(propertyInfo.PropertyType, value.ToString()));
                        }
                        // 根据属性的类型进行转换
                        else if (propertyInfo.PropertyType == typeof(DateTime)) // 如果是 DateTime 类型
                        {
                            propertyInfo.SetValue(entity, Convert.ToDateTime(value));
                        }
                        else if (propertyInfo.PropertyType == typeof(bool)) // 如果是布尔类型
                        {
                            // 这里可以根据具体的数据库存储方式进行转换，比如 1/0、true/false、T/F 等
                            propertyInfo.SetValue(entity, Convert.ToBoolean(value));
                        }
                        else if (propertyInfo.PropertyType == typeof(string)) // 如果是字符串类型
                        {
                            propertyInfo.SetValue(entity, Convert.ToString(value));
                        }
                        else if (propertyInfo.PropertyType == typeof(long)) // 如果是字符串类型
                        {
                            propertyInfo.SetValue(entity, Convert.ToInt64(value));
                        }
                        else
                        {
                            propertyInfo.SetValue(entity, value);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 对象赋值 mapper
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static TTarget Map<TTarget>(object source)
        {
            var targetType = typeof(TTarget);
            var sourceType = source.GetType();
            var key = (sourceType, targetType);

            if (!Cache.TryGetValue(key, out var mapper))
            {
                mapper = CreateMap<TTarget>((dynamic)source);
                Cache.TryAdd(key, mapper);
            }

            return ((Func<object, TTarget>)mapper)(source);
        }

        private static Func<object, TTarget> CreateMap<TTarget>(object source)
        {
            var targetType = typeof(TTarget);
            var sourceType = source.GetType();

            var sourceParam = Expression.Parameter(typeof(object), "source");

            var bindings = new List<MemberBinding>();

            foreach (var targetProperty in targetType.GetProperties())
            {
                var sourceProperty = sourceType.GetProperty(targetProperty.Name);
                if (sourceProperty != null && sourceProperty.CanRead)
                {
                    Expression sourceExpr = Expression.Property(Expression.Convert(sourceParam, sourceType), sourceProperty);
                    Expression targetExpr;

                    // 处理枚举类型
                    if (targetProperty.PropertyType.IsEnum)
                    {
                        targetExpr = Expression.Convert(Expression.Constant(Enum.Parse(targetProperty.PropertyType, sourceExpr.ToString())), targetProperty.PropertyType);
                    }
                    // 处理 DateTime 类型
                    else if (targetProperty.PropertyType == typeof(DateTime) && sourceProperty.PropertyType == typeof(string))
                    {
                        targetExpr = Expression.Call(typeof(DateTime), "Parse", null, sourceExpr);
                    }
                    // 处理布尔类型
                    else if (targetProperty.PropertyType == typeof(bool) && sourceProperty.PropertyType == typeof(string))
                    {
                        targetExpr = Expression.Call(typeof(bool), "Parse", null, sourceExpr);
                    }
                    // 处理字符串类型
                    else if (targetProperty.PropertyType == typeof(string) && sourceProperty.PropertyType != typeof(string))
                    {
                        targetExpr = Expression.Call(sourceExpr, "ToString", null);
                    }
                    // 默认情况
                    else
                    {
                        targetExpr = Expression.Convert(sourceExpr, targetProperty.PropertyType);
                    }

                    bindings.Add(Expression.Bind(targetProperty, targetExpr));
                }
            }

            var memberInit = Expression.MemberInit(Expression.New(targetType), bindings);
            var lambda = Expression.Lambda<Func<object, TTarget>>(memberInit, sourceParam);
            return lambda.Compile();
        }

        /// <summary>
        /// 泛型集合转换 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static List<T> ToObjectList<T>(this SqliteDataReader reader) where T : new()
        {
            List<T> list = new List<T>();
            if (reader != null && reader.FieldCount > 0)
            {
                while (reader.Read())
                {
                    list.Add(Mapper.Map<T>(reader));
                }
            }
            return list;
        }
        /// <summary>
        /// 泛型对象转换
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T ToObject<T>(this SqliteDataReader reader) where T : new()
        {
            T obj = new T();
            if (reader != null && reader.FieldCount>0)
            {
                reader.Read();
                obj = Mapper.Map<T>(reader);
            }
            else
            {
                return default(T);
            }
            return obj;
        }
    }
}
