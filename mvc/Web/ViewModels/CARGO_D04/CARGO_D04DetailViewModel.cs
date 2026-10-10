using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CARGO.Web.ViewModels.CARGO_D01;
using CARGO.Web.ViewModels.Shared;
namespace CARGO.Web.ViewModels.CARGO_D04 {
    public sealed class CARGO_D04DetailViewModel {
        public CARGO_D01DetailViewModel Application { get; set; }
        public List<CARGO_DSignViewModel> Signs { get; set; }
        public bool CanSign { get; set; }
    }
}
