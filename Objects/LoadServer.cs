using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace NetworkMonitor.Objects
{
    public class LoadServer
    {
         [Key]
        public int ID { get; set; }

        public string Url { get; set; }

        public string UserID { get; set; }
    }
}