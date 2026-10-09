# 車輛派遣系統 ASP.NET MVC 轉換原則

確認日期：2026-10-09

本專案由 HTML／JavaScript 互動原型轉換為公司使用的 ASP.NET MVC 5／.NET Framework 4.8 專案，採 Web、DTO、Service 三層專案結構，使用 Oracle Database 19c 與 Dapper，並沿用 `Html.Kendo().Grid(...)` 的畫面開發方式。

本文件記錄已確認的轉換規範，供後續建立專案、移植模組及驗收使用。命名範例不代表資料表結構、所有功能編號或方法簽章已定案。

## 1 適用範圍與原有規劃

轉換涵蓋模組 A、B、C、D 及共用功能。以現有需求與業務規則為基礎，調整執行架構、資料持久化及畫面實作，不因更換技術而自行改變媒合、派車、審批及狀態流轉規則。

轉換必須保留原版 UI 介面與色彩風格：側欄與頁面配置、深藍主色、橘色漸層標題列、欄位資訊卡與底線式輸入框、按鈕及狀態徽章皆以現有原型為準。改用 Razor、MVC Helper 或 Kendo Grid 不代表重新設計介面；查詢、新增／編輯、明細頁均須進行視覺比對。尚未移植的導覽項目須標示不可用，不連到不存在的功能。

本文件取代 [PLAN.md](PLAN.md) 中下列架構假設：

| 原規劃 | 轉換規範 |
|---|---|
| `VD.Core`、`VD.Web` 等專案分層 | Web、DTO、Service 三個主要專案 |
| SQL Server 假設 | Oracle Database 19c |
| `VD_` 資料表前綴 | `CARGO` 前綴，具體名稱使用 `CARGO_…` |
| Repository 資料存取 | Service 直接透過 Dapper 與 SQL 字串存取 Oracle，不設 Repository |

不使用 DI 容器的方向維持不變，Service 由呼叫端直接 `new`。原規劃中的 static Provider 不作為所有 Service 的通用實作模式。

[UI-CONVENTIONS.md](UI-CONVENTIONS.md) 的查詢、列表、新增、明細等操作流程可作為畫面參考；其中純前端頁面容器、全域狀態與 `innerHTML` 切頁方式，不作為 MVC 版的架構要求。[SPEC-DATA.md](SPEC-DATA.md) 中實際業務資料不得提交至此儲存庫的規範持續適用。

## 2 目標環境與套件基準

| 項目 | 目標 |
|---|---|
| Web 框架 | ASP.NET MVC 5，Microsoft.AspNet.Mvc 5.2.7 |
| 執行框架 | .NET Framework 4.8，不使用 ASP.NET Core |
| 畫面 | Razor View、ViewModel、JavaScript、Kendo MVC Helper |
| 資料庫 | Oracle Database 19c |
| 資料存取 | Dapper 1.50.5 |
| Oracle 驅動 | Oracle.ManagedDataAccess 4.121.2.0 |
| 公司 Kendo 前端版本 | 2019.1.115；對應 MVC DLL 版本仍需核對 |
| 公司 jQuery 版本紀錄 | 3.6.0；與 Kendo 的搭配仍需實測 |
| 本機暫時驗證環境 | Telerik UI for ASP.NET MVC 2026 Q3 |

公司提供的其餘套件版本紀錄如下。這是相容性參考，不代表新專案必須全部安裝或使用。

| 套件 | 版本紀錄 |
|---|---|
| EntityFramework | 6.1.3；本次資料存取不使用 EF |
| Newtonsoft.Json | 9.0.1 |
| Microsoft.AspNet.WebApi.* | 5.2.x，細項未確認 |
| Microsoft.Owin | 3.0.1 |
| Microsoft.AspNet.Identity | 2.2.1 |
| SimpleInjector | 4.0.8；本專案不使用注入 |
| AutoMapper | 9.0.0；是否採用另行決定 |
| EPPlus | 4.1.0 |
| log4net | 2.0.8 |
| InternationalNumericFormatter | 2.0.0 |
| jQuery UI | 1.8.24 |
| Bootstrap、SweetAlert2、AdminLTE、Masonry | 版本待確認 |

實作時只加入必要相依，依公司實際專案確認版本，不因本機範本或安裝工具較新而自動升級公司套件。不另建獨立 Web API 專案。

D01 實作核對補充：Oracle 4.121.2.0 為組件版本，官方 NuGet 12.1.2400 內含此組件。本機 Telerik 2026.3.811 的 MVC DLL 參考 MVC 5.3.0，須使用獨立預覽組態，不能取代公司 MVC 5.2.7 的編譯驗證。申請畫面保留車種順位 1～3，順位 1 必填、不可重複，實際車輛仍由調度指派（2026-10-09 確認）。

## 3 Solution 與 Project 分工

Solution 以三個主要 Project 組成：`Web.csproj`、`DTO.csproj`、`Service.csproj`。

| Project | 責任 |
|---|---|
| Web | Controller、View、ViewModel、請求與輸入處理、DTO 與 ViewModel 轉換、Kendo 畫面整合、回傳畫面或 JSON |
| DTO | 與資料表結構對應的類別，以及獨立的查詢條件、查詢結果等傳輸類別 |
| Service | 業務規則、派車與媒合邏輯、Oracle 連線、SQL、Dapper Query／Execute、交易與資料寫入 |

參考關係：

```text
Web ─────→ Service
 │            │
 └──→ DTO ←───┘
```

- Web 可以參考 Service 與 DTO。
- Service 可以參考 DTO，不參考 Web、ViewModel 或 Kendo 畫面類型。
- DTO 不參考 Web 或 Service，不包含資料庫操作。
- Controller 不撰寫 SQL、不建立 Oracle 連線，也不實作派車與媒合演算法。
- 不建立 Repository 層或為每張資料表增加 Repository 類別。

## 4 命名與功能劃分

系統代碼使用 `CARGO`，模組代碼接在系統代碼後，功能再加兩位編號。模組 D 的一般用車申請已確認為 D01，本次延伸轉換對照為 D02 單位主管審核、D03 派車調度、D04 運輸主管簽審；後續功能於移植前建立對照表。

| 用途 | 範例 |
|---|---|
| 模組 D 共用端點 | `CARGO_DController` |
| 模組 D 一般用車申請 | `CARGO_D01Controller` |
| 模組 D 共用業務邏輯 | `CARGO_DService` |
| 一般用車申請業務邏輯 | `CARGO_D01Service` |
| 一般用車申請查詢條件 | `CARGO_D01QueryDto` |
| 一般用車申請列表結果 | `CARGO_D01ListItemDto` |
| 查詢畫面 ViewModel | `CARGO_D01IndexViewModel` |
| 資料表及其對應 DTO | `CARGO_D_REQUEST`，僅為命名示例 |

`CARGO_DController` 是獨立 Controller，提供模組內共用 Action；`CARGO_D01Controller` 不繼承它。Controller 之間不直接互相呼叫，共用業務邏輯由 Service 重用。

資料表及相關類別使用 `CARGO` 前綴。DTO 的資料表對應類別依實際 Schema 設計；同一張表由多個功能操作時共用同一個 DTO，不按功能複製。

## 5 目錄示例

以下以 D01 示範，實際檔案依畫面與資料表拆分：

```text
Web/
  Controllers/
    CARGO_DController.cs
    CARGO_D01Controller.cs
  Views/
    CARGO_D01/
      Index.cshtml
      Create.cshtml
      Detail.cshtml
  ViewModels/
    CARGO_D01/
      CARGO_D01IndexViewModel.cs
      CARGO_D01CreateViewModel.cs
      CARGO_D01DetailViewModel.cs

DTO/
  Tables/
    CARGO_D_REQUEST.cs
  Queries/
    CARGO_D01QueryDto.cs
  Results/
    CARGO_D01ListItemDto.cs

Service/
  CARGO_DService.cs
  CARGO_D01Service.cs
```

## 6 資料傳遞與模型邊界

```text
View／Grid
    ↕ ViewModel
Controller
    ↕ DTO／查詢條件／查詢結果
Service
    ↕ Dapper＋參數化 SQL
Oracle Database 19c
```

- Controller 與 View 以 ViewModel 傳遞資料，Grid 的列資料也由 Web 層整理為畫面需要的模型。
- Controller 與 Service 以 DTO 專案內的類別傳遞資料，不把 ViewModel 傳入 Service。
- 資料表 DTO 對應欄位、型別及可空性，不加入純畫面的格式或顯示欄位。
- 日期區間、分頁、排序等查詢條件放在獨立 Query 類別。
- 跨表組合結果，例如申請單搭配申請人姓名與車號，放在獨立 Result 類別，不冒充單一資料表 DTO。
- DTO 與 ViewModel 的轉換由 Web 負責；是否使用 AutoMapper 不作為本次前置要求。
- Kendo 的 `DataSourceRequest`、`DataSourceResult` 等相依集中在 Web，由 Web 轉換成 Service 可接受的查詢條件與回傳資料。

## 7 Service 與資料庫操作

Controller 直接 `new` 所需的 Service，不使用 SimpleInjector 或其他注入容器。共用 Service 亦使用一般物件建立方式，不保留全域共用的開啟中資料庫連線。

所有 Oracle 連線建立、查詢、執行與交易程式碼放在 Service 專案。可在 Service 內抽出共用的連線建立輔助程式，但不形成 Repository 層。

SQL 以字串撰寫，透過 Dapper 的 Query／Execute 等操作執行，遵循下列原則：

- 查詢及寫入值使用參數，不直接拼接使用者輸入。
- 動態排序欄位、方向等使用明確允許清單，不直接拼接 Grid 傳入的欄位文字。
- 每次操作妥善開啟、釋放連線；同一筆交易內的操作共用連線與交易物件。
- 多筆必須一起成功的寫入採交易，失敗時回復；這不等同於自行新增原業務規劃未定義的容量鎖定或併發策略。
- 連線設定由公司既有設定方式接入，不在原始碼中硬編碼帳密。
- SQL、主鍵產生方式、Oracle 型別與 C# 型別對應，依 Oracle 19c 與實際 Schema 設計。

前端可做即時輸入提示，Service 必須負責影響資料正確性的業務檢核，不能只依賴瀏覽器檢查。

## 8 Kendo Grid 與跨版本開發

Grid 以 `Html.Kendo().Grid(...)` 為主要實作方式，配合公司既有習慣；JavaScript 用於事件與畫面互動，不以直接初始化全面替代 MVC Helper。

相容性目標是公司 Kendo 2019.1.115 環境。本機可暫用 Telerik 2026 的配套資源驗證畫面與流程，但只使用經確認在目標舊版存在的 Helper API 與功能。

- 每個環境的 `Kendo.Mvc.dll`、Kendo scripts（包含 `kendo.aspnetmvc.min.js`）及相容 CSS／主題必須配套，不混用新版 DLL 與舊版 scripts。
- 基本欄位、分頁、排序、篩選與列操作優先實作，新版專屬設定不得直接納入目標程式。
- 盡量避免以 Kendo 內部 DOM 結構作為自訂樣式或事件綁定的依據。
- 本機 2026 通過，只代表新版環境驗證通過；必須另以公司版本重新編譯並測試，包含 Razor View 的編譯及實際開頁。
- 公司 jQuery 3.6.0 不在 Kendo 2019.1.115 官方支援版本清單內，應核對公司頁面實際載入版本並測試，不自行更換全站 jQuery。
- 舊版下載權限與本機試用的使用範圍仍需依實際授權確認，不假定安裝新版就能取得舊版。

Kendo Helper 可以搭配 Dapper，不要求 EF。對已由 Dapper 查出的集合呼叫 `ToDataSourceResult()`，不會自動把資料操作轉成 Oracle SQL。持續累積的申請紀錄應在 Service 執行資料庫分頁、篩選、排序及總筆數查詢，避免先取回整張表再分頁。

若 Service 已完成分頁，Web 應使用該頁資料及總筆數建立 Grid 回應，不再次分頁。少量主檔可依資料量採記憶體處理。

## 9 轉換步驟與驗收原則

1. 建立 Web、DTO、Service 的 MVC 5／.NET Framework 4.8 骨架，確認必要套件、專案參考及本機建置工具。
2. 盤點現有功能，建立功能編號、Controller、Service、ViewModel 與資料表對照。
3. 先以 D01 的查詢、列表、新增、明細建立完整範例，驗證 Helper、模型轉換與資料存取邊界。
4. 將目前 JavaScript 中的核心業務規則移到 Service 的 C# 程式，保留前端必要的互動與提示。
5. 建立 Oracle Schema 與 SQL，移植持久化、狀態流轉及交易；資料表名稱與欄位另行設計確認。
6. 儘早在公司 2019 環境編譯並驗證代表性畫面，再逐步擴展其他功能。
7. 在公司測試環境接入登入身分、權限、SMTP、內網 API 與實際設定，完成整合驗收。

本機沒有 Oracle 測試環境時，可先用獨立測試資料完成畫面與規則驗證；不得將此結果視為 Oracle SQL 或交易已驗證，也不因此新增 Repository 或 DI 架構。

驗收至少涵蓋：

- .NET Framework 4.8 的編譯與 Razor 畫面開啟。
- 公司 Kendo 2019 的查詢、分頁、排序、篩選及列操作。
- Oracle 19c 的參數、型別、空值、日期、跨表查詢、分頁總筆數與交易行為。
- 原型既有核心規則在 C# 版本的等價結果；既有 Node 測試可作為案例來源，不能替代 C# 驗證。
- 申請、審核、派車與完成等適用流程，以及公司權限與外部服務整合。
- 公司內網環境可取得必要套件及本機靜態資源，不依賴 CDN 或外部線上服務才能操作。

## 10 實作前仍需確認

以下事項不影響已確認的分層規範，但必須在相關工作開始前取得具體資料：

- 公司實際 `Kendo.Mvc.dll`、前端 JS／CSS 版本與一份去除機密的 Helper 使用範例。
- Bootstrap、SweetAlert2、AdminLTE、Masonry 的版本、共用 Layout 與資源載入順序。
- 公司 Visual Studio／編譯器版本，以及本機 MVC Web 建置工具與 .NET Framework 4.8 Targeting Pack。
- 各模組功能編號、實際資料表、主鍵方式與公司資料庫欄位慣例。
- 本機 Oracle 測試環境是否可用，以及帶回公司時整合至既有網站或獨立網站。
- 公司登入身分、權限、設定與例外處理的接入慣例。

確認範本與設定時不提供帳密、連線字串中的機密、授權金鑰或正式業務資料。套件下載、安裝、部署與驗證方式仍遵循專案的授權及操作限制。

## 11 版本相容性參考

- [Telerik MVC 本機前端資源與版本配套](https://www.telerik.com/aspnet-mvc/documentation/installation/adding-client-side-resources/using-local-files)
- [Kendo UI 支援的 jQuery 版本](https://www.telerik.com/kendo-jquery-ui/documentation/intro/supporting/jquery-support)
- [MVC Grid Ajax Binding](https://www.telerik.com/aspnet-mvc/documentation/html-helpers/data-management/grid/binding/ajax-binding)
- [MVC DataSource 與資料操作](https://www.telerik.com/aspnet-mvc/documentation/html-helpers/datasource/overview)

上述官方文件會持續更新；實作舊版功能時，仍須以公司實際 2019 組件及該版本可用 API 為準。

## D02～D04 接續轉換紀錄（2026-10-09）

三個功能使用獨立 `CARGO_D02Controller`、`CARGO_D03Controller`、`CARGO_D04Controller` 與同名 Service；共用流程 DTO／ViewModel 使用 `CARGO_DWork…`，共用規則留在 Service。主管、車輛／司機、資源停用區間、簽審輪次先採 `mvc/Database/003_D02_D04.sql` 草案，尚未代表公司實際 Schema。界面仍沿用原型樣式及 `Html.Kendo().Grid(...)`。詳見 [接續轉換說明](../mvc/D02-D04.md)。
