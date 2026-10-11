using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CARGO.Web.ViewModels.CARGO_D01;
namespace CARGO.Web.ViewModels.CARGO_D05 {
    public sealed class CARGO_D05DetailViewModel {
        public CARGO_D05FormViewModel Form { get; set; }
        public CARGO_D01DetailViewModel Application { get; set; }
        public List<CARGO_D05HistoryViewModel> History { get; set; }
        public List<CARGO_D05ResourceViewModel> Resources { get; set; }
        public string Difference { get; set; }
        public CARGO_D05IndexViewModel ReturnQuery { get; set; }
        // MVC's select helper mutates Selected; never share item instances between fields.
        public List<SelectListItem> DriverOptions(bool first) {
            var options = Resources.Where(x => x.Kind == "DRIVER").Select(x => new SelectListItem { Value = x.Id, Text = x.Name + "（" + x.Id + "）" }).ToList();
            if (first && Application.Form.SelfDrive == true) options.Insert(0, new SelectListItem { Value = "SELF", Text = "使用者自駕（" + Application.Form.Applicant + "）" });
            return options;
        }
        public CARGO_D05DetailViewModel() { ReturnQuery = new CARGO_D05IndexViewModel(); }
    }
}
