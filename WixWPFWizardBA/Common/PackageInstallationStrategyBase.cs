//  
// Copyright (c) Nick Guletskii and Arseniy Aseev. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the solution root for full license information.  
//
namespace WixWPFWizardBA.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
    using Utilities;

    public class PackageInstallationStrategyBase<TParam, TInstallationType> : IPackageInstallationStrategy
    {
        XNamespace ManifestNamespace = "http://schemas.microsoft.com/wix/2010/BootstrapperApplicationData";
        Dictionary<string, string> packages;

        private readonly TParam _param;

        public PackageInstallationStrategyBase(IList<Package<TParam, TInstallationType>> packageList,
            TParam param, IInstallationTypeProvider<TInstallationType> installationTypeProvider)
        {
            this._param = param;
            this.PackageList = packageList;
            this.InstallationTypeProvider = installationTypeProvider;

            this.packages = this.ApplicationData.Descendants(ManifestNamespace + "WixPackageProperties").
                Where(x => x.Attribute("DisplayName") != null).
                ToDictionary(x => (string)x.Attribute("Package"), y => (string)y.Attribute("DisplayName"));
        }

        public XElement ApplicationData
        {
            get
            {
                var workingFolder = Path.GetDirectoryName(this.GetType().Assembly.Location);
                var bootstrapperDataFilePath = Path.Combine(workingFolder, "BootstrapperApplicationData.xml");

                using (var reader = new StreamReader(bootstrapperDataFilePath))
                {
                    var xml = reader.ReadToEnd();
                    var xDocument = XDocument.Parse(xml);
                    return xDocument.Element(ManifestNamespace + "BootstrapperApplicationData");
                }
            }
        }

        public IList<Package<TParam, TInstallationType>> PackageList { get; }
        public IInstallationTypeProvider<TInstallationType> InstallationTypeProvider { get; }

        public virtual FeatureState? PlanMsiFeature(LaunchAction launchAction, string packageId, string featureId)
        {
            // Let Burn decide what to do with packages we don't know about
            if (this.PackageList.All(x => x.PackageId != packageId))
                return null;

            // Unless stated otherwise, install the feature.
            return FeatureState.Local;
        }

        public virtual RequestState? PlanPackage(LaunchAction launchAction, string packageId)
        {
            var installationType = this.InstallationTypeProvider.InstallationType;

            var architecture = SystemInformationUtilities.Is64BitSystem() ? Architecture.X64 : Architecture.X86;
            var packageConfig = this.PackageList.FirstOrDefault(x => x.PackageId == packageId);

            // Let Burn decide what to do with packages we don't know about
            if (packageConfig == null)
                return null;

            switch (launchAction)
            {
                case LaunchAction.Layout:
                    if (packageConfig == null
                        || packageConfig.AcquireDuringLayout)
                    {
                        return RequestState.Present;
                    }
                    else
                    {
                        return RequestState.None;
                    }
                case LaunchAction.Uninstall:
                    if (packageConfig == null
                        || packageConfig.IsRemovable)
                        return RequestState.Absent;
                    return RequestState.None;
                case LaunchAction.Cache:
                case LaunchAction.Install:
                case LaunchAction.Modify:
                    if (packageConfig == null
                        ||
                        packageConfig.InstallationTypes.Contains(installationType)
                        && packageConfig.AdditionalPredicate(this._param)
                        && (packageConfig.Architectures & architecture) == architecture)
                        return RequestState.Present;
                    else
                        return RequestState.Absent;
                case LaunchAction.Repair:
                    if (packageConfig == null || packageConfig.IsRepairable)
                    {
                        return RequestState.Repair;
                    }
                    return RequestState.None;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public virtual void DetectAdditionalInformation()
        {
        }

        public virtual string GetPackageNameFromId(string packageId)
        {
            var packageConfig = this.PackageList.FirstOrDefault(x => x.PackageId == packageId);
            if (packageConfig != null)
                return packageConfig.DisplayName;

            string name;
            if (this.packages.TryGetValue(packageId, out name))
                return name;

            return packageId;
        }
    }
}