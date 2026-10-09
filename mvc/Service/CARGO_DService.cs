using System.Collections.Generic;
using System.Linq;
using Dapper;
using CARGO.DTO.Tables;

namespace CARGO.Service
{
    public sealed class CARGO_DService
    {
        public List<CARGO_D_OPTION> GetOptions()
        {
#if LOCAL_DEMO
            return CARGO_D01DemoData.Options();
#else
            using (var db = CARGO_DDatabase.Open())
                return db.Query<CARGO_D_OPTION>("SELECT OPTION_TYPE, CODE, LABEL, SORT_NO, ACTIVE FROM CARGO_D_OPTION WHERE ACTIVE = 1 ORDER BY OPTION_TYPE, SORT_NO, CODE").ToList();
#endif
        }
    }
}
