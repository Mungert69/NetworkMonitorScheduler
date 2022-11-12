using NetworkMonitor.Objects;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace NetworkMonitor.Objects
{
    public class SentUserData 
    {
      private UserInfo _user;
      private int _dataSetId;
      private int _monitorPingInfoId;

        public UserInfo User { get => _user; set => _user = value; }
        public int DataSetId { get => _dataSetId; set => _dataSetId = value; }
        public int MonitorPingInfoId { get => _monitorPingInfoId; set => _monitorPingInfoId = value; }
    }
}
