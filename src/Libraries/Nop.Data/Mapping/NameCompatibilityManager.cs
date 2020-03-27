﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Nop.Core.ComponentModel;
using Nop.Core.Infrastructure;

namespace Nop.Data.Mapping
{
    /// <summary>
    /// Helper class for maintaining backward compatibility of table naming
    /// </summary>
    public static partial class NameCompatibilityManager
    {
        #region Fields

        private static readonly Dictionary<Type, string> _tableNames = new Dictionary<Type, string>();
        private static readonly Dictionary<(Type, string), string> _columnName =new Dictionary<(Type, string), string>();
        private static readonly IList<Type> _loadedFor=new List<Type>();
        private static bool _isInitialized = false;
        private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        public static int counter = 1;
        
        #endregion

        #region Ctor

        #endregion

        #region Methods

        static void Initialize()
        {
            //perform with locked access to resources
            using (new ReaderWriteLockDisposable(_locker))
            {
                if(_isInitialized)
                    return;

                var typeFinder = new AppDomainTypeFinder();

                var compatibilities = typeFinder.FindClassesOfType<INameCompatibility>()
                    ?.Select(type => EngineContext.Current.ResolveUnregistered(type) as INameCompatibility).ToList() ?? new List<INameCompatibility>();

                compatibilities.AddRange(AdditionalNameCompatibilities.Select(type => EngineContext.Current.ResolveUnregistered(type) as INameCompatibility));

                counter += 1;
                foreach (var nameCompatibility in compatibilities.Distinct())
                {
                    if (_loadedFor.Contains(nameCompatibility.GetType()))
                        continue;

                    _loadedFor.Add(nameCompatibility.GetType());

                    foreach (var (key, value) in nameCompatibility.TableNames.Where(tableName =>
                        !_tableNames.ContainsKey(tableName.Key)))
                        _tableNames.Add(key, value);

                    foreach (var (key, value) in nameCompatibility.ColumnName.Where(columnName =>
                        !_columnName.ContainsKey(columnName.Key)))
                        _columnName.Add(key, value);
                }

                _isInitialized = true;
            }
        }

        /// <summary>
        /// Gets table name for mapping with the type
        /// </summary>
        /// <param name="type">Type to get the table name</param>
        /// <returns>Table name</returns>
        public static string GetTableName(Type type)
        {
            if (!_isInitialized)
                Initialize();

            return _tableNames.ContainsKey(type) ? _tableNames[type] : type.Name;
        }

        /// <summary>
        /// Gets column name for mapping with the entity's property
        /// </summary>
        /// <param name="type">Type of entity</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>Column name</returns>
        public static string GetColumnName(Type type, string propertyName)
        {
            if (!_isInitialized)
                Initialize();

            return _columnName.ContainsKey((type, propertyName)) ? _columnName[(type, propertyName)] : propertyName;
        }

        #endregion

        public static List<Type> AdditionalNameCompatibilities => new List<Type>();
    }
}