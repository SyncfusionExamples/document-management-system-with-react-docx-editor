using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Syncfusion.EJ2.DocumentEditor;
using WDocument = Syncfusion.DocIO.DLS.WordDocument;
using WFormatType = Syncfusion.DocIO.FormatType;
using Syncfusion.EJ2.SpellChecker;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace EJ2DocumentEditorServer.Controllers
{
    [Route("api/[controller]")]
    public class DocumentEditorController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        string path;
        //AWS S3 configuration read from appsettings.json.
        private readonly IConfiguration _configuration;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _bucketName;

        public DocumentEditorController(IHostingEnvironment hostingEnvironment, IConfiguration configuration)
        {
            _hostingEnvironment = hostingEnvironment;
            _configuration = configuration;
            _accessKey = configuration.GetValue<string>("AccessKey");
            _secretKey = configuration.GetValue<string>("SecretKey");
            _bucketName = configuration.GetValue<string>("BucketName");
            path = Startup.path;
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("Import")]
        public string Import(IFormCollection data)
        {
            if (data.Files.Count == 0)
                return null;
            Stream stream = new MemoryStream();
            IFormFile file = data.Files[0];
            int index = file.FileName.LastIndexOf('.');
            string type = index > -1 && index < file.FileName.Length - 1 ?
                file.FileName.Substring(index) : ".docx";
            file.CopyTo(stream);
            stream.Position = 0;

            //Hooks MetafileImageParsed event.
            WordDocument.MetafileImageParsed += OnMetafileImageParsed;
            WordDocument document = WordDocument.Load(stream, GetFormatType(type.ToLower()));
            //Unhooks MetafileImageParsed event.
            WordDocument.MetafileImageParsed -= OnMetafileImageParsed;

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(document);
            document.Dispose();
            return json;
        }

        //Converts Metafile to raster image.
        private static void OnMetafileImageParsed(object sender, MetafileImageParsedEventArgs args)
        {
            //You can write your own method definition for converting metafile to raster image using any third-party image converter.
            args.ImageStream = ConvertMetafileToRasterImage(args.MetafileStream);
        }

        private static Stream ConvertMetafileToRasterImage(Stream ImageStream)
        {
            //Here we are loading a default raster image as fallback.
            Stream imgStream = GetManifestResourceStream("ImageNotFound.jpg");
            return imgStream;
            //To do : Write your own logic for converting metafile to raster image using any third-party image converter(Syncfusion doesn't provide any image converter).
        }

        private static Stream GetManifestResourceStream(string fileName)
        {
            System.Reflection.Assembly execAssembly = typeof(WDocument).Assembly;
            string[] resourceNames = execAssembly.GetManifestResourceNames();
            foreach (string resourceName in resourceNames)
            {
                if (resourceName.EndsWith("." + fileName))
                {
                    fileName = resourceName;
                    break;
                }
            }
            return execAssembly.GetManifestResourceStream(fileName);
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("SpellCheck")]
        public string SpellCheck([FromBody] SpellCheckJsonData spellChecker)
        {
            try
            {
                SpellChecker spellCheck = new SpellChecker();
                spellCheck.GetSuggestions(spellChecker.LanguageID, spellChecker.TexttoCheck, spellChecker.CheckSpelling, spellChecker.CheckSuggestion, spellChecker.AddWord);
                return Newtonsoft.Json.JsonConvert.SerializeObject(spellCheck);
            }
            catch
            {
                return "{\"SpellCollection\":[],\"HasSpellingError\":false,\"Suggestions\":null}";
            }
        }
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("SpellCheckByPage")]
        public string SpellCheckByPage([FromBody] SpellCheckJsonData spellChecker)
        {
            try
            {
                SpellChecker spellCheck = new SpellChecker();
                spellCheck.CheckSpelling(spellChecker.LanguageID, spellChecker.TexttoCheck);
                return Newtonsoft.Json.JsonConvert.SerializeObject(spellCheck);
            }
            catch
            {
                return "{\"SpellCollection\":[],\"HasSpellingError\":false,\"Suggestions\":null}";
            }
        }

        public class SpellCheckJsonData
        {
            public int LanguageID { get; set; }
            public string TexttoCheck { get; set; }
            public bool CheckSpelling { get; set; }
            public bool CheckSuggestion { get; set; }
            public bool AddWord { get; set; }

        }

        public class CustomParameter
        {
            public string content { get; set; }
            public string type { get; set; }
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("SystemClipboard")]
        public string SystemClipboard([FromBody] CustomParameter param)
        {
            if (param.content != null && param.content != "")
            {
                try
                {
                    //Hooks MetafileImageParsed event.
                    WordDocument.MetafileImageParsed += OnMetafileImageParsed;
                    WordDocument document = WordDocument.LoadString(param.content, GetFormatType(param.type.ToLower()));
                    //Unhooks MetafileImageParsed event.
                    WordDocument.MetafileImageParsed -= OnMetafileImageParsed;
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(document);
                    document.Dispose();
                    return json;
                }
                catch (Exception)
                {
                    return "";
                }
            }
            return "";
        }

        public class CustomRestrictParameter
        {
            public string passwordBase64 { get; set; }
            public string saltBase64 { get; set; }
            public int spinCount { get; set; }
        }
        public class UploadDocument
        {
            public string DocumentName { get; set; }
        }
        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("RestrictEditing")]
        public string[] RestrictEditing([FromBody] CustomRestrictParameter param)
        {
            if (param.passwordBase64 == "" && param.passwordBase64 == null)
                return null;
            return WordDocument.ComputeHash(param.passwordBase64, param.saltBase64, param.spinCount);
        }


        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("LoadDefault")]
        public string LoadDefault()
        {
            Stream stream = System.IO.File.OpenRead("App_Data/GettingStarted.docx");
            stream.Position = 0;

            WordDocument document = WordDocument.Load(stream, FormatType.Docx);
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(document);
            document.Dispose();
            return json;
        }
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("LoadDocument")]
        public string LoadDocument([FromForm] UploadDocument uploadDocument)
        {
            string documentPath = Path.Combine(path, uploadDocument.DocumentName);
            Stream stream = null;
            if (System.IO.File.Exists(documentPath))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(documentPath);
                stream = new MemoryStream(bytes);
            }
            else
            {
                bool result = Uri.TryCreate(uploadDocument.DocumentName, UriKind.Absolute, out Uri uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (result)
                {
                    stream = GetDocumentFromURL(uploadDocument.DocumentName).Result;
                    if (stream != null)
                        stream.Position = 0;
                }
            }
            WordDocument document = WordDocument.Load(stream, FormatType.Docx);
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(document);
            document.Dispose();
            return json;
        }
        async Task<MemoryStream> GetDocumentFromURL(string url)
        {
            var client = new HttpClient(); ;
            var response = await client.GetAsync(url);
            var rawStream = await response.Content.ReadAsStreamAsync();
            if (response.IsSuccessStatusCode)
            {
                MemoryStream docStream = new MemoryStream();
                rawStream.CopyTo(docStream);
                return docStream;
            }
            else { return null; }
        }
        internal static FormatType GetFormatType(string format)
        {
            if (string.IsNullOrEmpty(format))
                throw new NotSupportedException("EJ2 DocumentEditor does not support this file format.");
            switch (format.ToLower())
            {
                case ".dotx":
                case ".docx":
                case ".docm":
                case ".dotm":
                    return FormatType.Docx;
                case ".dot":
                case ".doc":
                    return FormatType.Doc;
                case ".rtf":
                    return FormatType.Rtf;
                case ".txt":
                    return FormatType.Txt;
                case ".xml":
                    return FormatType.WordML;
                case ".html":
                    return FormatType.Html;
                default:
                    throw new NotSupportedException("EJ2 DocumentEditor does not support this file format.");
            }
        }
        internal static WFormatType GetWFormatType(string format)
        {
            if (string.IsNullOrEmpty(format))
                throw new NotSupportedException("EJ2 DocumentEditor does not support this file format.");
            switch (format.ToLower())
            {
                case ".dotx":
                    return WFormatType.Dotx;
                case ".docx":
                    return WFormatType.Docx;
                case ".docm":
                    return WFormatType.Docm;
                case ".dotm":
                    return WFormatType.Dotm;
                case ".dot":
                    return WFormatType.Dot;
                case ".doc":
                    return WFormatType.Doc;
                case ".rtf":
                    return WFormatType.Rtf;
                case ".txt":
                    return WFormatType.Txt;
                case ".xml":
                    return WFormatType.WordML;
                case ".odt":
                    return WFormatType.Odt;
                default:
                    throw new NotSupportedException("EJ2 DocumentEditor does not support this file format.");
            }
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("Export")]
        public FileStreamResult Export(IFormCollection data)
        {
            if (data.Files.Count == 0)
                return null;
            string fileName = this.GetValue(data, "filename");
            string name = fileName;
            int index = name.LastIndexOf('.');
            string format = index > -1 && index < name.Length - 1 ?
                name.Substring(index) : ".doc";
            if (string.IsNullOrEmpty(name))
            {
                name = "Document1";
            }
            Stream stream = new MemoryStream();
            string contentType = "";
            WDocument document = this.GetDocument(data);
            if (format == ".pdf")
            {
                contentType = "application/pdf";
            }
            else
            {
                WFormatType type = GetWFormatType(format);
                switch (type)
                {
                    case WFormatType.Rtf:
                        contentType = "application/rtf";
                        break;
                    case WFormatType.WordML:
                        contentType = "application/xml";
                        break;
                    case WFormatType.Dotx:
                        contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.template";
                        break;
                    case WFormatType.Doc:
                        contentType = "application/msword";
                        break;
                    case WFormatType.Dot:
                        contentType = "application/msword";
                        break;
                }
                document.Save(stream, type);
            }
            document.Close();
            stream.Position = 0;
            return new FileStreamResult(stream, contentType)
            {
                FileDownloadName = fileName
            };
        }
        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("CompareDocuments")]
        public string CompareDocuments(IFormCollection data)
        {
            if (data.Files.Count == 0 || data.Files.Count < 2)
                return null;

            IFormFile originalFile = data.Files[0];
            IFormFile revisedFile = data.Files[1];

            WDocument originalDocument = GetWordDocument(originalFile);
            WDocument revisedDocument = GetWordDocument(revisedFile);
            originalDocument.Compare(revisedDocument);
            //Hooks MetafileImageParsed event.
            WordDocument.MetafileImageParsed += OnMetafileImageParsed;
            WordDocument document = WordDocument.Load(originalDocument);
            //Unhooks MetafileImageParsed event.
            WordDocument.MetafileImageParsed -= OnMetafileImageParsed;
            originalDocument.Close();
            revisedDocument.Close();

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(document);
            document.Dispose();
            return json;
        }

        private static WDocument GetWordDocument(IFormFile file)
        {
            Stream stream = new MemoryStream();
            int index = file.FileName.LastIndexOf('.');
            string type = index > -1 && index < file.FileName.Length - 1 ?
                file.FileName.Substring(index) : ".docx";
            file.CopyTo(stream);
            stream.Position = 0;

            WDocument document;
            if (type == ".sfdt")
            {
                using (var reader = new StreamReader(stream))
                {
                    string sfdtContent = reader.ReadToEnd();
                    document = WordDocument.Save(sfdtContent);
                    var outStream = new MemoryStream();
                    document.Save(outStream, WFormatType.Docx);
                    document.Close();
                    WDocument wordDocument = new WDocument(outStream, WFormatType.Docx);
                    return wordDocument;
                }
            }
            else
            {
                document = new WDocument(stream, GetWFormatType(type));
                stream.Dispose();
                return document;
            }
        }
        private string GetValue(IFormCollection data, string key)
        {
            if (data.ContainsKey(key))
            {
                string[] values = data[key];
                if (values.Length > 0)
                {
                    return values[0];
                }
            }
            return "";
        }
        private WDocument GetDocument(IFormCollection data)
        {
            Stream stream = new MemoryStream();
            IFormFile file = data.Files[0];
            file.CopyTo(stream);
            stream.Position = 0;

            WDocument document = new WDocument(stream, WFormatType.Docx);
            stream.Dispose();
            return document;
        }
        #region AWS S3 document storage APIs
        //Document data classes reused by the version-history endpoints.
        public class SubChild
        {
            public string id { get; set; }
            public string name { get; set; }
            public string user { get; set; }
        }
        public class RootObject
        {
            public string id { get; set; }
            public string name { get; set; }
            public string user { get; set; }
            public bool expanded { get; set; }
            public List<SubChild> subChild { get; set; }
        }
        public class S3Version
        {
            public S3Version(string version, string fullName, string modifiedUser, DateTime? dateTime = null)
            {
                DocumentVersion = version;
                FullName = fullName;
                ModifiedUser = modifiedUser;
                LastSavedTime = dateTime ?? DateTime.Now;
            }
            public string DocumentVersion { get; set; }
            public string FullName { get; set; }
            public string ModifiedUser { get; set; }
            public DateTime LastSavedTime { get; set; }
        }
        //Compare helper that mirrors the main project: opens two docx files from FullName and runs DocIO Compare on them.
        private WordDocument Compare(S3Version o, S3Version n)
        {
            using (FileStream originalDocumentStreamPath = new FileStream(o.FullName, FileMode.Open, FileAccess.Read))
            using (WDocument originalDocument = new WDocument(originalDocumentStreamPath, WFormatType.Docx))
            {
                using (FileStream revisedDocumentStreamPath = new FileStream(n.FullName, FileMode.Open, FileAccess.Read))
                using (WDocument revisedDocument = new WDocument(revisedDocumentStreamPath, WFormatType.Docx))
                {
                    originalDocument.Compare(revisedDocument);
                    return WordDocument.Load(originalDocument);
                }
            }
        }
        public class S3CompareData
        {
            public string Document { get; set; }
            public List<RootObject> Data { get; set; }
        }
        public class S3DocumentDetails
        {
            public string FileName { get; set; }
            public string ModifiedUser { get; set; }
            public int VersionCount { get; set; }
            public DateTime? LastModifiedTime { get; set; }
        }
        //Returns an S3 client configured with the credentials and region from appsettings.json.
        private AmazonS3Client GetS3Client()
        {
            string region = _configuration.GetValue<string>("BucketRegion");
            RegionEndpoint bucketRegion = string.IsNullOrEmpty(region) ? RegionEndpoint.USEast1 : RegionEndpoint.GetBySystemName(region);
            return new AmazonS3Client(_accessKey, _secretKey, bucketRegion);
        }
        //The bucket-root JSON map key that mirrors the local App_Data/fileNameWithUserName.json layout.
        private const string S3UserMapKey = "fileNameWithUserName.json";
        //Reads the version-key -> modified user dictionary from the bucket root.
        private Dictionary<string, string> GetS3UserMap(AmazonS3Client s3Client)
        {
            try
            {
                GetObjectRequest getRequest = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = S3UserMapKey
                };
                using (GetObjectResponse getResponse = s3Client.GetObjectAsync(getRequest).GetAwaiter().GetResult())
                using (StreamReader reader = new StreamReader(getResponse.ResponseStream))
                {
                    string existingJson = reader.ReadToEnd();
                    Dictionary<string, string> userMap = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(existingJson);
                    return userMap ?? new Dictionary<string, string>();
                }
            }
            catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }
        //Writes the updated version-key -> modified user dictionary back to the bucket root JSON object.
        private void SaveS3UserMap(AmazonS3Client s3Client, Dictionary<string, string> userMap)
        {
            string updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(userMap, Newtonsoft.Json.Formatting.Indented);
            byte[] bytes = Encoding.UTF8.GetBytes(updatedJson);
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = S3UserMapKey,
                    InputStream = stream,
                    ContentType = "application/json"
                };
                s3Client.PutObjectAsync(putRequest).GetAwaiter().GetResult();
            }
        }
        //Lists all .docx version objects under the given key prefix in the bucket, handling paged results.
        //Returns an empty list if the bucket/prefix does not exist or access is denied so the
//save flow can still create a brand-new document without throwing.
        private List<S3Object> GetS3VersionObjects(AmazonS3Client s3Client, string prefix)
        {
            List<S3Object> versionObjects = new List<S3Object>();
            if (string.IsNullOrEmpty(_bucketName))
            {
                return versionObjects;
            }
            ListObjectsV2Request listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = prefix
            };
            ListObjectsV2Response listResponse;
            try
            {
                do
                {
                    listResponse = s3Client.ListObjectsV2Async(listRequest).GetAwaiter().GetResult();
                    if (listResponse != null && listResponse.S3Objects != null)
                    {
                        foreach (S3Object s3Object in listResponse.S3Objects)
                        {
                            if (s3Object != null && !string.IsNullOrEmpty(s3Object.Key) &&
                                s3Object.Key.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                            {
                                versionObjects.Add(s3Object);
                            }
                        }
                    }
                    listRequest.ContinuationToken = listResponse != null ? listResponse.NextContinuationToken : null;
                } while (listResponse != null && listResponse.IsTruncated == true);
            }
            catch (Amazon.S3.AmazonS3Exception ex) when (
                ex.StatusCode == System.Net.HttpStatusCode.NotFound ||
                ex.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                ex.ErrorCode == "NoSuchBucket" ||
                ex.ErrorCode == "AccessDenied")
            {
                return new List<S3Object>();
            }
            return versionObjects;
        }
        //Saves the given document stream as the next version inside the document's S3 "folder" and
        //updates the bucket-root user map (mirrors the local AutoSave behavior).
        private void SaveVersionToS3(string documentName, Stream stream, string modifiedUser)
        {
            if (string.IsNullOrEmpty(documentName))
                return;
            if (!documentName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                documentName = documentName + ".docx";
            }
            string prefix = documentName.TrimEnd('/') + "/";
            using (var s3Client = GetS3Client())
            {
                int versionCount = GetS3VersionObjects(s3Client, prefix).Count;
                string versionKey = prefix + string.Format("v{0}.docx", (versionCount + 1));
                stream.Position = 0;
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = versionKey,
                    InputStream = stream
                };
                s3Client.PutObjectAsync(putRequest).GetAwaiter().GetResult();
                //Persist the latest user attribution in the bucket-root JSON map (same idea as the local file).
                Dictionary<string, string> userMap = GetS3UserMap(s3Client);
                userMap[versionKey] = modifiedUser ?? string.Empty;
                SaveS3UserMap(s3Client, userMap);
            }
        }
        //Downloads the given S3 object into a fresh MemoryStream; caller disposes the result.
        private MemoryStream DownloadS3Object(AmazonS3Client s3Client, string key)
        {
            GetObjectRequest getRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };
            using (GetObjectResponse getResponse = s3Client.GetObjectAsync(getRequest).GetAwaiter().GetResult())
            {
                MemoryStream stream = new MemoryStream();
                getResponse.ResponseStream.CopyTo(stream);
                stream.Position = 0;
                return stream;
            }
        }
        //Compares two Word documents and returns the diff as an SFdt string for the React version history dialog.
        private string CompareDocumentsToSfdts(string firstFile, string secondFile)
        {
            using (FileStream originalStream = new FileStream(firstFile, FileMode.Open, FileAccess.Read))
            using (WDocument originalDocument = new WDocument(originalStream, WFormatType.Docx))
            {
                using (FileStream revisedStream = new FileStream(secondFile, FileMode.Open, FileAccess.Read))
                using (WDocument revisedDocument = new WDocument(revisedStream, WFormatType.Docx))
                {
                    originalDocument.Compare(revisedDocument);
                    WordDocument document = WordDocument.Load(originalDocument);
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(document);
                    document.Dispose();
                    return json;
                }
            }
        }
        //Lists all documents in the S3 bucket with their version info, mirroring GetAllDocuments behavior.
        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("GetAllDocumentsFromS3")]
        public string GetAllDocumentsFromS3()
        {
            List<S3DocumentDetails> documents = new List<S3DocumentDetails>();
            using (var s3Client = GetS3Client())
            {
                Dictionary<string, string> userMap = GetS3UserMap(s3Client);
                ListObjectsV2Request listRequest = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Delimiter = "/"
                };
                List<string> documentPrefixes = new List<string>();
                ListObjectsV2Response listResponse;
                try
                {
                    do
                    {
                        listResponse = s3Client.ListObjectsV2Async(listRequest).GetAwaiter().GetResult();
                        if (listResponse != null && listResponse.CommonPrefixes != null)
                        {
                            documentPrefixes.AddRange(listResponse.CommonPrefixes);
                        }
                        listRequest.ContinuationToken = listResponse != null ? listResponse.NextContinuationToken : null;
                    } while (listResponse != null && listResponse.IsTruncated == true);
                }
                catch (Amazon.S3.AmazonS3Exception ex) when (
                    ex.StatusCode == System.Net.HttpStatusCode.NotFound ||
                    ex.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                    ex.ErrorCode == "NoSuchBucket" ||
                    ex.ErrorCode == "AccessDenied")
                {
                    //Bucket missing or no list permission: return an empty document list.
                    return Newtonsoft.Json.JsonConvert.SerializeObject(documents);
                }
                foreach (string prefix in documentPrefixes)
                {
                    List<S3Object> versionObjects = GetS3VersionObjects(s3Client, prefix);
                    if (versionObjects.Count == 0)
                        continue;
                    S3Object latestVersion = versionObjects.OrderByDescending(o => o.LastModified).First();
                    string modifiedUser = "";
                    if (userMap.ContainsKey(latestVersion.Key))
                    {
                        modifiedUser = userMap[latestVersion.Key];
                    }
                    documents.Add(new S3DocumentDetails
                    {
                        FileName = prefix.TrimEnd('/'),
                        ModifiedUser = modifiedUser,
                        VersionCount = versionObjects.Count,
                        LastModifiedTime = latestVersion.LastModified
                    });
                }
            }
            return Newtonsoft.Json.JsonConvert.SerializeObject(documents.OrderByDescending(d => d.LastModifiedTime).ToList());
        }
        //Loads the latest version of the given document from the S3 bucket and returns it as sfdt.
        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("LoadLatestVersionDocumentFromS3")]
        //Accept a Dictionary instead of the legacy UploadDocument so the same payload works
        //whether the client sends { fileName: "..." } (current) or { DocumentName: "..." } (legacy docker).
        public string LoadLatestVersionDocumentFromS3([FromBody] Dictionary<string, string> jsonObject)
        {
            if (jsonObject == null)
                return "";
            string documentName = null;
            if (jsonObject.ContainsKey("fileName")) documentName = jsonObject["fileName"];
            if (string.IsNullOrEmpty(documentName) && jsonObject.ContainsKey("DocumentName")) documentName = jsonObject["DocumentName"];
            if (string.IsNullOrEmpty(documentName)) return "";
            string folderName = documentName;
            if (!folderName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                folderName = folderName + ".docx";
            }
            string prefix = folderName.TrimEnd('/') + "/";
            using (var s3Client = GetS3Client())
            {
                List<S3Object> versionObjects = GetS3VersionObjects(s3Client, prefix);
                if (versionObjects.Count == 0)
                    return "";
                S3Object latestVersion = versionObjects.OrderByDescending(o => o.LastModified).First();
                using (MemoryStream stream = DownloadS3Object(s3Client, latestVersion.Key))
                {
                    WordDocument document = WordDocument.Load(stream, FormatType.Docx);
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(document);
                    document.Dispose();
                    return json;
                }
            }
        }
        //Loads a specific version (by S3 key) and returns it as sfdt (uses the docs sample contract).
        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("LoadDocumentFromS3")]
        public string LoadDocumentFromS3([FromBody] Dictionary<string, string> jsonObject)
        {
            if (jsonObject == null || !jsonObject.ContainsKey("documentName"))
                return null;
            string documentName = jsonObject["documentName"];
            using (var s3Client = GetS3Client())
            {
                using (MemoryStream stream = DownloadS3Object(s3Client, documentName))
                {
                    WordDocument document = WordDocument.Load(stream, FormatType.Docx);
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(document);
                    document.Dispose();
                    return json;
                }
            }
        }
        //Saves a document (multipart form data, library contract) to the S3 bucket as its next version.
        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("SaveToS3")]
        public void SaveToS3(IFormCollection data)
        {
            if (data.Files.Count == 0)
                return;
            string documentName = this.GetValue(data, "documentName");
            string modifiedUser = this.GetValue(data, "modifiedUser");
            Stream stream = new MemoryStream();
            IFormFile file = data.Files[0];
            file.CopyTo(stream);
            stream.Position = 0;
            SaveVersionToS3(documentName, stream, modifiedUser);
        }
        public class ExportData
        {
            public string fileName { get; set; }
            public string modifiedUser { get; set; }
            public string documentData { get; set; }
        }
        //Saves a document (JSON body with base64-encoded .docx, same contract as the local AutoSave API).
        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("AutoSaveToS3")]
        public void AutoSaveToS3([FromBody] ExportData exportData)
        {
            if (exportData == null || string.IsNullOrEmpty(exportData.documentData))
                return;
            byte[] data = Convert.FromBase64String(exportData.documentData.Split(',')[1]);
            using (MemoryStream stream = new MemoryStream(data))
            {
                SaveVersionToS3(exportData.fileName, stream, exportData.modifiedUser);
            }
        }
        //Returns the version-history tree (along with the latest-vs-previous comparison document)
        //for the given document, mirroring the local GetVersionData response shape.
        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("GetVersionDataFromS3")]
        //Accept a Dictionary so the same payload shape used by the shared client works against this docker project.
        public string GetVersionDataFromS3([FromBody] Dictionary<string, string> jsonObject)
        {
            if (jsonObject == null)
                return null;
            string documentName = null;
            if (jsonObject.ContainsKey("fileName")) documentName = jsonObject["fileName"];
            if (string.IsNullOrEmpty(documentName) && jsonObject.ContainsKey("DocumentName")) documentName = jsonObject["DocumentName"];
            if (string.IsNullOrEmpty(documentName)) return null;
            string folderName = documentName;
            if (!folderName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                folderName = folderName + ".docx";
            }
            string prefix = folderName + "/";
            using (var s3Client = GetS3Client())
            {
                Dictionary<string, string> userMap = GetS3UserMap(s3Client);
                List<S3Object> versionObjects = GetS3VersionObjects(s3Client, prefix);
                if (versionObjects.Count == 0)
                    return null;
                List<S3Object> orderedVersions = versionObjects.OrderByDescending(o => o.LastModified).ToList();
                S3CompareData compare = new S3CompareData();
                if (orderedVersions.Count > 1)
                {
                    //Download both latest versions to temporary files and run DocIO compare on them,
                    //then return only the diff SFdt string (matches the local GetVersionData contract).
                    string firstStream = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    string secondStream = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    using (var fs = new FileStream(firstStream, FileMode.Create, FileAccess.Write))
                    {
                        using (var ms = DownloadS3Object(s3Client, orderedVersions[0].Key)) { ms.CopyTo(fs); }
                    }
                    using (var fs = new FileStream(secondStream, FileMode.Create, FileAccess.Write))
                    {
                        using (var ms = DownloadS3Object(s3Client, orderedVersions[1].Key)) { ms.CopyTo(fs); }
                    }
                    compare.Document = CompareDocumentsToSfdts(firstStream, secondStream);
                    try { System.IO.File.Delete(firstStream); System.IO.File.Delete(secondStream); } catch { }
                }
                else
                {
                    using (MemoryStream stream = DownloadS3Object(s3Client, orderedVersions[0].Key))
                    {
                        WordDocument document = WordDocument.Load(stream, FormatType.Docx);
                        compare.Document = Newtonsoft.Json.JsonConvert.SerializeObject(document);
                        document.Dispose();
                    }
                }
                List<S3Version> versionEntities = new List<S3Version>();
                foreach (S3Object s3Object in orderedVersions)
                {
                    string modifiedUser = userMap.ContainsKey(s3Object.Key) ? userMap[s3Object.Key] : "";
                    //AWS returns LastModified as a UTC DateTime. Normalize to the server's local time so
                    //both the tree label below and the LastModifiedTime field serialized to the React grid
                    //display the correct local date/time.
                    DateTime lastModified = DateTime.SpecifyKind(s3Object.LastModified, DateTimeKind.Utc).ToLocalTime();
                    versionEntities.Add(new S3Version(System.IO.Path.GetFileName(s3Object.Key), s3Object.Key, modifiedUser, lastModified));
                }
                var groupedDates = versionEntities.GroupBy(v => v.LastSavedTime.Date).OrderByDescending(g => g.Key);
                List<RootObject> rootObjects = new List<RootObject>();
                foreach (var group in groupedDates)
                {
                    List<SubChild> childObjects = new List<SubChild>();
                    List<S3Version> tempVersion = group.ToArray().OrderByDescending(v => v.LastSavedTime).ToList();
                    foreach (var child in tempVersion)
                    {
                        childObjects.Add(new SubChild
                        {
                            id = child.DocumentVersion,
                            name = child.LastSavedTime.ToString("MMMM dd, hh:mm tt"),
                            user = child.ModifiedUser
                        });
                    }
                    rootObjects.Add(new RootObject
                    {
                        id = group.Key.ToString("yyyy-MM-dd"),
                        name = group.Key.ToString("MMMM dd"),
                        user = childObjects[0].user,
                        subChild = childObjects
                    });
                }
                compare.Data = rootObjects;
                return Newtonsoft.Json.JsonConvert.SerializeObject(compare);
            }
        }
        public class CompareDocument
        {
            public string DocumentName { get; set; }
            public string SelectedVersion { get; set; }
        }
        //S3 version of CompareSelectedVersion. The React version-history tree click hits this endpoint and
        //passes the document name plus the version object key; we diff it against the previous version.
        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("CompareSelectedVersionFromS3")]
        //Accept a Dictionary so the shared client (which sends DocumentName in compareSelectedVersion) works regardless of payload casing.
        public string CompareSelectedVersionFromS3([FromBody] Dictionary<string, string> jsonObject)
        {
            if (jsonObject == null)
            {
                return "{}";
            }
            string folderName = null;
            if (jsonObject.ContainsKey("DocumentName")) folderName = jsonObject["DocumentName"];
            if (string.IsNullOrEmpty(folderName) && jsonObject.ContainsKey("documentName")) folderName = jsonObject["documentName"];
            string selectedVersion = null;
            if (jsonObject.ContainsKey("SelectedVersion")) selectedVersion = jsonObject["SelectedVersion"];
            if (string.IsNullOrEmpty(selectedVersion) && jsonObject.ContainsKey("selectedVersion")) selectedVersion = jsonObject["selectedVersion"];
            if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(selectedVersion))
            {
                return "{}";
            }
            if (!folderName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                folderName = folderName + ".docx";
            }
            using (var s3Client = GetS3Client())
            {
                string prefix = folderName.TrimEnd('/') + "/";
                List<S3Object> versionObjects = GetS3VersionObjects(s3Client, prefix);
                if (versionObjects.Count == 0)
                {
                    return "{}";
                }
                List<S3Object> orderedVersions = versionObjects.OrderByDescending(o => o.LastModified).ToList();
                string targetKey = prefix + selectedVersion;
                int targetIndex = orderedVersions.FindIndex(o => o.Key == targetKey);
                if (targetIndex == -1)
                {
                    return "{}";
                }
                string firstStream = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                string secondStream = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                try
                {
                    using (var fs = new FileStream(firstStream, FileMode.Create, FileAccess.Write))
                    {
                        using (var ms = DownloadS3Object(s3Client, orderedVersions[targetIndex].Key)) { ms.CopyTo(fs); }
                    }
                    string revisedKey = (targetIndex + 1 < orderedVersions.Count) ? orderedVersions[targetIndex + 1].Key : orderedVersions[targetIndex].Key;
                    using (var fs = new FileStream(secondStream, FileMode.Create, FileAccess.Write))
                    {
                        using (var ms = DownloadS3Object(s3Client, revisedKey)) { ms.CopyTo(fs); }
                    }
                    S3Version o = new S3Version(System.IO.Path.GetFileName(orderedVersions[targetIndex].Key), firstStream, null, orderedVersions[targetIndex].LastModified);
                    S3Version n = new S3Version(System.IO.Path.GetFileName(revisedKey), secondStream, null, targetIndex + 1 < orderedVersions.Count ? orderedVersions[targetIndex + 1].LastModified : orderedVersions[targetIndex].LastModified);
                    WordDocument result = Compare(o, n);
                    string sfdtString = Newtonsoft.Json.JsonConvert.SerializeObject(result);
                    //Match the response shape used by the legacy CompareSelectedVersion (the React client reads response.sfdt).
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { sfdt = sfdtString });
                }
                finally
                {
                    try { System.IO.File.Delete(firstStream); } catch { }
                    try { System.IO.File.Delete(secondStream); } catch { }
                }
            }
        }
        public class DownloadData
        {
            public string DocumentName { get; set; }
            public string SelectedVersion { get; set; }
        }
        //S3 version of the version-history download button. Returns the version object as a streamed file.
        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("DownloadFromS3")]
        public IActionResult DownloadFromS3([FromBody] Dictionary<string, string> jsonObject)
        {
            if (jsonObject == null)
            {
                return BadRequest();
            }
            string folderName = null;
            if (jsonObject.ContainsKey("DocumentName")) folderName = jsonObject["DocumentName"];
            if (string.IsNullOrEmpty(folderName) && jsonObject.ContainsKey("documentName")) folderName = jsonObject["documentName"];
            string selectedVersion = null;
            if (jsonObject.ContainsKey("SelectedVersion")) selectedVersion = jsonObject["SelectedVersion"];
            if (string.IsNullOrEmpty(selectedVersion) && jsonObject.ContainsKey("selectedVersion")) selectedVersion = jsonObject["selectedVersion"];
            if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(selectedVersion))
            {
                return BadRequest();
            }
            if (!folderName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                folderName = folderName + ".docx";
            }
            //SelectedVersion here is the S3 key suffix of the version object (e.g. "v3.docx").
            string key = folderName.TrimEnd('/') + "/" + selectedVersion;
            try
            {
                using (var s3Client = GetS3Client())
                {
                    MemoryStream memoryStream = DownloadS3Object(s3Client, key);
                    string fileName = folderName.Split('.')[0] + "_" + selectedVersion;
                    return new FileStreamResult(memoryStream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                    {
                        FileDownloadName = fileName
                    };
                }
            }
            catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }
        #endregion
    }

}
