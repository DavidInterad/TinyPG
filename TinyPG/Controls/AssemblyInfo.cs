// Copyright 2008 - 2010 Herre Kuijpers - <herre.kuijpers@gmail.com>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------

using System.Reflection;

namespace TinyPG.Controls
{
    public class AssemblyInfo
    {
        private static string _companyName = string.Empty;

        /// Get the name of the system provider name from the assembly ///
        public static string CompanyName
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null)
                {
                    return _companyName;
                }

                var customAttributes = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (customAttributes.Length > 0)
                {
                    _companyName = ((AssemblyCompanyAttribute)customAttributes[0]).Company;
                }

                if (string.IsNullOrEmpty(_companyName))
                {
                    _companyName = string.Empty;
                }

                return _companyName;
            }
        }

        private static string _productVersion = string.Empty;

        /// Get System version from the assembly ///
        public static string ProductVersion
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null)
                {
                    return _productVersion;
                }

                var customAttributes = assembly.GetCustomAttributes(typeof(AssemblyVersionAttribute), false);
                if (customAttributes.Length > 0)
                {
                    _productVersion = ((AssemblyVersionAttribute)customAttributes[0]).Version;
                }

                if (string.IsNullOrEmpty(_productVersion))
                {
                    _productVersion = string.Empty;
                }

                return _productVersion;
            }
        }
        static string _productName = string.Empty;

        /// Get the name of the System from the assembly ///
        public static string ProductName
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null)
                {
                    return _productName;
                }

                var customAttributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (customAttributes.Length > 0)
                {
                    _productName = ((AssemblyProductAttribute)customAttributes[0]).Product;
                }

                if (string.IsNullOrEmpty(_productName))
                {
                    _productName = string.Empty;
                }

                return _productName;
            }
        }
        static string _copyRightsDetail = string.Empty;

        /// Get the copyRights details from the assembly ///
        public static string CopyRightsDetail
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null)
                {
                    return _copyRightsDetail;
                }

                var customAttributes = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (customAttributes.Length > 0)
                {
                    _copyRightsDetail = ((AssemblyCopyrightAttribute)customAttributes[0]).Copyright;
                }

                if (string.IsNullOrEmpty(_copyRightsDetail))
                {
                    _copyRightsDetail = string.Empty;
                }

                return _copyRightsDetail;
            }
        }

        static string _productTitle = string.Empty;

        /// Get the Product tile from the assembly ///
        public static string ProductTitle
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null)
                {
                    return _productTitle;
                }

                var customAttributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (customAttributes.Length > 0)
                {
                    _productTitle = ((AssemblyTitleAttribute)customAttributes[0]).Title;
                }

                if (string.IsNullOrEmpty(_productTitle))
                {
                    _productTitle = string.Empty;
                }

                return _productTitle;
            }
        }

        static string _productDescription = string.Empty;

        /// Get the description of the product from the assembly ///
        public static string ProductDescription
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null)
                {
                    return _productDescription;
                }

                var customAttributes = assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (customAttributes.Length > 0)
                {
                    _productDescription = ((AssemblyDescriptionAttribute)customAttributes[0]).Description;
                }

                if (string.IsNullOrEmpty(_productDescription))
                {
                    _productDescription = string.Empty;
                }

                return _productDescription;
            }
        }
    }
}
