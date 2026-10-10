using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CARGO.Web.ViewModels.CARGO_D01;
using CARGO.Web.ViewModels.Shared;
namespace CARGO.Web.ViewModels.CARGO_D03 {
    public sealed class CARGO_D03DetailViewModel {
        public CARGO_D01DetailViewModel Application { get; set; }
        public List<CARGO_D03ResourceViewModel> Resources { get; set; }
        public List<CARGO_DSignViewModel> Signs { get; set; }
        public List<string> PriorityRequests { get; set; }
        public bool CanDispatch { get; set; }
        public bool CanChange { get; set; }
    }
}
