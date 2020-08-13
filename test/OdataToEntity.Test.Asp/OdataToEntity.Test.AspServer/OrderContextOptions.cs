﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using OdataToEntity.EfCore;
using System;

namespace OdataToEntity.Test.Model
{
    internal static class OrderContextOptions
    {
        public static readonly ILoggerFactory ConsoleLoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });

        public static EdmModel BuildDbEdmModel(IEdmModel oeEdmModel, bool useRelationalNulls)
        {
            IEdmModel orderEdmModel;
            OeEfCoreDataAdapter<OrderContext> orderDataAdapter;
            OeEfCoreDataAdapter<Order2Context> order2DataAdapter;

            Db.OeDataAdapter dataAdapter = oeEdmModel.GetDataAdapter(oeEdmModel.EntityContainer);
            if (dataAdapter.CreateDataContext() is DbContext dbContext)
                try
                {
                    orderDataAdapter = new OeEfCoreDataAdapter<OrderContext>(CreateOptions<OrderContext>(dbContext))
                    {
                        IsDatabaseNullHighestValue = dataAdapter.IsDatabaseNullHighestValue
                    };
                    orderEdmModel = orderDataAdapter.BuildEdmModelFromEfCoreModel();
                    order2DataAdapter = new OeEfCoreDataAdapter<Order2Context>(CreateOptions<Order2Context>(dbContext))
                    {
                        IsDatabaseNullHighestValue = dataAdapter.IsDatabaseNullHighestValue
                    };
                    return order2DataAdapter.BuildEdmModelFromEfCoreModel(orderEdmModel);
                }
                finally
                {
                    dataAdapter.CloseDataContext(dbContext);
                }

            orderDataAdapter = new OeEfCoreDataAdapter<OrderContext>(Create(useRelationalNulls))
            {
                IsDatabaseNullHighestValue = dataAdapter.IsDatabaseNullHighestValue
            };
            orderEdmModel = orderDataAdapter.BuildEdmModelFromEfCoreModel();
            order2DataAdapter = new OeEfCoreDataAdapter<Order2Context>(Create<Order2Context>(useRelationalNulls))
            {
                IsDatabaseNullHighestValue = dataAdapter.IsDatabaseNullHighestValue
            };
            return order2DataAdapter.BuildEdmModelFromEfCoreModel(orderEdmModel);
        }
        public static DbContextOptions<OrderContext> Create(bool useRelationalNulls)
        {
            return Create<OrderContext>(useRelationalNulls);
        }
        public static DbContextOptions<T> Create<T>(bool useRelationalNulls) where T : DbContext
        {
            var optionsBuilder = new DbContextOptionsBuilder<T>();
            optionsBuilder = optionsBuilder.UseSqlServer(@"Server=.\sqlexpress;Initial Catalog=OdataToEntity;Trusted_Connection=Yes;", opt => opt.UseRelationalNulls(useRelationalNulls));
            optionsBuilder.UseLoggerFactory(ConsoleLoggerFactory);
            return optionsBuilder.Options;
        }
        public static DbContextOptions CreateOptions<T>(DbContext dbContext) where T : DbContext
        {
            var serviceProvider = (IInfrastructure<IServiceProvider>)dbContext;
#pragma warning disable EF1001
            DbContextOptions options = (DbContextOptions)serviceProvider.GetService<IDbContextServices>().ContextOptions;
#pragma warning restore EF1001
            return options.CreateOptions(typeof(T));
        }
    }
}