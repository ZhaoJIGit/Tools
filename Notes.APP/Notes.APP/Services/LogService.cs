using Notes.APP.Common;
using Notes.APP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notes.APP.Services
{
    public class LogService
    {
        private DBHelper dBHelper;
        public LogService()
        {
            dBHelper = new DBHelper();
        }
        private static LogService _instance;
        private static readonly object _lock = new object();


        public static LogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LogService();
                        }
                    }
                }
                return _instance;
            }
        }
        public bool AddMessage(string message)
        {
            var sql = $@" INSERT INTO LogInfo (Message,
                                CreateTime) 
                          Values('{message}',
                                CURRENT_TIMESTAMP)";
            var result = dBHelper.ExecuteNonQuery(sql);
            return result > 0;
        }
    }
}
