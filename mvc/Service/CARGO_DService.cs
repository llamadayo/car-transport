using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using CARGO.DTO.Tables;

namespace CARGO.Service
{
    public sealed class CARGO_DService
    {
        // Display lookup only: include inactive resources so historical assignments remain readable.
        public List<CARGO_D_RESOURCE> GetResourceLabels(IEnumerable<string> resourceIds)
        {
            var ids = resourceIds.Where(x => !String.IsNullOrEmpty(x) && x != "SELF").Distinct().ToList();
            var result = new List<CARGO_D_RESOURCE>();
            if (ids.Count == 0) return result;
#if LOCAL_DEMO
            return CARGO_DWorkflowDemoData.Load().Resources.Where(x => ids.Contains(x.RESOURCE_ID)).ToList();
#else
            using (var db = CARGO_DDatabase.Open())
            {
                // Stay below Oracle's IN-list limit; values are always bound parameters.
                for (var offset = 0; offset < ids.Count; offset += 500)
                {
                    var batch = ids.Skip(offset).Take(500).ToList();
                    var values = new Dictionary<string, object>();
                    for (var i = 0; i < batch.Count; i++) values.Add("Id" + i, batch[i]);
                    var names = String.Join(",", values.Keys.Select(x => ":" + x));
                    result.AddRange(db.Query<CARGO_D_RESOURCE>("SELECT RESOURCE_ID, KIND, NAME FROM CARGO_D_RESOURCE WHERE POOL = 'BIZ' AND RESOURCE_ID IN (" + names + ")", CARGO_DDatabase.Params(values)));
                }
            }
            return result;
#endif
        }

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
