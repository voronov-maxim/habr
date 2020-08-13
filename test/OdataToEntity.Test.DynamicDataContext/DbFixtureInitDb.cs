﻿using Microsoft.EntityFrameworkCore;
using Microsoft.OData;
using Microsoft.OData.Edm;
using OdataToEntity.EfCore.DynamicDataContext;
using OdataToEntity.EfCore.DynamicDataContext.InformationSchema;
using OdataToEntity.ModelBuilder;
using OdataToEntity.Test.Model;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OdataToEntity.Test
{
    public abstract class DbFixtureInitDb : DbFixture
    {
        private bool _initialized;
        private readonly IServiceProvider _serviceProvider;
        private static readonly ConcurrentDictionary<Type, EdmModel> _edmModels = new ConcurrentDictionary<Type, EdmModel>();

        protected DbFixtureInitDb(Type fixtureType, bool useRelationalNulls, ModelBoundTestKind modelBoundTestKind)
            : base(CreateEdmModel(fixtureType, useRelationalNulls), modelBoundTestKind, useRelationalNulls)
        {
            _serviceProvider = new DynamicDataContext.EnumServiceProvider(base.DbEdmModel);
        }

        public override OrderContext CreateContext()
        {
            Db.OeDataAdapter dataAdapter = OeEdmModel.GetDataAdapter(OeEdmModel.EntityContainer);
            var dbContext = (DbContext)dataAdapter.CreateDataContext();
            try
            {
                DbContextOptions options = TestHelper.CreateOptions<OrderContext>(dbContext);
                return new OrderContext(options);
            }
            finally
            {
                dataAdapter.CloseDataContext(dbContext);
            }
        }
        internal static EdmModel CreateEdmModel(Type fixtureType, bool useRelationalNulls)
        {
            return _edmModels.GetOrAdd(fixtureType, t => CreateEdmModel(useRelationalNulls));
        }
        private static EdmModel CreateEdmModel(bool useRelationalNulls)
        {
            EdmModel edmModel = CreateDynamicEdmModel(useRelationalNulls);
            edmModel.AddElement(OeEdmModelBuilder.CreateEdmEnumType(typeof(Sex)));
            edmModel.AddElement(OeEdmModelBuilder.CreateEdmEnumType(typeof(OrderStatus)));
            return edmModel;
        }
        private static EdmModel CreateDynamicEdmModel(bool useRelationalNulls)
        {
            ProviderSpecificSchema providerSchema = DynamicDataContext.Program.CreateSchemaSqlServer(useRelationalNulls);
            //ProviderSpecificSchema providerSchema = DynamicDataContext.Program.CreateSchemaPostgreSql(useRelationalNulls);
            //ProviderSpecificSchema providerSchema = DynamicDataContext.Program.CreateSchemaMySql(useRelationalNulls);

            InformationSchemaSettings informationSchemaSettings = DynamicDataContext.Program.GetSettings();
            using (var metadataProvider = providerSchema.CreateMetadataProvider(informationSchemaSettings))
            {
                var typeDefinitionManager = DynamicTypeDefinitionManager.Create(metadataProvider);
                var dataAdapter = new DynamicDataAdapter(typeDefinitionManager);
                return dataAdapter.BuildEdmModel(metadataProvider);
            }
        }
        public override async Task Execute<T, TResult>(QueryParameters<T, TResult> parameters)
        {
            var dataAdapter = (DynamicDataAdapter)base.OeEdmModel.GetDataAdapter(base.OeEdmModel.EntityContainer);
            if (dataAdapter.TypeDefinitionManager.ExpressionVisitor != null)
                parameters.Expression = (Expression<Func<IQueryable<T>, IQueryable<TResult>>>)dataAdapter.TypeDefinitionManager.ExpressionVisitor.Visit(parameters.Expression);
            parameters.Expression = (Expression<Func<IQueryable<T>, IQueryable<TResult>>>)new EfCore.Fix.FixSelectDistinctVisitor().Visit(parameters.Expression);

            Task t1 = base.Execute(parameters);
            Task t2 = base.Execute(parameters);
            await Task.WhenAll(t1, t2);
        }
        public override async Task Execute<T, TResult>(QueryParametersScalar<T, TResult> parameters)
        {
            Task t1 = base.Execute(parameters);
            Task t2 = base.Execute(parameters);
            await Task.WhenAll(t1, t2);
        }
        public async override Task Initalize()
        {
            if (_initialized)
                return;

            _initialized = true;
            var parser = new OeParser(new Uri("http://dummy/"), base.OeEdmModel);
            ODataUri odataUri = ParseUri("ResetDb");
            await parser.ExecuteOperationAsync(odataUri, OeRequestHeaders.JsonDefault, null, new MemoryStream(), CancellationToken.None);
            await ExecuteBatchAsync(base.OeEdmModel, "Add", new DynamicDataContext.EnumServiceProvider(base.OeEdmModel));
        }
        public override ODataUri ParseUri(String requestUri)
        {
            if (requestUri == "ResetDb" || requestUri == "TableFunction" || requestUri.StartsWith("GetOrders") || requestUri.StartsWith("TableFunctionWithParameters"))
                return base.ParseUri("dbo." + requestUri);

            return base.ParseUri(ReplaceEnum(requestUri, '\''));
        }
        private static String ReplaceEnum(String requestUri, Char quotationMark)
        {
            requestUri = ReplaceEnum(typeof(Sex), requestUri, quotationMark);
            return ReplaceEnum(typeof(OrderStatus), requestUri, quotationMark);
        }
        private static String ReplaceEnum(Type enumType, String requestUri, Char quotationMark)
        {
            foreach (String name in Enum.GetNames(enumType))
            {
                int i = requestUri.IndexOf(quotationMark.ToString() + name + quotationMark.ToString());
                if (i != -1)
                {
                    int j = i - enumType.FullName.Length;
                    int len = name.Length + 2;
                    if (j > 0 && String.CompareOrdinal(requestUri, j, enumType.FullName, 0, enumType.FullName.Length) == 0)
                    {
                        i = j;
                        len += enumType.FullName.Length;
                    }

                    int value = ((int)Enum.Parse(enumType, name));
                    requestUri = requestUri.Substring(0, i) + value.ToString() + requestUri.Substring(i + len);
                }
            }

            return requestUri;
        }
        public override String SerializeRequestData(Object requestData)
        {
            return ReplaceEnum(base.SerializeRequestData(requestData), '"');
        }

        public override IServiceProvider ServiceProvider => _serviceProvider;
    }

    public abstract class ManyColumnsFixtureInitDb : DbFixture
    {
        private bool _initialized;

        protected ManyColumnsFixtureInitDb(Type fixtureType, bool useRelationalNulls, ModelBoundTestKind modelBoundTestKind)
            : base(DbFixtureInitDb.CreateEdmModel(fixtureType, useRelationalNulls), modelBoundTestKind, useRelationalNulls)
        {
        }

        public override OrderContext CreateContext()
        {
            throw new NotImplementedException();
        }
        public override async Task Execute<T, TResult>(QueryParameters<T, TResult> parameters)
        {
            Task t1 = base.Execute(parameters);
            Task t2 = base.Execute(parameters);
            await Task.WhenAll(t1, t2);
        }
        public override async Task Execute<T, TResult>(QueryParametersScalar<T, TResult> parameters)
        {
            Task t1 = base.Execute(parameters);
            Task t2 = base.Execute(parameters);
            await Task.WhenAll(t1, t2);
        }
        public async override Task Initalize()
        {
            if (_initialized)
                return;

            _initialized = true;
            var parser = new OeParser(new Uri("http://dummy/"), base.OeEdmModel);
            ODataUri odataUri = OeParser.ParseUri(base.OeEdmModel, new Uri("dbo.ResetManyColumns", UriKind.Relative));
            await parser.ExecuteOperationAsync(odataUri, OeRequestHeaders.JsonDefault, null, new MemoryStream(), CancellationToken.None);
            await DbFixture.ExecuteBatchAsync(base.OeEdmModel, "ManyColumns", null);
        }
    }
}
