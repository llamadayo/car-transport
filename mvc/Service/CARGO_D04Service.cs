using System.Collections.Generic;
using CARGO.DTO;
namespace CARGO.Service {
    public sealed class CARGO_D04Service {
        private readonly CARGO_DWorkflowService workflow = new CARGO_DWorkflowService();
        public CARGO_DWorkPageDto Query(CARGO_DWorkQueryDto query, CARGO_DStaffDto actor) { return workflow.Query(query, actor, 4); }
        public CARGO_DWorkDetailDto Detail(long id, CARGO_DStaffDto actor) { return workflow.Detail(id, actor, 4); }
        public void Change(List<CARGO_DWorkActionDto> actions, CARGO_DStaffDto actor) { workflow.Change(actions, actor, 4); }
        
    }
}
