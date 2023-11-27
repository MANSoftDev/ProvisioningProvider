using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using System.IO;
using System.Xml.Linq;

namespace CPSiteProvider
{
    public class ProvisioningProvider : SPWebProvisioningProvider
    {
        private const string SITE_TEMPLATE = "STS#1";

        public override void Provision(SPWebProvisioningProperties props)
        {
            // Create a blank site to begin from
            props.Web.ApplyWebTemplate(SITE_TEMPLATE);

            // Save this so it is available in other methods
            Properties = props;

            SPSecurity.CodeToRunElevated code = new SPSecurity.CodeToRunElevated(CreateSite);
            SPSecurity.RunWithElevatedPrivileges(code);
        }

        #region Private Methods

        /// <summary>
        /// Create the site
        /// </summary>
        private void CreateSite()
        {
            using(SPSite site = new SPSite(Properties.Web.Site.ID))
            {
                using(SPWeb web = site.OpenWeb(Properties.Web.ID))
                {
                    // Add specified features to this site
                    AddSiteFeatures(site);

                    // Add specified features to this web
                    AddWebFeatures(web);

                    // Add new default page
                    AddDefaultPage(web);
                }
            }
        }

        /// <summary>
        /// Add features to the given site
        /// </summary>
        /// <param name="site">SPSite to add features to</param>
        private void AddSiteFeatures(SPSite site)
        {
            List<XElement> features = (from f in DataFile.Elements("SiteFeatures")
                                .Elements("Feature")
                                 select f).ToList();

            foreach(XElement feature in features)
            {
                // Make sure the feature hasn't already been activated
                SPFeature f = site.Features[new Guid(feature.Attribute("ID").Value)];
                if(f == null)
                {
                    site.Features.Add(new Guid(feature.Attribute("ID").Value));
                }
            }
        }

        /// <summary>
        /// Add features to the given web
        /// </summary>
        /// <param name="web">SPWeb to add features to</param>
        private void AddWebFeatures(SPWeb web)
        {
            List<XElement> features = (from f in DataFile.Elements("WebFeatures")
                                .Elements("Feature")
                                select f).ToList();

            foreach(XElement feature in features)
            {
                // Make sure the feature hasn't already been activated
                SPFeature f = web.Features[new Guid(feature.Attribute("ID").Value)];
                if(f == null)
                {
                    web.Features.Add(new Guid(feature.Attribute("ID").Value));
                }
            }
        }

        /// <summary>
        /// Add default page to site
        /// </summary>
        /// <param name="web">SPWeb to add page to</param>
        private void AddDefaultPage(SPWeb web)
        {
            string file = (from f in DataFile.Elements("DefaultPage")
                            select f).Single().Attribute("file").Value;

            string filePath = FeaturePath + "\\" + file;
            TextReader reader = new StreamReader(filePath);

            MemoryStream outStream = new MemoryStream();
            StreamWriter writer = new StreamWriter(outStream);

            writer.Write(reader.ReadToEnd());
            writer.Flush();

            web.Files.Add("Default.aspx", outStream, true);
        }

        #endregion

        #region Properties

        private SPWebProvisioningProperties Properties { get; set; }

        private XElement DataFile
        {
            get
            {
                XElement featuresXml = null;
                if(Properties != null)
                {
                    // Construct the path from the SharePoint root folder to
                    // the file specified in the webtemp
                    string path = SPUtility.GetGenericSetupPath(Path.GetDirectoryName(Properties.Data));
                    path = Path.Combine(path, Path.GetFileName(Properties.Data));

                    // Load the xml file
                    featuresXml = XElement.Load(path);
                }

                return featuresXml;
            }
        }

        private string FeaturePath
        {
            get
            {
                string path = string.Empty;
                if(Properties != null)
                {
                    // Construct the path from the SharePoint root folder to
                    // the file specified in the webtemp
                    path = SPUtility.GetGenericSetupPath(Path.GetDirectoryName(Properties.Data));
                    path = Path.GetDirectoryName(path);
                }
                return path;
            }
        }

        #endregion
    }
}
