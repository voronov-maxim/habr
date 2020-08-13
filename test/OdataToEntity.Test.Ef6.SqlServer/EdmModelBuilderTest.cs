﻿using Microsoft.OData.Edm;
using OdataToEntity.Ef6;
using OdataToEntity.EfCore;
using OdataToEntity.Test.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace OdataToEntity.Test
{
    public class EdmModelBuilderTest
    {
        internal class MyFinanceDbContext : DbContext
        {
            public DbSet<Acct> Accts { get; set; }
            public DbSet<Dept> Depts { get; set; }
            public DbSet<Stat> Stats { get; set; }

            static MyFinanceDbContext()
            {
                Database.SetInitializer<MyFinanceDbContext>(null);
            }
            public MyFinanceDbContext() : base(@"Server =.\sqlexpress; Initial Catalog = OdataToEntity; Trusted_Connection=Yes;")
            {
            }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<Dept>().HasIndex(d => d.Ref).IsUnique();
                modelBuilder.Entity<Dept>().HasKey(d => d.Ref).HasMany(d => d.AcctRefs).WithRequired(a => a.DeptRefNavigation).HasForeignKey(a => a.DeptRef);
            }
        }

        private static String FixNamesInSchema(String schema)
        {
            Type efCore = typeof(OrderContext);
            Type ef6 = typeof(Ef6.SqlServer.OrderEf6Context);

            XDocument xdoc = XDocument.Parse(schema);
            List<XElement> xschemas = xdoc.Root.Descendants().Where(x => x.Name.LocalName == "Schema").ToList();

            foreach (XElement xelement in xschemas[1].Elements())
            {
                XAttribute xattribute = xelement.Attribute("Name");
                xattribute.SetValue(xattribute.Value.Replace(ef6.Name, efCore.Name));
                xschemas[0].Add(xelement);
            }
            xschemas[1].Remove();

            var annotations = xschemas[0].Elements().Where(x => x.Name.LocalName == "Annotations").ToList();
            foreach (XElement annotation in annotations)
                annotation.Remove();
            xschemas[0].Add(annotations);

            using (var stream = new MemoryStream())
            using (var xwriter = XmlWriter.Create(stream, new XmlWriterSettings() { Indent = true, Encoding = new UTF8Encoding(false) }))
            {
                xdoc.WriteTo(xwriter);
                xwriter.Flush();
                String fixSchema = Encoding.UTF8.GetString(stream.ToArray());
                return fixSchema.Replace(ef6.Namespace, efCore.Namespace);
            }
        }
        [Fact]
        public void FluentApi()
        {
            var ethalonDataAdapter = new OeEfCoreDataAdapter<OrderContext>(OrderContextOptions.Create(true));
            EdmModel ethalonEdmModel = ethalonDataAdapter.BuildEdmModel();
            String ethalonSchema = TestHelper.GetCsdlSchema(ethalonEdmModel);
            if (ethalonSchema == null)
                throw new InvalidOperationException("Invalid ethalon schema");

            var testDataAdapter = new OrderDataAdapter(false, false);
            EdmModel testEdmModel = testDataAdapter.BuildEdmModelFromEf6Model();
            String testSchema = TestHelper.GetCsdlSchema(testEdmModel);
            if (testSchema == null)
                throw new InvalidOperationException("Invalid test schema");

            String fixTestSchema = FixNamesInSchema(testSchema);
            Assert.Equal(ethalonSchema, fixTestSchema);
        }
        [Fact]
        public void MissingDependentNavigationProperty()
        {
            var da = new OeEf6DataAdapter<MyFinanceDbContext>();
            da.BuildEdmModelFromEf6Model();
        }
    }
}
