# CARGO 模組 D MVC 版（D01～D06）

本目錄為 ASP.NET MVC 5／.NET Framework 4.8 的模組 D 移植功能，採 Web、DTO、Service 三個 Project。原有根目錄 HTML／JavaScript 原型仍可獨立使用。分層與命名依 [轉換原則](../docs/MVC-MIGRATION-PRINCIPLES.md)。

## 本次功能

- 一般用車申請查詢、Kendo MVC Helper Grid、伺服器端分頁及單欄排序；篩選使用列表上方的申請人、用車日期、狀態條件。
- 新增、暫存、編輯、送出、退回修編後重送，以及調度確認前撤回修改。
- 申請內容、簽核／派車結果、指派歷史、通知及異動紀錄的明細呈現。
- 派車簽審通過後提出提前歸還；只儲存待確認請求，不直接縮短用車期間。
- 車種順位 1～3，順位 1 必填、不得重複；實際車輛仍由調度指派。
- 自駕人最多 8 筆、可增刪行程地點及隨行貨物，保留現有申請資訊與提示欄位。

D02 單位主管審核、D03 派車調度、D04 運輸主管簽審已接上同一套申請資料；詳細範圍、公司接入點及驗收方式見 [D02～D04 轉換說明](D02-D04.md)。SMTP、跨模組申請引導與 C 模組本體尚未接入 MVC。

D05 車輛使用實登與 D06 司機任務單已新增，說明與待驗收項目見 [D05～D06 轉換說明](D05-D06.md)。駕駛僅看自己的任務，業務可看全部及自駕交接。

## 開啟與相依

### 不連 Oracle 的本機示範

本機必要套件準備好後，在儲存庫根目錄的 PowerShell 執行：

```powershell
.\mvc\tools\Start-D01Demo.ps1
```

開啟 `http://localhost:5188/CARGO_D01`。保持 PowerShell 視窗開啟，結束時按 Q。若 5188 已使用，可加 `-Port 5189`。此指令不下載套件，會使用現有 MSBuild、IIS Express 與 Telerik 資源。

示範使用固定的虛構申請人（兼具示範主管／業務／運輸主管角色）、7 筆不同狀態資料與記憶體儲存。可以新增暫存、修改送出、撤回修改；GU000003 可測退回修編重送，GU000005 可測提前歸還。網站重啟後資料重置，不會連 Oracle 或寄信。

示範身分和資料實作僅在 `UseLocalTelerik=true` 加 `LocalDemo=true` 時編入獨立輸出，且只接受本機 loopback 請求與示範暫存組態。公司建置不包含示範身分與假資料實作；不得將示範輸出部署到公司。

本機 Telerik 2026 需有效試用或正式授權。使用者將官方下載的 `telerik-license.txt` 放在 `%AppData%\Telerik` 後，重新執行啟動指令；本機建置會透過官方 `Telerik.Licensing.targets` 驗證並產生啟用資訊。金鑰由官方建置工具使用，不由啟動腳本讀取或輸出，也不提交到 Git。此建置流程只在 `UseLocalTelerik=true` 啟用。

使用具備 ASP.NET Web 開發工具與 .NET Framework 4.8 Targeting Pack 的 Visual Studio 開啟 `CARGO.sln`。資料存取使用 Dapper 1.50.5，沒有 EF、Repository 或 DI 容器。

Oracle 官方 NuGet 套件 **12.1.2400** 內的 `Oracle.ManagedDataAccess.dll` 組件版本為 **4.121.2.0**。公司清單使用的是組件版本，不能直接以 4.121.2.0 當作 NuGet 套件版本還原。公司可能採同組件版本的其他修補套件，交接時仍須比對檔案版本。

從儲存庫根目錄執行以下命令會連到官方 NuGet 下載必要套件；離線時可由公司套件來源提供相同目錄。依工作規範，執行下載前須先取得授權。

```powershell
.\mvc\tools\Restore-Packages.ps1
```

所有下載套件、商業 DLL、vendor scripts 與編譯輸出都在 `.gitignore` 排除範圍。不要提交授權金鑰、連線機密或公司正式資料。

## 公司目標與本機預覽分開

| 組態 | MVC | Kendo | 用途 |
|---|---|---|---|
| 預設 Debug／Release | 5.2.7 | 公司提供的 2019 配套 | 公司編譯與驗收 |
| `UseLocalTelerik=true` | 5.3.0 | 本機 2026.3.811 | 暫時編譯與預覽 |

本機已安裝的 2026 DLL 實際參考 MVC 5.3.0，故不能用它證明 MVC 5.2.7／Kendo 2019 相容性。`packages.config` 與來源 `Web.config` 保留公司版本；本機預覽只改變建置參考與另建的預覽目錄。

公司組態需自行提供配套的 `mvc/lib/company/Kendo.Mvc.dll`。本機預覽可執行：

```powershell
.\mvc\tools\Restore-Packages.ps1 -LocalPreview
.\mvc\tools\Prepare-LocalTelerik.ps1
msbuild .\mvc\CARGO.sln /p:UseLocalTelerik=true /p:UseSharedCompilation=false /t:Build
$preview = .\mvc\tools\Stage-LocalPreview.ps1
```

`Prepare-LocalTelerik.ps1` 僅從已安裝的 Telerik 2026 目錄複製指定 DLL、JS、CSS，以及已下載的 jQuery 3.7.1；它不讀取授權金鑰、不設定啟用狀態。官方授權工作於本機建置時處理試用授權，瀏覽器可顯示官方有效試用提示。

`Stage-LocalPreview.ps1` 在 `mvc/artifacts` 產生獨立目錄，複製預覽輸出並設定新版需要的 binding redirects。不要將此預覽目錄當成公司部署成品。

公司版應使用公司 jQuery 與 2019 配套 scripts／CSS；不要帶入本機 `vendor` 內容。Layout 與 D01 畫面保留原型的深藍側欄、橘色漸層卡片、亮藍欄位、底線輸入及狀態徽章。`Content/prototype.css` 取自根目錄 `css/styles.css`，`cargo.css` 只負責 MVC／Kendo 配接；後續移植不得自行換風格。尚未移植的導覽項目停用。2019 API、樣式及公司 jQuery 3.6.0 的組合必須另行測試。

## Oracle 與登入接入

1. 由資料庫負責人檢查 `Database/001_D01.sql` 的 Schema 草案，在專用開發 Schema 建立表格與 sequence；腳本尚未在 Oracle 執行。`CARGO_D_OPTION` 是目前的清單接點，可依公司的主檔查詢改寫 `CARGO_DService.GetOptions()`。
2. `Database/002_D01_sample_options.sql` 只含虛構測試選項，供開發使用；不代表公司主檔。
3. 由公司既有設定機制提供名為 `CARGO` 的 connection string。Service 透過 `ConfigurationManager.ConnectionStrings["CARGO"]` 取得，原始碼不含實際連線設定。
4. 公司組態由宿主提供已驗證的 `User.Identity.Name` 與角色；未登入時 Controller 不開放。本機示範身分只存在獨立示範建置。登入方案、代理申請與主管權限仍需接入公司既有機制。
5. 目前 D01 僅查詢與操作登入使用者建立的申請，以 `OWNER_ID` 控制；申請人顯示欄位不作為授權依據。例行用車依伺服器端角色「總經理」或「部長秘書」判斷，沒有原型的任意角色切換。

Schema 中 `CARGO_D_REQUEST` 保存申請主檔、狀態及後續流程結果；地點、自駕人、貨物、歷程、指派區間與通知各自有對應表及 DTO。申請單號暫以 sequence 產生 `GU`＋至少六位數字，正式編號格式可依公司規範調整。所有相關寫入使用同一筆交易；版本欄位防止舊畫面覆蓋已更新的申請，D02～D04 同样使用版本檢查，依既有範圍不新增跨單資源鎖定。

## 驗證

從儲存庫根目錄執行：

```powershell
# 新 C# 規則及 DTO／ViewModel 轉換，使用 MVC 5.2.7，不需 Oracle 或 Telerik runtime
.\mvc\tools\Test-D01.ps1

# 記憶體示範流程、所有權與版本衝突
.\mvc\tools\Test-D01Demo.ps1

# D02～D04 狀態、角色、資源、批次及生命週期
.\mvc\tools\Test-Workflow.ps1

# D05／D06 實登、修訂與司機授權範圍
.\mvc\tools\Test-Usage.ps1

# Schema 與 DTO 欄位對照、專案分層
python .\mvc\tests\check-structure.py

# 既有原型回歸
node .\tests\guardrails.test.js

# 本機 2026 配套建置與 Razor 預編譯
msbuild .\mvc\CARGO.sln /p:UseLocalTelerik=true /p:UseSharedCompilation=false /t:Build
$preview = .\mvc\tools\Stage-LocalPreview.ps1
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe" -v / -p $preview
```

本機示範已在瀏覽器操作新增暫存、編輯送出、撤回修改與列表載入。Oracle SQL、交易、主鍵與 Unicode 參數的實際執行，須在 Oracle 19c 測試資料庫確認；公司 Kendo 2019、登入、角色及主檔接入也尚待驗收。

建議公司端先驗收一條完整流程：新增暫存 → 編輯送出 → 二級審退回 → 修改重送 → 調度前撤回。再依 D02～D04 說明驗證主管核准、派車送簽、運輸主管同意／退回及提前歸還確認。另測試兩個瀏覽器同時編輯同一單時，舊版本不能覆蓋新資料。
