using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WixWPFWizardBA.Utilities
{
    public static class BootstrapperApplicationData
    {
        public static XNamespace ManifestNamespace = "http://schemas.microsoft.com/wix/2010/BootstrapperApplicationData";

        public static XElement ApplicationData = GetApplicationData();

        private static XElement GetApplicationData()
        {
            var workingFolder = Path.GetDirectoryName(typeof(BootstrapperApplicationData).Assembly.Location);
            var bootstrapperDataFilePath = Path.Combine(workingFolder, "BootstrapperApplicationData.xml");

            using (var reader = new StreamReader(bootstrapperDataFilePath))
            {
                var xml = reader.ReadToEnd();
                var xDocument = XDocument.Parse(xml);
                return xDocument.Element(ManifestNamespace + "BootstrapperApplicationData");
            }
        }
    }
}
