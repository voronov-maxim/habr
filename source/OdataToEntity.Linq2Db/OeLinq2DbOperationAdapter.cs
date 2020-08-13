﻿using LinqToDB;
using LinqToDB.Data;
using OdataToEntity.Db;
using OdataToEntity.ModelBuilder;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace OdataToEntity.Linq2Db
{
    public class OeLinq2DbOperationAdapter : OeOperationAdapter
    {
        public OeLinq2DbOperationAdapter(Type dataContextType)
            : base(dataContextType)
        {
        }

        protected override IAsyncEnumerable<Object> ExecuteNonQuery(Object dataContext, String sql, IReadOnlyList<KeyValuePair<String, Object?>> parameters)
        {
            var dataConnection = (DataConnection)dataContext;
            dataConnection.Execute(sql, GetDataParameters(parameters));
            return Infrastructure.AsyncEnumeratorHelper.Empty;
        }
        protected override IAsyncEnumerable<Object> ExecuteReader(Object dataContext, String sql, IReadOnlyList<KeyValuePair<String, Object?>> parameters, OeEntitySetAdapter entitySetAdapter)
        {
            return ExecuteReader(dataContext, sql, parameters, entitySetAdapter.EntityType);
        }
        private IAsyncEnumerable<Object> ExecuteReader(Object dataContext, String sql, IReadOnlyList<KeyValuePair<String, Object?>> parameters, Type retuenType)
        {
            Func<DataConnection, String, DataParameter[], IEnumerable<Object>> queryMethod;
            if (sql.StartsWith("select ", StringComparison.Ordinal))
                queryMethod = DataConnectionExtensions.Query<Object>;
            else
                queryMethod = DataConnectionExtensions.QueryProc<Object>;

            MethodInfo queryMethodInfo = queryMethod.GetMethodInfo().GetGenericMethodDefinition().MakeGenericMethod(new Type[] { retuenType });
            Type queryMethodType = typeof(Func<DataConnection, String, DataParameter[], IEnumerable<Object>>);
            var queryFunc = (Func<DataConnection, String, DataParameter[], IEnumerable<Object>>)Delegate.CreateDelegate(queryMethodType, queryMethodInfo);

            IEnumerable<Object> result = queryFunc((DataConnection)dataContext, sql, GetDataParameters(parameters));
            return Infrastructure.AsyncEnumeratorHelper.ToAsyncEnumerable(result);
        }
        protected override IAsyncEnumerable<Object> ExecutePrimitive(Object dataContext, String sql,
            IReadOnlyList<KeyValuePair<String, Object?>> parameters, Type returnType, CancellationToken cancellationToken)
        {
            Type? itemType = Parsers.OeExpressionHelper.GetCollectionItemTypeOrNull(returnType);
            if (itemType == null)
            {
                Task<Object> result = ((DataConnection)dataContext).ExecuteAsync<Object>(sql, GetDataParameters(parameters));
                return Infrastructure.AsyncEnumeratorHelper.ToAsyncEnumerable(result);
            }

            return ExecuteReader(dataContext, sql, parameters, itemType);
        }
        private DataParameter[] GetDataParameters(IReadOnlyList<KeyValuePair<String, Object?>> parameters)
        {
            var dataParameters = new DataParameter[parameters.Count];
            for (int i = 0; i < dataParameters.Length; i++)
            {
                Object? value = GetParameterCore(parameters[i], null, i);
                if (value is DataParameter dataParameter)
                    dataParameters[i] = dataParameter;
                else
                    dataParameters[i] = new DataParameter(parameters[i].Key, value);
            }
            return dataParameters;
        }
        protected override String[] GetParameterNames(Object dataContext, IReadOnlyList<KeyValuePair<String, Object?>> parameters)
        {
            var parameterNames = new String[parameters.Count];
            for (int i = 0; i < parameterNames.Length; i++)
                parameterNames[i] = "@" + parameters[i].Key;
            return parameterNames;
        }
        protected override String GetProcedureName(Object dataContext, String operationName, IReadOnlyList<KeyValuePair<String, Object?>> parameters)
        {
            return GetOperationCaseSensitivityName(operationName, null);
        }
        protected override IReadOnlyList<OeOperationConfiguration>? GetOperationConfigurations(MethodInfo methodInfo)
        {
            var dbFunction = (Sql.FunctionAttribute)methodInfo.GetCustomAttribute(typeof(Sql.FunctionAttribute));
            if (dbFunction == null || dbFunction.Name == null)
                return base.GetOperationConfigurations(methodInfo);

            return new[] { new OeOperationConfiguration(dbFunction.Configuration, dbFunction.Name, methodInfo, true) };
        }
    }
}
