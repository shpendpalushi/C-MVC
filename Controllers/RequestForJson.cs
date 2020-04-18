using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CrudMvc.Controllers
{
    public class RequestForJson
    {
        public int RTID_Request { get; set; }

        public string RT_Request_Description { get; set; }
        public string RT_Request_Registered_DateTime { get; set; }
        public string RT_Request_Finished_Date { get; set; }
        
        public int RT_Status_ID { get; set; }
        public string RT_Document_Name { get; set; }
        public string RT_Document_Content { get; set; }
        public string RT_Request_Taken_Date { get; set; }
        public string RT_Title { get; set; }
    }
}