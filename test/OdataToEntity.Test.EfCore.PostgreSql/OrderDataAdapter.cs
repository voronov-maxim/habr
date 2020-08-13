﻿using OdataToEntity.EfCore;
using OdataToEntity.EfCore.Postgresql;

namespace OdataToEntity.Test.Model
{
    public sealed class OrderDataAdapter : EfCorePostgreSqlDataAdapter<OrderContext>
    {
        public OrderDataAdapter(bool allowCache, bool useRelationalNulls) :
            base(OrderContextOptions.Create<OrderContext>(useRelationalNulls), new Cache.OeQueryCache(allowCache))
        {
        }

        public static ModelBuilder.OeEdmModelMetadataProvider CreateMetadataProvider()
        {
            using (var dbContext = new OrderContext(OrderContextOptions.Create<OrderContext>(true)))
                return new OeEfCoreEdmModelMetadataProvider(dbContext.Model);
        }
    }

    public sealed class Order2DataAdapter : EfCorePostgreSqlDataAdapter<Order2Context>
    {
        public Order2DataAdapter(bool allowCache, bool useRelationalNulls) :
            base(OrderContextOptions.Create<Order2Context>(useRelationalNulls), new Cache.OeQueryCache(allowCache))
        {
        }
    }
}
