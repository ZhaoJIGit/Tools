using Newtonsoft.Json;
using Notes.APP.Common;
using Notes.APP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;

namespace Notes.APP.Services
{
    public class HitokotoService
    {
        //a 动画
        //b 漫画
        //c 游戏
        //d 文学
        //e 原创
        //f 来自网络
        //g 其他
        //h 影视
        //i 诗词
        //j 网易云
        //k 哲学
        //l 抖机灵
        //其他 作为 动画 类型处
        public string GetHitokotoInfoApi()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string host = "https://v1.hitokoto.cn/?c=d&c=e&c=i&c=j&c=k";
                    httpClient.Timeout = TimeSpan.FromSeconds(3);
                    HttpResponseMessage response = httpClient.GetAsync(host).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();
                    string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    // 解析 JSON 数据
                    var model = JsonConvert.DeserializeObject<HitokotoInfo>(responseBody);
                    if (model != null && !string.IsNullOrWhiteSpace(model.hitokoto))
                    {
                        AddHitokoto(model);
                    }
                }
            }
            catch (Exception)
            {
            }
            return "";
        }
        private DBHelper dBHelper;
        public HitokotoService()
        {
            dBHelper = new DBHelper();
        }
        private static HitokotoService _instance;
        private static readonly object _lock = new object();


        public static HitokotoService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new HitokotoService();
                        }
                    }
                }
                return _instance;
            }
        }
        public bool AddHitokoto(HitokotoInfo model)
        {
            var sql = $@" INSERT INTO HitokotoInfo (hitokoto,type ) 
                          Values(@hitokoto,@type)";
            var result = dBHelper.ExecuteNonQuery(sql, model);
            return result > 0;
        }
        public string GetHitokoto()
        {
            var sql = $@"  SELECT * FROM HitokotoInfo order by Id desc LIMIT 1";
            var result = dBHelper.ExecuteReaderToModel<HitokotoInfo>(sql);
            if (result != null && !string.IsNullOrWhiteSpace(result.hitokoto))
            {
                return result.hitokoto;
            }
            return "";
        }
    }
}
