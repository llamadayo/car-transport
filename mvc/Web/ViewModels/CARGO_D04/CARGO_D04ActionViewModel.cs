using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CARGO.Web.ViewModels.CARGO_D01;
using CARGO.Web.ViewModels.Shared;
namespace CARGO.Web.ViewModels.CARGO_D04 {
    public sealed class CARGO_D04ActionViewModel {
        public long RequestId { get; set; }
        public int Version { get; set; }
        public string Command { get; set; }
        [StringLength(1000)] public string Note { get; set; }
    }
}
