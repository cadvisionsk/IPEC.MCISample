using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using MFaaP.MFWSClient;
using System.Windows.Forms;

namespace MFilesRestAPI {

    public class Program {
        [STAThread]
        static void Main(string[] args) {
            Sample sample = new Sample();
        }
    }
    public class Sample {
        // IP address to the M-Files server, without the port
        private string m_IPAddress = "";
        // M-Files user's username
        private string m_username = "";
        // M-Files user's password
        private string m_password = "";

        private MFWSClient m_client = null;

        public Sample() {
            if (m_IPAddress == "" || m_username == "" || m_password == "") {
                Debug.Log("Write the M-Files server IP address and press ENTER..");
                m_IPAddress = Console.ReadLine();
                Console.Clear();
                Debug.Log("Write the Username and press ENTER..");
                m_username = Console.ReadLine();
                Console.Clear();
                Debug.Log("Write the Password and press ENTER..");
                m_password = Console.ReadLine();
                Console.Clear();
            }


            LoginToAVault();
            //CreateAnObject();
            Debug.Log("Press ENTER to start selecting a document to upload..");
            Console.ReadLine();
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK) {
                Debug.Log($"Selected file: {dialog.FileName}");
                Debug.Log("Press ENTER to start creating a document object on the vault..");
                Console.ReadLine();
                Debug.Log("Write the object name property..");
                string objectName = Console.ReadLine();
                Debug.Log("Press ENTER to start uploading the document to the vault..");
                Console.ReadLine();
                List<PropertyValue> propertyValues = new List<PropertyValue>();
                propertyValues.Add(NewPropertyValue(100, 
                                                    MFDataType.Lookup,
                                                    new Lookup() {
                                                    Item = 1,
                                                    Version = -1
                                                }));
                propertyValues.Add(NewPropertyValue(0,
                                                    MFDataType.Text,
                                                    objectName)); 

                ObjectCreationInfo objectCreationInfo = new ObjectCreationInfo();
                objectCreationInfo.PropertyValues = propertyValues.ToArray();
                ObjectVersion objectVersion = m_client.ObjectOperations.CreateNewObject(0, objectCreationInfo);

                objectVersion = m_client.ObjectOperations.CheckOut(objectVersion.ObjVer);
                List<FileInfo> fileInfos = new List<FileInfo>();
                fileInfos.Add(new FileInfo(dialog.FileName));
                ExtendedObjectVersion extendedObjectVersion = m_client.ObjectFileOperations.AddFiles(objectVersion.ObjVer, fileInfos.ToArray());
                objectVersion = m_client.ObjectOperations.CheckIn(extendedObjectVersion.ObjVer);
                Debug.Log("Object successfully uploaded!");

            }
            
            Debug.Log("Press ENTER to shut the console application down..");
            Console.ReadLine();
        }

        private void LoginToAVault() {
            bool authenticationSuccessful = true;
            List<Vault> onlineVaults = null;
            try {
                // Connect to the target M-Files server
                m_client = new MFWSClient("http://" + m_IPAddress);
                // Authenticate using any M-Files account (an account with sufficient read/write permissions is recommended)
                // Authentication returns a token, which is later used in every RestAPI call
                m_client.AuthenticateUsingCredentials(null, m_username, m_password);
                // Authentication always returns a token (also if the credentials do not match or exist), the only way to tell if authentication was successful, is by checking whether the next RestAPI call returns an error or not
                // This is where we get the list of vaults to connect to and at the same time check whether the authentication was successful
                onlineVaults = m_client.GetOnlineVaults();
            }
            catch (Exception pException) {
                // Authentication has most likely failed and that is why we end up in this catch statement
                authenticationSuccessful = false;
                Debug.Log(pException.ToString());
            }
            // If the authentication was successful, we can continue (code design is of course up to the programming team, this is just a simple sample)
            if (authenticationSuccessful) {
                // Now we can choose from online vaults to connect to
                int i = 1;
                Debug.Log("Online vaults: ");
                Debug.Line();
                foreach (Vault vault in onlineVaults) {
                    Debug.Log($"{i}. {vault.Name} - {vault.GUID}");
                    i++;
                }

                i = int.Parse(Console.ReadLine());

                // Connecting to a vault is done by using the vault's GUID in the AuthenticateUsingCredentials call
                // Previously we used the AuthenticateUsingCredentials call to get the online vaults, now we use it again with the target vault's guid
                // The vault's GUID does not change, so it is possible to "hard-core" it's GUID into the code
                // I will go the "hard-code" way
                // Once the vault's guid is known, it is possible to skip the first authentication step without a vault's guid
                m_client.AuthenticateUsingCredentials(new Guid(onlineVaults[i-1].GUID), m_username, m_password);
                Debug.Line();
                Debug.Log($"You are now logged to {onlineVaults[i-1].Name} - {onlineVaults[i-1].GUID}");
                Debug.Line();
            }
        }

        private void CreateAnObject() {
            // First lets prepare the properties that will be attached to the object
            List<PropertyValue> propertyValues = new List<PropertyValue>();
            // Create a property "Class". 
            propertyValues.Add(NewPropertyValue(100, // Property ID is "100"
                                                MFDataType.Lookup, // Property type is "Lookup"
                                                new Lookup() { // Therefore the value is a Lookup
                                                    Item = 1, // Item is the ID of the target class
                                                    Version = -1 // Version -1 is the most up to date version of the Item
                                                }));
            // Create a property "Name". 
            propertyValues.Add(NewPropertyValue(0, // Property ID is "0"
                                                MFDataType.Text, // Property type is "Text"
                                                "NameOfTheNewObject")); //Therefore the value is a string
            // Create a property "Created by"
            propertyValues.Add(NewPropertyValue(25, // Property ID is "25"
                                                MFDataType.Lookup, // Property type is "Lookup"
                                                new Lookup() { // Therefore the value is a Lookup
                                                    Item = 36, // Item is the ID of the target user
                                                    Version = -1 // Version -1 is the most up to date version of the Item
                                                }));
            // Now lets attach the properties to the new object using ObjectCreationInfo
            ObjectCreationInfo objectCreationInfo = new ObjectCreationInfo();
            objectCreationInfo.PropertyValues = propertyValues.ToArray();
            // And finally create the object
            // ObjectVersion is the response from the target vault containing info about the new object
            ObjectVersion objectVersion = m_client.ObjectOperations.CreateNewObject(0, objectCreationInfo);
            // Lets now use the ObjectVersion to checkout the object, attach a file to it and check it back in
            // Checkining out
            objectVersion = m_client.ObjectOperations.CheckOut(objectVersion.ObjVer);
            // Preparing files to upload
            List<FileInfo> fileInfos = new List<FileInfo>();
            fileInfos.Add(new FileInfo("C:\\Path\\To\\The\\File.extension"));
            // Attaching and uploading an array of files
            ExtendedObjectVersion extendedObjectVersion = m_client.ObjectFileOperations.AddFiles(objectVersion.ObjVer, fileInfos.ToArray());
            Debug.Log("Added files.Count = " + extendedObjectVersion.AddedFiles.Count);
            Debug.Line();
            // Finally check the object back in
            objectVersion = m_client.ObjectOperations.CheckIn(extendedObjectVersion.ObjVer);
        }

        private PropertyValue NewPropertyValue(int pPropertyDef, MFDataType pDataType, object pValue) {
            PropertyValue propertyValue = new PropertyValue();
            // PropertyDef is the ID of a property, each property has a unique and vault specific ID
            propertyValue.PropertyDef = pPropertyDef;
            // Create a TypedValue
            TypedValue typedValue = new TypedValue();
            typedValue.DataType = pDataType;
            switch (pDataType) {
                case MFDataType.Lookup:
                    if (!(pValue is Lookup)) { throw new Exception("pValue must be a Lookup"); } 
                    else { typedValue.Lookup = (Lookup)pValue; }
                    break;
                case MFDataType.MultiSelectLookup:
                    if (!(pValue is List<Lookup>)) { throw new Exception("pValue must be a list of Lookups"); }
                    else { typedValue.Lookups = (List<Lookup>)pValue; }
                    break;
                default:
                    typedValue.Value = pValue;
                    break;
            }
            typedValue.Value = pValue;
            // Assign the TypedValue
            propertyValue.TypedValue = typedValue;
            return propertyValue;
        }
    }

    public static class Debug {
        public static void Log(string pString) {
            Console.WriteLine(pString);
        }
        public static void Line() {
            Console.WriteLine("---------------------------------------------------------");
        }
    }
}
