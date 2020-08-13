﻿using Microsoft.EntityFrameworkCore;
using OdataToEntity.Test.Model;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OdataToEntity.Test
{
    public sealed class BatchTest
    {
        [Fact]
        public async Task Add()
        {
            var fixture = new RDBNull_DbFixtureInitDb();
            await fixture.Initalize().ConfigureAwait(false);

            using (var orderContext = fixture.CreateContext())
            {
                Assert.Equal(8, orderContext.Categories.Count());
                Assert.Equal(4, orderContext.Customers.Count());
                Assert.Equal(4, orderContext.Orders.Count());
                Assert.Equal(7, orderContext.OrderItems.Count());
                Assert.Equal(5, orderContext.ShippingAddresses.Count());
                Assert.Equal(5, orderContext.CustomerShippingAddress.Count());

                var category = orderContext.Categories.Single(t => t.Name == "jackets");
                Assert.Equal("clothes", orderContext.Categories.Single(t => t.Id == category.ParentId).Name);
                Assert.Equal(2, orderContext.Categories.AsQueryable().Where(t => t.ParentId == category.Id).Count());

                var order1 = orderContext.Orders.Include(t => t.Items).Single(t => t.Name == "Order 1");
                Assert.Equal(3, order1.Items.Count());

                var order2 = orderContext.Orders.Include(t => t.Items).Single(t => t.Name == "Order 2");
                Assert.Equal(2, order2.Items.Count());

                var order3 = orderContext.Orders.Include(t => t.Items).Single(t => t.Name == "Order unknown");
                Assert.Equal(2, order3.Items.Count());
            }
        }
        [Fact]
        public async Task Delete()
        {
            var fixture = new RDBNull_DbFixtureInitDb();
            await fixture.Initalize().ConfigureAwait(false);

            await DbFixture.ExecuteBatchAsync(fixture.OeEdmModel, "Delete").ConfigureAwait(false);
            using (var orderContext = fixture.CreateContext())
            {
                Assert.Equal(5, orderContext.Categories.Count());
                Assert.Equal(4, orderContext.Customers.Count());
                Assert.Equal(3, orderContext.Orders.Count());
                Assert.Equal(3, orderContext.OrderItems.Count());
                Assert.Equal(2, orderContext.ShippingAddresses.Count());
                Assert.Equal(2, orderContext.CustomerShippingAddress.Count());

                var order1 = orderContext.Orders.Include(t => t.Items).Single(t => t.Name == "Order 1");
                Assert.Equal("Product order 1 item 3", order1.Items.Single().Product);
            }
        }
        [Fact]
        public async Task Update()
        {
            var fixture = new RDBNull_DbFixtureInitDb();
            await fixture.Initalize().ConfigureAwait(false);

            await DbFixture.ExecuteBatchAsync(fixture.OeEdmModel, "Update").ConfigureAwait(false);
            using (var orderContext = fixture.CreateContext())
            {
                var category = orderContext.Categories.Single(t => t.Name == "sombrero jacket");
                Assert.Equal("jackets", orderContext.Categories.Single(t => t.Id == category.ParentId).Name);

                Assert.Equal(4, orderContext.Customers.Count());
                Assert.Equal(4, orderContext.Orders.Count());
                Assert.Equal(7, orderContext.OrderItems.Count());

                var order1 = orderContext.Orders.Include(t => t.Items).Single(t => t.Id == 1);
                Assert.Equal("New Order 1", order1.Name);
                Assert.Equal("New Product order 1 item 3", order1.Items.Single(t => t.Id == 3).Product);

                Assert.Equal(Sex.Female, orderContext.Customers.Single(c => c.Country == "RU" && c.Id == 1).Sex);
                Assert.Null(orderContext.Customers.Single(c => c.Country == "EN" && c.Id == 1).Sex);
            }
        }
    }
}
