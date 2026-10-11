using System;
using System.Collections.Generic;
using System.Globalization;
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
        public DateTime? EffectiveAt { get; set; }
        public string EffectiveAtInputValue {
            get { return EffectiveAt.HasValue ? EffectiveAt.Value.ToString(EffectiveAtStep == "60" ? "yyyy-MM-ddTHH:mm" : "yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) : ""; }
        }
        public string EffectiveAtStep {
            get { return EffectiveAt.HasValue && EffectiveAt.Value.Ticks % TimeSpan.TicksPerMinute == 0 ? "60" : "0.001"; }
        }
    }
}
