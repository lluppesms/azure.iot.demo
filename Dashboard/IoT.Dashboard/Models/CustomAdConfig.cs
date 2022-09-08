//namespace IoT.Dashboard.Models;

//public class CustomAdConfig : Microsoft.Extensions.Configuration.ConfigurationSection
//{    
//    public class AzureAd
//    {
//        private string _instance = string.Empty;
//        [ConfigurationProperty("instance")]
//        public string Instance
//        {
//            get { return _instance; }
//            set { _instance = value; }
//        }
//        private string _domain = string.Empty;
//        [ConfigurationProperty("domain")]
//        public string Domain
//        {
//            get { return _domain; }
//            set { _domain = value; }
//        }
//        private string _tenantId = string.Empty;
//        [ConfigurationProperty("tenantId")]
//        public string TenantId
//        {
//            get { return _tenantId; }
//            set { _tenantId = value; }
//        }
//        private string _callbackPath = string.Empty;
//        [ConfigurationProperty("callbackPath")]
//        public string CallbackPath
//        {
//            get { return _callbackPath; }
//            set { _callbackPath = value; }
//        }
//        private string _signedOutCallbackPath = string.Empty;
//        [ConfigurationProperty("signedOutCallbackPath")]
//        public string SignedOutCallbackPath
//        {
//            get { return _signedOutCallbackPath; }
//            set { _signedOutCallbackPath = value; }
//        }
//        private string _clientId = string.Empty;


//        [ConfigurationProperty("clientId")]
//        public string ClientId
//        {
//            get { return _clientId; }
//            set { _clientId = value; }
//        }
//    }
//    public CustomAdConfig(IConfigurationRoot root, string path) : base(root, path)
//    {
//        //AzureAd = new AzureAd();
//    }

//    //public CustomAdConfig(string instance, string domain, string tenantId, string callbackPath, string signedOutCallbackPath, string clientId)
//    //{
//    //    _instance = instance;
//    //    _domain = domain;
//    //    _tenantId = tenantId;
//    //    _callbackPath = callbackPath;
//    //    _signedOutCallbackPath = signedOutCallbackPath;
//    //    _clientId = clientId;
//    //}

//    //protected override void DeserializeSection(System.Xml.XmlReader reader)
//    //{
//    //    base.DeserializeSection(reader);
//    //    // You can add custom processing code here.
//    //}

//    //protected override string SerializeSection(ConfigurationElement parentElement, string name, ConfigurationSaveMode saveMode)
//    //{
//    //    var s = base.SerializeSection(parentElement, name, saveMode);
//    //    // You can add custom processing code here.
//    //    return s;
//    //}
//}
