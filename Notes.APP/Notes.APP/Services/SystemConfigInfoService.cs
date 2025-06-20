using Notes.APP.Common;
using Notes.APP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Notes.APP.Services
{
    public class SystemConfigInfoService
    {
        private DBHelper dBHelper;
        public SystemConfigInfoService()
        {
            dBHelper = new DBHelper();
        }
        private static SystemConfigInfoService _instance;
        private static readonly object _lock = new object();


        public static SystemConfigInfoService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SystemConfigInfoService();
                        }
                    }
                }
                return _instance;
            }
        }
        public bool SaveConfig(SystemConfigInfo message)
        {
            var model = GetConfig();
            if (model == null)
            {
                var sql = $@" INSERT INTO SystemConfigInfo(StartOpen,Color,BackgroundColor,Fixed) values({message.StartOpen},'{message.Color}','{message.BackGroundColor}','{message.Fixed}')";
                var result = dBHelper.ExecuteNonQuery(sql);
                return result > 0;
            }
            else
            {
                var sql = $@"UPDATE SystemConfigInfo set  StartOpen={message.StartOpen},Color='{message.Color}',BackgroundColor='{message.BackGroundColor}',Fixed='{message.Fixed}' where Id={model.Id}";
                var result = dBHelper.ExecuteNonQuery(sql);
                return result > 0;
            }

        }
        public SystemConfigInfo GetConfig()
        {
            var sql = $@"SELECT * FROM  SystemConfigInfo";
            var result = dBHelper.ExecuteReaderToModel<SystemConfigInfo>(sql);
            if (result == null)
            {
                var initData = $@" INSERT INTO SystemConfigInfo(StartOpen,Color,BackgroundColor,Fixed) values(0,'#fff','#66000000',0)";
                dBHelper.ExecuteNonQuery(initData);
                return GetConfig();
            }
            return result;
        }
    }
}
