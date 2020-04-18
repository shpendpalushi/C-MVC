using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using CrudMvc.Models;
using GemBox.Document;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Path = System.IO.Path;

namespace CrudMvc.Controllers
{
    public class RequestController : Controller
    {
        // GET: Request
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetData()
        {
            using (Request_ManagerEntities4 db = new Request_ManagerEntities4())
            {
                List<RM_Request> requestList = db.RM_Request.ToList<RM_Request>();
                SetStatusName(requestList);
                //List<RequestForJson> requestListJson = SerializeForJsonRequest(requestList);
                return Json(new { data = requestList }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult DownloadFile(int id)
        {
            using (Request_ManagerEntities4 db = new Request_ManagerEntities4())
            {
                RM_Request req = db.RM_Request.Find(id);
                if(req!=null)
                {
                    if (req.RT_Document_Name.Contains(".pdf"))
                        Response.ContentType = "application/pdf";
                    else
                        Response.ContentType = "Application/msword";
                    Response.AppendHeader("Content-Disposition", "attachment; filename="+findSimpleName(req.RT_Document_Name));
                    Response.TransmitFile(req.RT_Document_Name);
                    Response.End();
                    return Json(new { success = true, message = "Dokumenti u shkarkua me sukses!", JsonRequestBehavior.AllowGet });
                }
                return Json(new { success = false, message = "Nuk mund të shkarkohet", JsonRequestBehavior.AllowGet });

            }
                
        }

        [HttpGet]
        public ActionResult AddOrEdit(int id = 0)
        {
            using (Request_ManagerEntities4 manager = new Request_ManagerEntities4())
            {
                var get_Status_List = manager.RM_Status_Table.ToList();
                SelectList status = new SelectList(get_Status_List, "ST_ID_Status", "ST_Status_Name");
                ViewBag.status = status;
                if (id == 0)
                    return View(new RM_Request());
                else
                {
                    using (Request_ManagerEntities4 db = new Request_ManagerEntities4())
                    {
                        return View(db.RM_Request.Where(x => x.RTID_Request == id).FirstOrDefault<RM_Request>());
                    }
                }
            }

        }

        //Editimi dhe shtimi behen duke therritur te njejten url, brenda funksioni

        [HttpPost]
        public ActionResult AddOrEdit(RM_Request request)
        {
            using (Request_ManagerEntities4 db = new Request_ManagerEntities4())
            {

                if (request.RTID_Request == 0)
                {
                    Add(db, request);

                    return Json(new { success = true, message = "Rekordi u ruajt me sukses!", JsonRequestBehavior.AllowGet });
                }
                else
                {
                    var entity = db.Set<RM_Request>().Find(request.RTID_Request);
                    string name = entity.RT_Document_Name;
                    DateTime? date = entity.RT_Request_Registered_DateTime;
                    db.Entry(entity).State = EntityState.Detached;
                    Edit(db, request,name,date);
                    return Json(new { success = true, message = "Rekordi u ndryshua me sukses!", JsonRequestBehavior.AllowGet });
                }

            }

        }
        //Metoda post per fshirjen- Behet kontrolli nese id e derguar me post eshte valide,
        //nese, po realizohet fshirja
        [HttpPost]
        public ActionResult Delete(int id)
        {
            using (Request_ManagerEntities4 db = new Request_ManagerEntities4())
            {
                RM_Request request = db.RM_Request.Where(x => x.RTID_Request == id).FirstOrDefault<RM_Request>();
                db.RM_Request.Remove(request);
                db.SaveChanges();
                return Json(new { success = true, message = "Rekordi u fshi nga baza e të dhënave!", JsonRequestBehavior.AllowGet });
            }
        }

        //Behet vendosja e nje emri identifikues IDEmer, per te mundesuar shkarkimin e file nga tabela
        private void SetStatusName(List<RM_Request> list)
        {
            Request_ManagerEntities4 db = new Request_ManagerEntities4();
            foreach (RM_Request r in list)
            {
                RM_Status_Table st = db.RM_Status_Table.Find(r.RT_Status_ID);
                r.Status = st.ST_Status_Name;
                if (r.RT_Document_Name != null) 
                {

                    r.Simple_Name =r.RTID_Request.ToString() +"/"+ findSimpleName(r.RT_Document_Name);
                    System.Diagnostics.Debug.WriteLine(r.Simple_Name);
                }
            }

        }

        //Funksioni i shtimit te kerkeses, ne fillim ruhet file ne direktorine e caktuar, pastaj ruhet kerkesa
        private void Add(Request_ManagerEntities4 db, RM_Request request)
        {
            request.RT_Request_Registered_DateTime = System.DateTime.Now;
            if (request.File != null) 
            {
                string path = Server.MapPath("~/App_Data/File");
                string name = Path.GetFileName(request.File.FileName);
                string fullPath = Path.Combine(path, name);
                request.RT_Document_Name = fullPath;
                request.File.SaveAs(fullPath);
                if (Path.GetExtension(request.File.FileName).Equals(".docx"))
                    request.RT_Document_Content = ReadFromWordFile(fullPath);
                else
                    request.RT_Document_Content = ReadFromPDFFile(fullPath);
            }
            db.RM_Request.Add(request);
            db.SaveChanges();
        }

        //Funksioni edit, Kontrollohet nese eshte shtuar file e r, nese po ajo duhet ruajtur
        //dhe zevendesuar, nese jo ruhen te dhenat e ndryshuara
        private void Edit(Request_ManagerEntities4 db, RM_Request request, string document_name,DateTime? date)
        {
            request.RT_Request_Registered_DateTime = date;
            db.Entry(request).State = System.Data.Entity.EntityState.Modified;
            string path = Server.MapPath("~/App_Data/File");
            if (request.File != null)
            {
                string name = Path.GetFileName(request.File.FileName);
                string fullPath = System.IO.Path.Combine(path, name);
                request.RT_Document_Name = fullPath;
                request.File.SaveAs(fullPath);
                if (Path.GetExtension(request.File.FileName).Equals(".docx"))
                    request.RT_Document_Content = ReadFromWordFile(fullPath);
                else
                    request.RT_Document_Content = ReadFromPDFFile(fullPath);
            }
            else 
            {
                request.RT_Document_Name = document_name;
            }
            
            db.SaveChanges();
        }

        //Sherben per leximin e text nga file word (.docx)
        private string ReadFromWordFile(String location)
        {
            ComponentInfo.SetLicense("FREE-LIMITED-KEY");
            var document = new DocumentModel();
            document = DocumentModel.Load(location);
            return document.Sections[0].
                    Blocks.Cast<Paragraph>(0).
                    Inlines.Cast<Run>(0).Text;
        }

        //Sherben per leximin e text nga file pdf (.pdf)
        private string ReadFromPDFFile(string location)
        {
            StringBuilder text = new StringBuilder();
            using (PdfReader reader = new PdfReader(location))
            {
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
                }
            }

            return text.ToString();
        }


        //Sherben per te nxjerre vetem emrin e file pa location
        private string findSimpleName(string name)
        {
            try
            {
                int index = name.LastIndexOf(@"\");
                return name.Substring(index + 1);
            }catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return null;
            }
            
        }
    }
}