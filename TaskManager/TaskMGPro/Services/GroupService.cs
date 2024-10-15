using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMGPro.Helper;
using TaskMGPro.Models;

namespace TaskMGPro.Services
{
    public class GroupService
    {
        private static readonly SQLiteHelper databaseHelper = SQLiteHelper.Instance();
        public static int SaveGroup(GroupInfo model)
        {
            if (model.Id > 0)
            {
                return databaseHelper.Execute($@"update  GroupInfo set Title=@Title,Type=@Type,LogAddress=@LogAddress,Address=@Address where id = @Id", model);
            }
            else
            {
                return databaseHelper.Execute($@"INSERT INTO GroupInfo(Title,Type,LogAddress,Address) values(@Title,@Type,@LogAddress,@Address)", model);
            }
        }
        public static List<GroupInfo> GetGroupList()
        {
            return databaseHelper.Query<GroupInfo>($@"Select * from GroupInfo");
        }
        public static int DeleteGroup(long id)
        {
            return databaseHelper.Execute($@"delete from GroupInfo where id ={id}");
        }
    }
}
