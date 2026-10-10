using System.Collections.Generic;
using CARGO.Web.ViewModels.CARGO_D01;
namespace CARGO.Web.ViewModels.CARGO_D05 {
    public sealed class CARGO_D05DetailViewModel {
        public CARGO_D05FormViewModel Form { get; set; }
        public CARGO_D01DetailViewModel Application { get; set; }
        public List<CARGO_D05HistoryViewModel> History { get; set; }
        public List<CARGO_D05ResourceViewModel> Resources { get; set; }
        public string Difference { get; set; }
    }
}
