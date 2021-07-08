using System;
using System.Collections;
using System.Xml;
using Verse;

namespace RimFridge
{
    class PatchSaveSettings : PatchOperationPathed
    {
        protected string key;
        private XmlContainer value;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            bool found = false;
            foreach (var m in ModsConfig.ActiveModsInLoadOrder)
            {
                if (m.PackageId == "savestoragesettings.kv.rw")
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return true;
            }

            XmlNode valNode = value.node;
            bool result = false;
            IEnumerator enumerator = xml.SelectNodes(xpath).GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    object obj = enumerator.Current;
                    result = true;
                    XmlNode parentNode = obj as XmlNode;
                    XmlNode xmlNode = parentNode.SelectSingleNode(key);
                    if (xmlNode == null)
                    {
                        // Add - Add node if not existing
                        xmlNode = parentNode.OwnerDocument.CreateElement(key);
                        parentNode.AppendChild(xmlNode);
                    }
                    else
                    {
                        // Replace - Clear existing children
                        xmlNode.RemoveAll();
                    }
                    // (Re)add value
                    xmlNode.AppendChild(parentNode.OwnerDocument.ImportNode(valNode.FirstChild, true));
                }
            }
            finally
            {
                IDisposable disposable = enumerator as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            return result;
        }
    }
}
