#region public HttpResponseMessage UploadAttachment(int projectId, int loggedInId)

        // POST api/values
        [Route("Api/Project/UploadAttachment")]
        [System.Web.Http.HttpPost]
        public HttpResponseMessage UploadAttachment(int projectId, int loggedInId, int ServiceId, int ServiceTypeId)
        {
            var projectSelectResult = new Result<Project>
            {
                Message = string.Empty,
                Status = true,
                Data = new Project()
            };

            var projectAttachmentInsert = new Result<int>
            {
                Message = string.Empty,
                Status = true,
                Data = default(int)
            };

            try
            {


                if (projectId > 0)
                {
                    projectSelectResult = _projectBusiness.Select(projectId);

                    var Multipleservice = _multipleServiceProjectBusiness.Where(x => x.ProjectId == projectId && x.ServiceId == ServiceId).Data.FirstOrDefault();
                    Multipleservice.CurrentUploadSequenceNumber = Multipleservice.CurrentUploadSequenceNumber + 1;

                    var MultiServiceUpdate = _multipleServiceProjectBusiness.Update(Multipleservice);
                    if (!MultiServiceUpdate.Status)
                        throw new Exception(MultiServiceUpdate.Message);

                    ////projectSelectResult.Data.CurrentUploadSequenceNumber =
                    ////    projectSelectResult.Data.CurrentUploadSequenceNumber + 1;
                    ////var projectUpdate = _projectBusiness.Update(projectSelectResult.Data);
                    //if (!projectUpdate.Status)
                    //    throw new Exception(projectUpdate.Message);

                    if (projectSelectResult.Data.Id > 0)
                    {

                        var httpRequest = HttpContext.Current.Request;
                        string[] FileExtensions = new string[] { ".pdf", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".gif", ".png", ".jpg", ".jpeg", ".mp3", ".mp4", ".PDF", ".TXT", ".DOC", ".DOCX", ".XLS", ".XLSX", ".GIF", ".PNG", ".JPG", ".JPEG", ".MP3", ".MP4" };
                        if (httpRequest.Files.Count > 0)
                        {
                            for (int i = 0; i < httpRequest.Files.Count; i++)
                            {
                                string fileExtn = string.Empty;
                                var postedFile = httpRequest.Files[i];
                                if (postedFile != null)
                                {
                                    fileExtn = Path.GetExtension(postedFile.FileName);
                                    if (FileExtensions.Any(x => x == fileExtn))
                                    {
                                        string replacestr = Regex.Replace(postedFile.FileName, "[^a-zA-Z0-9_.]+", "");

                                        var fileName = replacestr.Replace(" ", "_");
                                        string displayName = fileName;

                                        fileName = string.Format("{0}_{1}", AppGlobal.CurrentMiliseconds, fileName);

                                        var filePath = string.Format("{0}\\{1}", AppGlobal.ProjectAttachmentPath,
                                            fileName);
                                        LogDataAccess.Log(GetType().Name, filePath.ToString());
                                        postedFile.SaveAs(filePath);
                                        // NOTE: To store in memory use postedFile.InputStream
                                        var Users = _userBusiness.Select(loggedInId);

                                        // NOTE: To store in memory use postedFile.InputStream
                                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                                            CloudConfigurationManager.GetSetting("StorageConnectionString"));
                                        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                                        CloudBlobContainer container =
                                            blobClient.GetContainerReference(
                                                ConfigurationManager.AppSettings[
                                                    "AzureContainerForProjectAttachmentsIn"]);

                                        // Create the container if it doesn't already exist.
                                        container.CreateIfNotExists();
                                        container.SetPermissions(new BlobContainerPermissions
                                        {
                                            PublicAccess = BlobContainerPublicAccessType.Blob
                                        });
                                        // Retrieve reference to a blob named "myblob".

                                        string refference;

                                        if (Users.Data.RoleId == 2)
                                        {
                                            refference = "OUT/";
                                        }
                                        else
                                        {
                                            refference = "IN/";
                                        }

                                        CloudBlockBlob blockBlob =
                                            container.GetBlockBlobReference(string.Format("{0}{1}", refference, fileName));
                                        string apPath =
                                            System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;

                                        // Create or overwrite the "myblob" blob with contents from a local file.
                                        using (var fileStream = System.IO.File.OpenRead(filePath))
                                        {
                                            blockBlob.UploadFromStream(fileStream);
                                            Console.WriteLine(blockBlob.Uri.ToString());
                                        }
                                        if (!filePath.Equals(string.Empty))
                                        {
                                            System.IO.File.Delete(filePath);
                                        }
                                        //      fileName = blockBlob.Uri.ToString();
                                        //fileName = blockBlob.Name.Substring(3);

                                        var projectAttachment = new ProjectAttachment { Path = fileName, DisplayName = displayName };

                                        projectAttachmentInsert = new Result<int>
                                        {
                                            Message = string.Empty,
                                            Status = true,
                                            Data = default(int)
                                        };

                                        if (!fileName.Equals(string.Empty))
                                            projectAttachment.Extension = filePath.Equals(string.Empty)
                                                ? string.Empty
                                                : Path.GetExtension(filePath);

                                        projectAttachment.ProjectId = projectSelectResult.Data.Id;
                                        projectAttachment.ForStatus = false;
                                        projectAttachment.AttachedOn = DateTime.UtcNow;
                                        projectAttachment.PostedBy = loggedInId;
                                        projectAttachment.UploadSequenceNumber = Multipleservice.CurrentUploadSequenceNumber;
                                        projectAttachment.ServiceId = ServiceId;
                                        projectAttachment.ServiceTypeId = ServiceTypeId;
                                        projectAttachmentInsert = _projectAttachmentBusiness.Insert(projectAttachment);
                                        if (!projectAttachmentInsert.Status)
                                            throw new Exception(projectAttachmentInsert.Message);

                                    }
                                    else
                                    {
                                        projectAttachmentInsert.Message = string.Format("Please check the upload file : {0}", postedFile.FileName);
                                    }
                                }

                            }

                        }
                        projectAttachmentInsert.Message = string.Format("Insert Successfully");
                        return Request.CreateResponse(HttpStatusCode.Created, projectAttachmentInsert);

                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.OK,
                            new Result<int>
                            {
                                Status = false,
                                Message = string.Format("{0} ", projectSelectResult.Data.Id),

                                Data = default(int)
                            });
                    }

                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new Result<int>
                    {
                        Status = false,
                        Message = "Project Id is less than or equal zero",
                        Data = default(int)
                    });
                }

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new Result<int>
                {
                    Status = false,
                    Message = ex.Message,
                    Data = default(int)
                });
                //throw new Exception(ex.Message);
            }
        }

        #endregion