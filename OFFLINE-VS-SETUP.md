# 公司離線環境的 Visual Studio 啟動準備

更新日期：2026-10-11。

適用於 `mvc/CARGO.sln` 的公司版 ASP.NET MVC 5／.NET Framework 4.8 專案。以下路徑均相對於儲存庫根目錄。

公司離線環境需要先備齊七個 NuGet 套件、公司 Kendo DLL 與配套前端資源。這些是編譯與啟動的必要準備；要實際查詢、新增及簽審，還需要公司登入、角色及 Oracle 測試環境。公司 Kendo 2019 相容性仍須現場建置驗證。

## 一 NuGet 套件

將以下已解壓縮的完整套件資料夾放到 `mvc/packages/`，保留內部 `lib` 等目錄結構。只有將 `.nupkg` 檔案放進目錄，不會讓專案自動引用其中的 DLL。

| 套件 | 版本 | 放置資料夾 |
|---|---|---|
| Microsoft.AspNet.Mvc | 5.2.7 | `mvc/packages/Microsoft.AspNet.Mvc.5.2.7/` |
| Microsoft.AspNet.Razor | 3.2.7 | `mvc/packages/Microsoft.AspNet.Razor.3.2.7/` |
| Microsoft.AspNet.WebPages | 3.2.7 | `mvc/packages/Microsoft.AspNet.WebPages.3.2.7/` |
| Microsoft.Web.Infrastructure | 1.0.0.0 | `mvc/packages/Microsoft.Web.Infrastructure.1.0.0.0/` |
| Newtonsoft.Json | **13.0.4** | `mvc/packages/Newtonsoft.Json.13.0.4/` |
| Dapper | 1.50.5 | `mvc/packages/Dapper.1.50.5/` |
| Oracle.ManagedDataAccess | **12.1.2400** | `mvc/packages/Oracle.ManagedDataAccess.12.1.2400/` |

檢查時，這七個套件的必要 DLL 在目前電腦上均存在，可預先攜帶，不必到公司連外網還原。它們被 Git 排除，單純複製 Git 追蹤檔案或下載原始碼 ZIP 不會包含這些套件。

專案實際引用的 DLL 路徑如下：

```text
mvc/packages/
  Microsoft.AspNet.Mvc.5.2.7/lib/net45/System.Web.Mvc.dll
  Microsoft.AspNet.Razor.3.2.7/lib/net45/System.Web.Razor.dll
  Microsoft.AspNet.WebPages.3.2.7/lib/net45/System.Web.WebPages.dll
  Microsoft.AspNet.WebPages.3.2.7/lib/net45/System.Web.WebPages.Razor.dll
  Microsoft.AspNet.WebPages.3.2.7/lib/net45/System.Web.Helpers.dll
  Microsoft.AspNet.WebPages.3.2.7/lib/net45/System.Web.WebPages.Deployment.dll
  Microsoft.Web.Infrastructure.1.0.0.0/lib/net40/Microsoft.Web.Infrastructure.dll
  Newtonsoft.Json.13.0.4/lib/net45/Newtonsoft.Json.dll
  Dapper.1.50.5/lib/net451/Dapper.dll
  Oracle.ManagedDataAccess.12.1.2400/lib/net40/Oracle.ManagedDataAccess.dll
```

版本注意事項：

- Oracle 的 **4.121.2.0 是 DLL 組件版本**；本專案使用的 NuGet 套件版本為 **12.1.2400**。公司若提供相同組件版本的其他修補套件，仍須核對檔案版本。
- `mvc/tools/Restore-Packages.ps1` 目前預設仍下載 Newtonsoft.Json 9.0.1，本機預覽分支則下載 13.0.3，與專案實際引用的 **13.0.4** 不一致。準備時以本表、`packages.config` 與 `.csproj` 的實際引用路徑為準。
- 該還原腳本會連外網，不適合在公司離線環境執行。本文件未修改該腳本。

## 二 公司 Kendo DLL

將公司使用的 **Kendo 2019、ASP.NET MVC 5 配套** DLL 放到：

```text
mvc/lib/company/Kendo.Mvc.dll
```

目前工作目錄缺少此檔案，預設公司組態會因此無法建置。應向公司取得配套版本，且 DLL 與前端 Kendo scripts 應來自同一發行版本。

`mvc/lib/local/Kendo.Mvc.dll` 是本機 Telerik 2026 預覽版本，不應複製到 `company` 目錄替代公司版本。

一般公司組態不需要本機預覽專用的 Telerik.Licensing、Telerik.SvgIcons、Telerik.FontIcons 或 WebApi 5.3.0 套件。

## 三 前端資源

目前 Layout 使用以下固定路徑，需提供對應檔案：

```text
mvc/Web/Scripts/vendor/
  jquery.min.js                    公司 jQuery 3.6.0
  kendo.all.min.js                  公司 Kendo 2019 配套
  kendo.aspnetmvc.min.js            同一 Kendo 發行版本
  kendo.culture.zh-TW.min.js        同一 Kendo 發行版本
  kendo.messages.zh-TW.min.js       同一 Kendo 發行版本

mvc/Web/Content/vendor/
  kendo-theme.css                  公司 Kendo 配套樣式
  sweetalert2/
    sweetalert2.min.css
    sweetalert2.min.js
```

SweetAlert2 已隨專案保存，保留其檔案及授權說明即可。Font Awesome 圖示及授權說明也應保留。

現有本機 vendor 的 Kendo 資源為 2026 配套，jQuery 為 3.7.1；公司測試時應換成公司版本。

**Kendo 樣式需要依實際公司配套處理。** 如果公司版本使用 common CSS、主題 CSS、字型或圖片，須一起放入並維持相對路徑，必要時調整 `mvc/Web/Views/Shared/_Layout.cshtml` 的 CSS 引用。不能只將其中一個檔案改名為 `kendo-theme.css` 就視為配套完整。

`mvc/Web/Scripts/vendor/` 及部分 Content vendor 資源被 Git 排除，離線交接時需另外確認齊全。

## 四 公司電腦的 Visual Studio 環境

以下是電腦端的開發元件，不是放進 `packages` 就會具備的套件：

- Visual Studio 的「ASP.NET 與網頁程式開發」工作負載及 ASP.NET MVC／.NET Framework 專案支援。
- .NET Framework 4.8 Developer Pack／Targeting Pack；只有執行階段不等於具備編譯參考組件。
- IIS Express，供 Visual Studio 本機啟動網站。

若公司電腦缺少上述元件，需要公司可用的離線安裝來源。

## 五 用 Visual Studio 啟動

1. 放妥上述 NuGet 套件、公司 Kendo DLL 與前端資源。
2. 開啟 `mvc/CARGO.sln`。
3. 將 `Web` 設為啟始專案。
4. 選擇一般 `Debug / Any CPU` 組態，不啟用 `UseLocalTelerik` 或 `LocalDemo`。
5. 建置方案，確認沒有遺失參考或版本不相容錯誤。
6. 使用 IIS Express 啟動，進入 `/CARGO_D01`。

本專案預設啟動路由為 CARGO_D01。公司組態要求已驗證的使用者與角色；如果尚未接登入，不能把頁面拒絕存取當成套件缺少。

不要用 `mvc/tools/Start-D01Demo.ps1` 代替公司版啟動程序。該脚本依賴本機 Telerik 2026 安裝目錄與授權建置流程，產生的是獨立的本機示範輸出。

## 六 能操作資料的額外條件

補齊套件後，仍需完成以下公司測試環境接入：

| 項目 | 需要準備的內容 |
|---|---|
| Oracle 連線 | 由公司既有設定機制提供名為 `CARGO` 的 connection string，並可透過內網連線測試資料庫 |
| 資料表與 sequence | 由資料庫負責人審閱 `mvc/Database/` 的 SQL，建立專用測試 Schema |
| 登入 | 宿主提供已驗證的 `User.Identity.Name` |
| 角色 | 提供相應的單位主管、業務、運輸主管、駕駛等角色與授權對應 |
| 主檔與對應資料 | 選項、車輛、司機、主管範圍及司機帳號對應等測試資料 |

資料庫腳本順序及用途：

1. `001_D01.sql`：D01 基礎資料表與 sequence。
2. `002_D01_sample_options.sql`：虛構測試選項，依測試需要使用，不代表公司主檔。
3. `003_D02_D04.sql`：主管審核、調度及運輸簽審相關結構。
4. `004_D05_D06.sql`：實登及司機帳號對應結構。
5. `005_D03_RESOURCE_TONS.sql`：只適用於尚未有 `TONS` 欄位的既有 Schema；新建 Schema 的 `003` 已包含該欄位，不能重複新增。

這些 SQL 尚未在公司 Oracle 環境驗證，不應直接當作正式部署程序。SMTP 與其他公司系統串接也不會因為套件齊全就自動完成。

## 七 攜帶前確認

- [ ] 專案原始碼與 `mvc/CARGO.sln` 已備妥。
- [ ] 七個指定版本的完整套件資料夾已放到 `mvc/packages/`。
- [ ] 公司 Kendo DLL 已放到 `mvc/lib/company/Kendo.Mvc.dll`。
- [ ] 公司 jQuery、Kendo JS、CSS 及 CSS 引用資源齊全。
- [ ] SweetAlert2、圖示及授權說明已保留。
- [ ] 公司電腦已具備 VS 網頁開發元件、.NET Framework 4.8 開發元件及 IIS Express。
- [ ] 如需操作資料，公司登入、角色、Oracle 測試 Schema 與主檔已準備。
- [ ] 未將 `.env`、密碼、連線機密、私人授權金鑰或公司正式資料混入交接檔案。

## 八 本次檢查結果

2026-10-11 在本機使用既有相依檔案完成以下檢查，未連外網：

- HTML 原型 Guardrails：234 項通過、0 失敗。
- MVC D01 規則、示範資料、Workflow、Usage 及 Queries 測試通過。
- D01～D06 與共用明細 Razor 產生碼編譯通過；此項使用本機 2026 Kendo 配套。
- 非示範 Oracle Service 編譯通過，未建立資料庫連線。
- 14 張 Oracle 資料表與 DTO 結構對照通過。

上述結果不等同公司 Kendo 2019 建置、Oracle SQL／交易或公司登入流程已驗收。公司版能否完整執行，仍須在配套齊全後完成現場測試。
