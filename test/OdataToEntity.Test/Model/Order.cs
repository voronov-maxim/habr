﻿using OdataToEntity.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OdataToEntity.Test.Model
{
    [Table("Categories", Schema = "dbo")]
    public sealed class Category
    {
        [Page(NavigationNextLink = true)]
        public ICollection<Category> Children { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public String Name { get; set; }
        public Category Parent { get; set; }
        public int? ParentId { get; set; }
        public DateTime? DateTime { get; set; }
    }

    [Table("Customers", Schema = "dbo")]
    public sealed class Customer
    {
        public Customer()
        {
            Address = OpenTypeConverter.NotSetString;
            Country = OpenTypeConverter.NotSetString;
            Id = int.MinValue;
            Name = OpenTypeConverter.NotSetString;
            Sex = (Sex)Int32.MinValue;
        }

        public String Address { get; set; }
        [InverseProperty(nameof(Order.AltCustomer))]
        [Expand(SelectExpandType.Disabled)]
        public ICollection<Order> AltOrders { get; set; }
        [Key, Column(Order = 0), Required]
        public String Country { get; set; }
        public ICollection<CustomerShippingAddress> CustomerShippingAddresses { get; set; }
        [Key, Column(Order = 1)]
        public int Id { get; set; }
        [Required]
        public String Name { get; set; }
        [InverseProperty(nameof(Order.Customer))]
        [Count(Disabled = true)]
        [Expand(SelectExpandType.Automatic)]
        public ICollection<Order> Orders { get; set; }
        public Sex? Sex { get; set; }
        [NotMapped]
        public ICollection<ShippingAddress> ShippingAddresses { get; set; }

        public override String ToString() => "Customer: " + "Country = " + Country + ", Id = " + Id.ToString();
    }

    public abstract class OrderBase
    {
        public OrderBase()
        {
            AltCustomerCountry = OpenTypeConverter.NotSetString;
            AltCustomerId = Int32.MinValue;
            Name = OpenTypeConverter.NotSetString;
        }

        //[ForeignKey(nameof(AltCustomer)), Column(Order = 0)]
        public String AltCustomerCountry { get; set; }
        //[ForeignKey(nameof(AltCustomer)), Column(Order = 1)]
        public int? AltCustomerId { get; set; }
        [Required]
        [Select(SelectExpandType.Automatic)]
        public String Name { get; set; }
    }

    [Table("Orders", Schema = "dbo")]
    [Count]
    [Page(MaxTop = 2, PageSize = 1)]
    public sealed class Order : OrderBase
    {
        public Order()
        {
            CustomerCountry = OpenTypeConverter.NotSetString;
            CustomerId = Int32.MinValue;
            Date = DateTimeOffset.MinValue;
            Id = Int32.MinValue;
            Status = (OrderStatus)Int32.MinValue;
        }

        [ForeignKey("AltCustomerCountry,AltCustomerId")]
        public Customer AltCustomer { get; set; }
        [ForeignKey("CustomerCountry,CustomerId")]
        [Expand(SelectExpandType.Automatic)]
        [Select("Name", "Sex", SelectType = SelectExpandType.Automatic)]
        public Customer Customer { get; set; }
        [Required]
        public String CustomerCountry { get; set; }
        public int CustomerId { get; set; }
        [Select(SelectExpandType.Automatic)]
        public DateTimeOffset? Date { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Count]
        [Expand(SelectExpandType.Automatic)]
        [OrderBy("Id")]
        [Page(MaxTop = 2, PageSize = 1)]
        public ICollection<OrderItem> Items { get; set; }
        public ICollection<ShippingAddress> ShippingAddresses { get; set; }
        [Select(SelectExpandType.Automatic)]
        public OrderStatus Status { get; set; }

        public override String ToString() => "Order: Id = " + Id.ToString();
    }

    [Table("OrderItems", Schema = "dbo")]
    [Count(Disabled = true)]
    [OrderBy(Disabled = true)]
    public sealed class OrderItem
    {
        public OrderItem()
        {
            Count = Int32.MinValue;
            Id = Int32.MinValue;
            OrderId = Int32.MinValue;
            Price = Decimal.MinValue;
            Product = OpenTypeConverter.NotSetString;
        }

        public int? Count { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Select(SelectExpandType.Disabled)]
        [Filter(Disabled = true)]
        public int Id { get; set; }
        public Order Order { get; set; }
        [Select(SelectExpandType.Disabled)]
        public int OrderId { get; set; }
        public Decimal? Price { get; set; }
        [Required]
        public String Product { get; set; }

        public override String ToString() => "OrderItem: Id = " + Id.ToString();
    }

    [Table("ShippingAddresses", Schema = "dbo")]
    public sealed class ShippingAddress
    {
        [Required]
        public String Address { get; set; }
        [NotMapped]
        public ICollection<Customer> Customers { get; set; }
        public ICollection<CustomerShippingAddress> CustomerShippingAddresses { get; set; }
        [Key, Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        [Key, Column(Order = 0)]
        public int OrderId { get; set; }

        public override String ToString() => "ShippingAddress: OrderId = " + OrderId.ToString() + ", Id = " + Id.ToString();
    }

    public sealed class CustomerShippingAddress
    {
        public CustomerShippingAddress()
        {
            CustomerCountry = OpenTypeConverter.NotSetString;
            CustomerId = Int32.MinValue;
            ShippingAddressOrderId = Int32.MinValue;
            ShippingAddressId = Int32.MinValue;
        }

        [ForeignKey("CustomerCountry,CustomerId")]
        public Customer Customer { get; set; }
        [Key, Column(Order = 0)]
        public String CustomerCountry { get; set; }
        [Key, Column(Order = 1)]
        public int CustomerId { get; set; }
        [ForeignKey("ShippingAddressOrderId,ShippingAddressId")]
        public ShippingAddress ShippingAddress { get; set; }
        [Key, Column(Order = 2)]
        public int ShippingAddressOrderId { get; set; }
        [Key, Column(Order = 3)]
        public int ShippingAddressId { get; set; }
    }

    public sealed class OrderItemsView
    {
        [Required] //ef6 FluentApi test 
        public String Name { get; set; }
        [Required] //ef6 FluentApi test
        public String Product { get; set; }
    }

    public class ManyColumnsBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Column01 { get; set; }
        public int Column02 { get; set; }
        public int Column03 { get; set; }
        public int Column04 { get; set; }
        public int Column05 { get; set; }
        public int Column06 { get; set; }
        public int Column07 { get; set; }
        public int Column08 { get; set; }
        public int Column09 { get; set; }
        public int Column10 { get; set; }
        public int Column11 { get; set; }
        public int Column12 { get; set; }
        public int Column13 { get; set; }
        public int Column14 { get; set; }
        public int Column15 { get; set; }
        public int Column16 { get; set; }
        public int Column17 { get; set; }
        public int Column18 { get; set; }
        public int Column19 { get; set; }
        public int Column20 { get; set; }
        public int Column21 { get; set; }
        public int Column22 { get; set; }
        public int Column23 { get; set; }
        public int Column24 { get; set; }
        public int Column25 { get; set; }
        public int Column26 { get; set; }
        public int Column27 { get; set; }
        public int Column28 { get; set; }
        public int Column29 { get; set; }
        public int Column30 { get; set; }
    }

    public sealed class ManyColumns : ManyColumnsBase
    {
    }

    [Table("ManyColumns", Schema = "dbo")]
    public sealed class ManyColumns2 : ManyColumnsBase
    {
    }

    public sealed class ManyColumnsView : ManyColumnsBase
    {
    }

    public sealed class CustomerOrdersCount
    {
        public String CustomerName { get; set; }
        public int OrderCount { get; set; }
    }

    public enum OrderStatus
    {
        Unknown,
        Processing,
        Shipped,
        Delivering,
        Cancelled
    }

    public enum Sex
    {
        Male,
        Female
    }

    //test buil edm model
    internal class Dept
    {
        public int Id { get; set; }
        public byte[] Ref { get; set; }

        //public virtual ICollection<Acct> Accts { get; set; } // Adding fixes issue
        public virtual ICollection<Stat> Stats { get; set; }
        public ICollection<Acct> AcctRefs { get; set; }
    }

    internal class Acct
    {
        public int Id { get; set; }

        public int? DeptId { get; set; }
        public virtual Dept Dept { get; set; }

        public byte[] DeptRef { get; set; }
        public Dept DeptRefNavigation { get; set; }
    }

    internal class Stat
    {
        public int Id { get; set; }

        public int? DeptId { get; set; }
        public virtual Dept Dept { get; set; }
    }

    public class Car
    {
        public int Id { get; set; }

        // Missing: `public int StateId { get; set; }`
        public virtual State State { get; set; }
    }

    public class State
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Car> Cars { get; set; }
    }
}
