using System;
using System.IO;
using Function_JsonDTOs;
using Function_XMLDTOs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Xml.Serialization;
using Microsoft.WindowsAzure.Storage.Blob;

namespace FunctionAppBlobTrigger
{
    public static class Function_ConvertJsonToXml
    {
            [FunctionName("Function_ConvertJsonToXml")]
            public static async System.Threading.Tasks.Task RunAsync([BlobTrigger("samples-json-files/{name}", Connection = "ConnectionStrings:AzureWebJobsStorage")] Stream myBlob,
                string name, ILogger log,
                [Blob("sample-xml-files/{name}", FileAccess.Write, Connection = "ConnectionStrings:AzureWebJobsStorage")] CloudBlobContainer applicationXmlOutput)
            {
                log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

                if (myBlob == null || myBlob.Length < 0)
                {
                    log.LogInformation($"Processed blob\n Name:{name} \n is null or empty");
                }

                JsonSerializer serializer = new JsonSerializer();
                UserJsonModel deserializedJson = null;

                using (StreamReader sr = new StreamReader(myBlob))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    while (!sr.EndOfStream)
                    {
                        deserializedJson = serializer.Deserialize<UserJsonModel>(reader);
                    }
                }

                log.LogInformation($"deserialized Json content: {deserializedJson}");

                string result = MapApplicationDataAndReturnXmlResult(deserializedJson);

                log.LogInformation($"xml content: {result}");

                var blobName = name.Replace("json", "xml");

                var cloudBlockBlob = applicationXmlOutput.GetBlockBlobReference(blobName);
                await cloudBlockBlob.UploadTextAsync(result);
                log.LogInformation($"successfull read and upload");
        }

        private static string MapApplicationDataAndReturnXmlResult(UserJsonModel jsonModel)
        {
            UserXmlModel xmlModel = new UserXmlModel
            {
                Id = jsonModel.Id,
                UserName = jsonModel.FirstName + ' ' + jsonModel.LastName,
                DateOfBirth = jsonModel.DateOfBirth
            };

            xmlModel.Id = jsonModel.Id;

            XmlSerializer serializer = new XmlSerializer(xmlModel.GetType());
            string result = string.Empty;

            using (MemoryStream memStm = new MemoryStream())
            {
                serializer.Serialize(memStm, xmlModel);

                memStm.Position = 0;
                result = new StreamReader(memStm).ReadToEnd();
            }

            return result;
        }
    }
}
