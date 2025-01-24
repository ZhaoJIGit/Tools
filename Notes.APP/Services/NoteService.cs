using Notes.APP.Common;
using Notes.APP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notes.APP.Services
{
    public class NoteService
    {
        private DBHelper dBHelper;
        public NoteService()
        {
            dBHelper = new DBHelper();
        }
        public bool AddNote(NoteModel model) {
            var sql = $@" INSERT INTO NoteInfo (NoteName,
                                Content,
                                CreateTime,
                                UpdateTime,
                                Color,
                                BackgroundColor,
                                PageBackgroundColor,
                                Opacity, 
                                XAxis,
                                YAxis,
                                IsDeleted) 
                          Values(@NoteName,
                                @Content,
                                @CreateTime,
                                @UpdateTime,
                                @Color,
                                @BackgroundColor,
                                @PageBackgroundColor,
                                @Opacity, 
                                @XAxis,
                                @YAxis,
                                @IsDeleted)";
            var result= dBHelper.ExecuteNonQuery(sql,model);
            return result > 0;
        }
        public bool UpdateNote(NoteModel model)
        {
            var sql = $@" Update NoteInfo set Content =@Content,UpdateTime=@UpdateTime ,BackgroundColor=@BackgroundColor ,Color =@Color,Opacity=@Opacity,XAxis=@XAxis,YAxis=@YAxis where NoteId =@NoteId";
            var result = dBHelper.ExecuteNonQuery(sql, model);
            return result > 0;
        }

        public NoteModel SelectNote(long id)
        {
            var sql = $@" SELECT * FROM NoteInfo WHERE NoteId = {id}";
            var result = dBHelper.ExecuteReaderToModel<NoteModel>(sql);
            return result;
        }
    }
}
