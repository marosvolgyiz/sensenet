﻿using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SenseNet.Core.Tests.Implementations
{
    internal class TestPackageStorageProviderFactory : IPackageStorageProviderFactory
    {
        // An idea: to parallelize test execution, need to store the provider instance in the thread context.
        private IPackageStorageProvider _provider;
        public TestPackageStorageProviderFactory(IPackageStorageProvider provider)
        {
            _provider = provider;
        }
        public IPackageStorageProvider CreateProvider()
        {
            return _provider;
        }
    }

    public class TestPackageStorageProvider : IPackageStorageProvider
    {
        private int _id;
        private List<Package> __storage = new List<Package>();

        private List<Package> Storage
        {
            get
            {
                if (!DatabaseEnabled)
                    throw new Exception("Database is not available");
                return __storage;
            }
        }

        public bool DatabaseEnabled { get; set; }

        private Package ClonePackage(Package source)
        {
            var target = new Package();
            UpdatePackage(source, target);
            return target;
        }
        private void UpdatePackage(Package source, Package target)
        {
            target.Id = source.Id;
            target.Description = source.Description;
            target.ComponentId = source.ComponentId;
            target.PackageType = source.PackageType;
            target.ReleaseDate = source.ReleaseDate;
            target.ExecutionDate = source.ExecutionDate;
            target.ExecutionResult = source.ExecutionResult;
            target.ComponentVersion = source.ComponentVersion;
            target.ExecutionError = source.ExecutionError;
        }

        public IDataProcedureFactory DataProcedureFactory
        {
            get { throw new NotSupportedException(); }
            set { /* do nothing */ }
        }

        /* ================================================================================================= IPackageStorageProvider */

        public IEnumerable<ComponentInfo> LoadInstalledComponents()
        {
            var nullVersion = new Version(0, 0);
            var componentInfos = new Dictionary<string, ComponentInfo>();
            foreach (var package in Storage
                .Where(p => p.PackageType == PackageType.Install
                    && p.ExecutionResult == ExecutionResult.Successful))
            {
                var componentId = package.ComponentId;
                ComponentInfo component = null;
                if (!componentInfos.TryGetValue(componentId, out component))
                {
                    component = new ComponentInfo
                    {
                        ComponentId = package.ComponentId,
                        Version = package.ComponentVersion,
                        AcceptableVersion = package.ComponentVersion,
                        Description = package.Description
                    };
                    componentInfos.Add(componentId, component);
                }

                if (package.ComponentVersion > (component.AcceptableVersion ?? nullVersion))
                    component.AcceptableVersion = package.ComponentVersion;
            }

            foreach (var package in Storage
                .Where(p => (p.PackageType == PackageType.Install || p.PackageType == PackageType.Patch)))
            {
                var componentId = package.ComponentId;
                ComponentInfo component = null;
                if (componentInfos.TryGetValue(componentId, out component))
                {
                    if ((package.ComponentVersion > (component.AcceptableVersion ?? nullVersion))
                        && package.ExecutionResult == ExecutionResult.Successful)
                        component.AcceptableVersion = package.ComponentVersion;
                    if (package.ComponentVersion > (component.Version ?? nullVersion))
                        component.Version = package.ComponentVersion;
                }
            }
            return componentInfos.Values.ToArray();
        }

        public IEnumerable<Package> LoadInstalledPackages()
        {
            return Storage
                //.Where(p => p.ExecutionResult != ExecutionResult.Unfinished)
                .Select(ClonePackage)
                .ToArray();
        }

        public void SavePackage(Package package)
        {
            if (package.Id > 0)
                throw new InvalidOperationException("Only new package can be saved.");

            package.Id = ++_id;
            Storage.Add(ClonePackage(package));

            RepositoryVersionInfo.Reset();
        }

        public void UpdatePackage(Package package)
        {
            var existing = Storage.FirstOrDefault(p => p.Id == package.Id);
            if (existing == null)
                throw new InvalidOperationException("Package does not exist. Id: " + package.Id);
            UpdatePackage(package, existing);
        }

        public bool IsPackageExist(string componentId, PackageType packageType, Version version)
        {
            throw new NotImplementedException();
        }

        public void DeletePackage(Package package)
        {
            throw new NotImplementedException();
        }

        public void DeletePackagesExceptFirst()
        {
            if (Storage.Count == 0)
                return;
            throw new NotImplementedException();
        }

        // ================================================================================================= Test tools

        public int GetRecordCount()
        {
            return Storage.Count;
        }
    }
}