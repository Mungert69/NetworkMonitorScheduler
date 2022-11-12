using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitor.Objects
{
    public class MonitorIP
    {
       
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public bool Enabled { get ; set; }
        public string Address { get; set; }
        public string EndPointType { get; set; }

        public int Timeout {get;set;}

        public string UserID{get;set;}

        public bool Hidden { get; set; } = false; 
        public virtual UserInfo UserInfo{get;set;}
      }
}
