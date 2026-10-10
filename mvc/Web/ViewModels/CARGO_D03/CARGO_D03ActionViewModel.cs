using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CARGO.Web.ViewModels.CARGO_D01;
using CARGO.Web.ViewModels.Shared;
namespace CARGO.Web.ViewModels.CARGO_D03 {
    public sealed class CARGO_D03ActionViewModel {
        public long RequestId { get; set; }
        public int Version { get; set; }
        public string Command { get; set; }
        [StringLength(1000)] public string Note { get; set; }
        public string Vehicle { get; set; }
        public string Driver1 { get; set; }
        public string Driver2 { get; set; }
        public DateTime? EffectiveAt { get; set; }
        public DateTime? EndAt { get; set; }
    }
}
