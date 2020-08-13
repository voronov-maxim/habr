﻿using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace OdataToEntity.Linq2Db
{
    public abstract class OeLinq2DbTable
    {
        protected readonly struct UpdatableEntity<T> where T : class
        {
            public UpdatableEntity(T entity, IReadOnlyList<String> updatedPropertyNames)
            {
                Entity = entity;
                UpdatedPropertyNames = updatedPropertyNames;
            }

            public T Entity { get; }
            public IReadOnlyList<String> UpdatedPropertyNames { get; }
        }

        public abstract bool IsKey(PropertyInfo propertyInfo);
        public abstract int SaveDeleted(DataConnection dc);
        public abstract int SaveInserted(DataConnection dc);
        public abstract int SaveUpdated(DataConnection dc);
        public abstract void UpdateIdentities(PropertyInfo fkeyProperty, IReadOnlyDictionary<Object, Object> identities);

        public abstract IReadOnlyDictionary<Object, Object> Identities { get; }
    }

    public sealed class OeLinq2DbTable<T> : OeLinq2DbTable where T : class
    {
        private readonly List<T> _deleted;
        private readonly Dictionary<Object, Object> _identities;
        private readonly List<T> _inserted;
        private static readonly PropertyInfo[] _primaryKey = new OeLinq2DbEdmModelMetadataProvider().GetPrimaryKey(typeof(T));
        private readonly List<UpdatableEntity<T>> _updated;

        public OeLinq2DbTable()
        {
            _deleted = new List<T>();
            _inserted = new List<T>();
            _identities = new Dictionary<Object, Object>();
            _updated = new List<UpdatableEntity<T>>();
        }

        public void Delete(T entity)
        {
            _deleted.Add(entity);
        }
        private static List<PropertyInfo> GetDatabaseGenerated()
        {
            var identityProperties = new List<PropertyInfo>();
            foreach (PropertyInfo property in typeof(T).GetProperties())
                if (property.GetCustomAttribute(typeof(IdentityAttribute)) != null)
                    identityProperties.Add(property);
            return identityProperties;
        }
        public void Insert(T entity)
        {
            _inserted.Add(entity);
        }
        public override bool IsKey(PropertyInfo propertyInfo)
        {
            return Array.IndexOf(_primaryKey, propertyInfo) != -1;
        }
        private static void OrderBy(PropertyInfo? selfRefProperty, PropertyInfo keyProperty, List<T> items)
        {
            if (selfRefProperty == null || items.Count == 0)
                return;

            for (int i = 0; i < items.Count; i++)
            {
                Object parentKey = selfRefProperty.GetValue(items[i]);
                if (parentKey == null)
                    continue;

                for (int j = i; j < items.Count; j++)
                {
                    bool found = false;
                    for (int k = j; k < items.Count; k++)
                    {
                        Object key = keyProperty.GetValue(items[k]);
                        if (key.Equals(parentKey))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        T temp = items[i];
                        items[i] = items[j];
                        items[j] = temp;
                        break;
                    }
                }
            }
        }
        public override int SaveDeleted(DataConnection dc)
        {
            if (SelfRefProperty == null)
            {
                foreach (T entity in Deleted)
                    dc.Delete(entity);
                return Deleted.Count;
            }

            List<PropertyInfo> identityProperties = GetDatabaseGenerated();
            OrderBy(SelfRefProperty, identityProperties[0], _deleted);
            for (int i = _deleted.Count - 1; i >= 0; i--)
                dc.Delete(_deleted[i]);
            return Deleted.Count;
        }
        public override int SaveInserted(DataConnection dc)
        {
            List<PropertyInfo> identityProperties = GetDatabaseGenerated();
            if (identityProperties.Count == 0)
            {
                foreach (T entity in Inserted)
                    dc.Insert(entity);
                return Inserted.Count;
            }

            OrderBy(SelfRefProperty, identityProperties[0], _inserted);
            for (int i = 0; i < _inserted.Count; i++)
            {
                T entity = _inserted[i];
                Object identity = dc.InsertWithIdentity(entity);
                identity = Convert.ChangeType(identity, identityProperties[0].PropertyType, CultureInfo.InvariantCulture);
                Object old = identityProperties[0].GetValue(entity);
                identityProperties[0].SetValue(entity, identity);
                _identities.Add(old, identity);

                if (SelfRefProperty != null)
                    UpdateParentIdentity(old, identity, i, SelfRefProperty);
            }
            return Inserted.Count;
        }
        public override int SaveUpdated(DataConnection dc)
        {
            if (_updated.Count == 0)
                return 0;

            ITable<T> table = dc.GetTable<T>();
            foreach (UpdatableEntity<T> updatableEntity in _updated)
                if (updatableEntity.UpdatedPropertyNames == null)
                    dc.Update(updatableEntity.Entity);
                else
                    Updatable(table, updatableEntity.Entity, updatableEntity.UpdatedPropertyNames).Update();
            return _updated.Count;
        }
        private IUpdatable<T> Updatable(ITable<T> table, T entity, IReadOnlyList<String> updatedPropertyNames)
        {
            IUpdatable<T>? updatable = null;
            IQueryable<T> query = table.Where(EntityUpdateHelper.GetWhere(_primaryKey, entity));
            foreach (String updatedPropertyName in updatedPropertyNames.Except(_primaryKey.Select(p => p.Name)))
            {
                PropertyInfo updatedProperty = typeof(T).GetProperty(updatedPropertyName);
                SetBuilder<T> setBuilder = EntityUpdateHelper.GetSetBuilder<T>(updatedProperty);
                updatable = updatable == null ? setBuilder.GetSet(query, entity) : setBuilder.GetSet(updatable, entity);
            }
            return updatable ?? throw new InvalidOperationException("Table " + table.DatabaseName + " update properties is empty");
        }
        public void Update(T entity, IEnumerable<String> updatedPropertyNames)
        {
            _updated.Add(new UpdatableEntity<T>(entity, updatedPropertyNames.ToList()));
        }
        private void UpdateParentIdentity(Object oldIdentity, Object newIdentity, int entityIndex, PropertyInfo selfRefProperty)
        {
            if (newIdentity.Equals(oldIdentity))
                return;

            for (int i = entityIndex + 1; i < _inserted.Count; i++)
            {
                Object parentIdentity = selfRefProperty.GetValue(_inserted[i]);
                if (oldIdentity.Equals(parentIdentity))
                    selfRefProperty.SetValue(_inserted[i], newIdentity);
            }
        }
        public override void UpdateIdentities(PropertyInfo fkeyProperty, IReadOnlyDictionary<Object, Object> identities)
        {
            foreach (T entity in Inserted)
            {
                Object oldValue = fkeyProperty.GetValue(entity);
                if (oldValue != null && identities.TryGetValue(oldValue, out Object newValue))
                    fkeyProperty.SetValue(entity, newValue);
            }
        }

        public IReadOnlyList<T> Deleted => _deleted;
        public override IReadOnlyDictionary<Object, Object> Identities => _identities;
        public IReadOnlyList<T> Inserted => _inserted;
        public static PropertyInfo? SelfRefProperty { get; set; }
    }
}
