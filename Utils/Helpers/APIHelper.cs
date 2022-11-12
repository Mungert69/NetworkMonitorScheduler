using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using NetworkMonitor.Objects;

namespace NetworkMonitor.Utils.Helpers
{
    public class APIHelper
    {
        public static async Task<ResultObj> GetJson(string url)
        {
             ResultObj result = new ResultObj();
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<ResultObj>(responseBody);

            }
            catch (Exception ex)
            {
                result.Message += "Error in APIHelper.GetJson getting load server : Error was : " + ex.Message;
                result.Success = false;
            }
            return result;

        }
    }
}